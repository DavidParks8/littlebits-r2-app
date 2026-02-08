using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace LittleBitsR2Controller.Services;

public class BluetoothService : IBluetoothService, IDisposable
{
    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private readonly WeakEventManager _connectionStatusChangedEventManager = new();
    private IDevice? _connectedDevice;
    private ICharacteristic? _writeCharacteristic;
    private bool _disposed;

    // Auto-reconnect state
    private BluetoothDevice? _lastConnectedDevice;
    private bool _isUserDisconnect;
    private CancellationTokenSource? _reconnectCts;
    private const int ReconnectDelayMs = 5000;
    private const int ReconnectMaxAttempts = 60; // ~5 minutes

    // Serialises multi-write command sequences (drive, stop, sound) so they
    // cannot interleave.  Without this, a drive command's delayed second
    // write can fire after a stop command, overriding it.
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    // Connection health monitoring — detects silent disconnects
    // (e.g. device powered off without a BLE disconnect event)
    private System.Threading.Timer? _healthCheckTimer;
    private const int HealthCheckIntervalMs = 3000;
    private int _consecutiveHealthFailures;

    public bool IsReconnecting { get; private set; }
    
    public event EventHandler<bool>? ConnectionStatusChanged
    {
        add => _connectionStatusChangedEventManager.AddEventHandler(value);
        remove => _connectionStatusChangedEventManager.RemoveEventHandler(value);
    }
    
    public bool IsConnected => _connectedDevice?.State == Plugin.BLE.Abstractions.DeviceState.Connected;

    private static bool IsR2D2DeviceName(string? name) =>
        !string.IsNullOrEmpty(name) &&
        (name.StartsWith("w32", StringComparison.OrdinalIgnoreCase) ||
         name.StartsWith("R2D2", StringComparison.OrdinalIgnoreCase));

    public BluetoothService(IBluetoothLE bluetoothLE, IAdapter adapter)
    {
        _bluetoothLE = bluetoothLE ?? throw new ArgumentNullException(nameof(bluetoothLE));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        
        _adapter.DeviceConnected += OnDeviceConnected;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
    }

    public Task<bool> IsBluetoothEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_bluetoothLE.IsOn);
    }

    public async Task<IEnumerable<BluetoothDevice>> ScanForDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<BluetoothDevice>();
        
        if (!await IsBluetoothEnabledAsync(cancellationToken))
        {
            return devices;
        }

        var serviceUuid = Guid.Parse(R2D2Protocol.ServiceUuid);
        HashSet<string> seen = [];

        // Phase 1: Check for already-paired/connected devices with the R2D2 service UUID.
        // On Windows, BLE scan may not discover previously-paired devices.
        try
        {
            var pairedDevices = _adapter.GetSystemConnectedOrPairedDevices(new[] { serviceUuid });
            foreach (var device in pairedDevices)
            {
                var id = device.Id.ToString();
                if (!seen.Add(id))
                    continue;

                // If the device has a name, it must match a known R2D2 prefix;
                // unnamed devices get a fallback name and are kept.
                if (!string.IsNullOrEmpty(device.Name) && !IsR2D2DeviceName(device.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"[BLE] Skipping paired device with non-R2D2 name: '{device.Name}' ({id})");
                    continue;
                }

                var name = !string.IsNullOrEmpty(device.Name) ? device.Name : $"R2D2 ({id[..8]})";
                devices.Add(new BluetoothDevice(id, name));
                System.Diagnostics.Debug.WriteLine($"[BLE] Paired device found: {name} ({id})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Error checking paired devices: {ex.Message}");
        }

        // Phase 2: Active BLE scan filtered by the R2D2 service UUID.
        // This is much more reliable than name-matching, which varies by platform.
        var scanLock = new object();
        var scanResults = new List<IDevice>();

        EventHandler<DeviceEventArgs>? deviceDiscoveredHandler = null;
        deviceDiscoveredHandler = (s, e) =>
        {
            if (e.Device != null)
            {
                lock (scanLock)
                {
                    scanResults.Add(e.Device);
                }
                System.Diagnostics.Debug.WriteLine(
                    $"[BLE] Scan discovered: Name='{e.Device.Name}', Id={e.Device.Id}");
            }
        };

        try
        {
            _adapter.ScanTimeout = 10000; // 10 second scan
            _adapter.ScanMode = ScanMode.LowLatency;
            _adapter.DeviceDiscovered += deviceDiscoveredHandler;

            // Filter scan by R2D2 service UUID so we only get devices that
            // advertise the littleBits control hub service.
            await _adapter.StartScanningForDevicesAsync(
                serviceUuids: [serviceUuid],
                cancellationToken: cancellationToken);
        }
        finally
        {
            _adapter.DeviceDiscovered -= deviceDiscoveredHandler;
        }

        foreach (var device in scanResults)
        {
            var id = device.Id.ToString();
            if (!seen.Add(id))
                continue;

            var name = !string.IsNullOrEmpty(device.Name) ? device.Name : $"R2D2 ({id[..8]})";
            devices.Add(new BluetoothDevice(id, name));
        }

        System.Diagnostics.Debug.WriteLine(
            $"[BLE] Scan complete. Found {devices.Count} R2D2 device(s) out of {scanResults.Count} scanned.");

        // Phase 3: If service-UUID scan found nothing, do an unfiltered scan
        // and show all devices so the user can manually pick the R2D2.
        if (devices.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine(
                "[BLE] No devices found with R2D2 service UUID. Running unfiltered scan...");

            scanResults.Clear();
            seen.Clear();

            try
            {
                _adapter.ScanTimeout = 10000;
                _adapter.DeviceDiscovered += deviceDiscoveredHandler;

                await _adapter.StartScanningForDevicesAsync(
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _adapter.DeviceDiscovered -= deviceDiscoveredHandler;
            }

            foreach (var device in scanResults)
            {
                var id = device.Id.ToString();
                if (!seen.Add(id))
                    continue;

                // Only include devices whose name starts with a known R2D2 prefix
                if (IsR2D2DeviceName(device.Name))
                {
                    devices.Add(new BluetoothDevice(id, device.Name));
                    System.Diagnostics.Debug.WriteLine(
                        $"[BLE] Unfiltered device: Name='{device.Name}', Id={id}");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[BLE] Unfiltered scan complete. Showing {devices.Count} named device(s).");
        }

        return devices;
    }

    public async Task<bool> ConnectToDeviceAsync(BluetoothDevice device, CancellationToken cancellationToken = default)
    {
        try
        {
            // Cancel any in-progress reconnect loop since we're connecting explicitly
            StopReconnectLoop();

            var deviceId = Guid.Parse(device.Id);
            var bleDevice = await _adapter.ConnectToKnownDeviceAsync(deviceId, cancellationToken: cancellationToken);
            
            if (bleDevice == null)
            {
                return false;
            }

            _connectedDevice = bleDevice;

            // Get the service and characteristic using the correct UUIDs from the reverse-engineered protocol
            var services = await bleDevice.GetServicesAsync(cancellationToken);
            var service = services.FirstOrDefault(s => 
                s.Id.ToString().Equals(R2D2Protocol.ServiceUuid, StringComparison.OrdinalIgnoreCase));
            
            if (service != null)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                _writeCharacteristic = characteristics.FirstOrDefault(c => 
                    c.Id.ToString().Equals(R2D2Protocol.CharacteristicUuid, StringComparison.OrdinalIgnoreCase));
            }

            // Use WriteWithoutResponse to match the original JS implementation
            // (interface06's writeToChar uses characteristic.writeValue which is fire-and-forget,
            //  interface02 explicitly uses writeValueWithoutResponse)
            if (_writeCharacteristic != null)
            {
                try
                {
                    _writeCharacteristic.WriteType = Plugin.BLE.Abstractions.CharacteristicWriteType.WithoutResponse;
                }
                catch (InvalidOperationException)
                {
                    // Characteristic may not support WriteWithoutResponse; fall back to default
                    System.Diagnostics.Debug.WriteLine("[BLE] WriteWithoutResponse not supported, using default write type.");
                }
            }

            var connected = IsConnected && _writeCharacteristic != null;
            if (connected)
            {
                _lastConnectedDevice = device;
                _isUserDisconnect = false;
                StartHealthCheck();
            }

            return connected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isUserDisconnect = true;
        StopHealthCheck();
        StopReconnectLoop();

        if (_connectedDevice != null)
        {
            await _adapter.DisconnectDeviceAsync(_connectedDevice);
            _connectedDevice = null;
            _writeCharacteristic = null;
        }

        _lastConnectedDevice = null;
    }

    public async Task SendDriveCommandAsync(double speed, double turn, CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            // Send turn command first, then drive after a delay.
            // This matches the original JS driveAndTurn():
            //   if (x) turn(x);
            //   setTimeout(() => drive(y), 100);
            var turnData = R2D2Protocol.GetTurnCommand(turn);
            await _writeCharacteristic.WriteAsync(turnData, cancellationToken);

            // Delay between commands as required by the protocol (matches original 100ms timing)
            await Task.Delay(100, cancellationToken);

            // Send drive command (speed: -1.0 to 1.0)
            var driveData = R2D2Protocol.GetDriveCommand(speed);
            await _writeCharacteristic.WriteAsync(driveData, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending command: {ex.Message}");
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            // Original stop() sets y=31 and calls driveAndTurn() which sends the last
            // turn value + drive-stop.  We send drive-stop first (the critical command),
            // then straighten the wheels.
            var stopData = R2D2Protocol.GetStopCommand();
            await _writeCharacteristic.WriteAsync(stopData, cancellationToken);

            // Delay between commands (matches original 100ms timing)
            await Task.Delay(100, cancellationToken);

            // Reset turn to straight-ahead
            var turnStraight = R2D2Protocol.GetTurnStraightCommand();
            await _writeCharacteristic.WriteAsync(turnStraight, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending stop command: {ex.Message}");
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task SendSoundCommandAsync(string soundName, CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            var soundData = R2D2Protocol.GetSoundCommand(soundName);
            if (soundData != null)
            {
                await _writeCharacteristic.WriteAsync(soundData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending sound command: {ex.Message}");
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private void OnDeviceConnected(object? sender, DeviceEventArgs e)
    {
        _connectionStatusChangedEventManager.HandleEvent(this, true, nameof(ConnectionStatusChanged));
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        StopHealthCheck();
        _connectedDevice = null;
        _writeCharacteristic = null;
        _connectionStatusChangedEventManager.HandleEvent(this, false, nameof(ConnectionStatusChanged));

        // If the disconnect was unexpected and we know the device, start auto-reconnect
        if (!_isUserDisconnect && _lastConnectedDevice != null)
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Unexpected disconnect. Starting auto-reconnect...");
            StartReconnectLoop(_lastConnectedDevice);
        }
    }

    /// <summary>
    /// Periodically verifies the BLE connection is still alive by checking device state
    /// and attempting a characteristic read. Catches silent disconnects (e.g. device powered off)
    /// that the OS BLE stack doesn't report via the disconnect event.
    /// </summary>
    private void StartHealthCheck()
    {
        StopHealthCheck();
        _consecutiveHealthFailures = 0;
        _healthCheckTimer = new System.Threading.Timer(HealthCheckCallback, null, HealthCheckIntervalMs, HealthCheckIntervalMs);
        System.Diagnostics.Debug.WriteLine("[BLE] Connection health monitor started.");
    }

    private void StopHealthCheck()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    private async void HealthCheckCallback(object? state)
    {
        // Skip check if we're already reconnecting or deliberately disconnected
        if (_isUserDisconnect || IsReconnecting)
            return;

        try
        {
            var device = _connectedDevice;
            var characteristic = _writeCharacteristic;

            // Check 1: Device object or characteristic is gone
            if (device == null || characteristic == null)
            {
                HandleHealthCheckFailure("device or characteristic is null");
                return;
            }

            // Check 2: Plugin.BLE device state reports not-connected
            if (device.State != DeviceState.Connected)
            {
                HandleHealthCheckFailure($"device state is {device.State}");
                return;
            }

            // Check 3: Try to read services — this forces actual BLE communication.
            // If the device is off, this will throw.
            await device.GetServicesAsync();

            // Healthy — reset failure counter
            _consecutiveHealthFailures = 0;
        }
        catch (Exception ex)
        {
            HandleHealthCheckFailure($"exception: {ex.Message}");
        }
    }

    private void HandleHealthCheckFailure(string reason)
    {
        _consecutiveHealthFailures++;
        System.Diagnostics.Debug.WriteLine(
            $"[BLE] Health check failure #{_consecutiveHealthFailures}: {reason}");

        // Require 2 consecutive failures to avoid transient BLE hiccups
        if (_consecutiveHealthFailures >= 2)
        {
            System.Diagnostics.Debug.WriteLine(
                "[BLE] Device presumed offline. Triggering disconnect/reconnect.");
            StopHealthCheck();

            // Manually trigger the same flow as OnDeviceDisconnected
            _connectedDevice = null;
            _writeCharacteristic = null;
            _connectionStatusChangedEventManager.HandleEvent(this, false, nameof(ConnectionStatusChanged));

            if (_lastConnectedDevice != null)
            {
                StartReconnectLoop(_lastConnectedDevice);
            }
        }
    }

    private void StartReconnectLoop(BluetoothDevice device)
    {
        StopReconnectLoop();
        IsReconnecting = true;
        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;

        _ = Task.Run(async () =>
        {
            var attempt = 0;
            while (!token.IsCancellationRequested && attempt < ReconnectMaxAttempts)
            {
                attempt++;
                try
                {
                    // Wait before attempting (gives the device time to boot)
                    await Task.Delay(ReconnectDelayMs, token);

                    System.Diagnostics.Debug.WriteLine(
                        $"[BLE] Reconnect attempt {attempt}/{ReconnectMaxAttempts} to {device.Name}...");

                    var deviceId = Guid.Parse(device.Id);
                    var bleDevice = await _adapter.ConnectToKnownDeviceAsync(deviceId, cancellationToken: token);

                    if (bleDevice == null)
                        continue;

                    _connectedDevice = bleDevice;

                    // Re-acquire the service and characteristic
                    var services = await bleDevice.GetServicesAsync(token);
                    var service = services.FirstOrDefault(s =>
                        s.Id.ToString().Equals(R2D2Protocol.ServiceUuid, StringComparison.OrdinalIgnoreCase));

                    if (service != null)
                    {
                        var characteristics = await service.GetCharacteristicsAsync();
                        _writeCharacteristic = characteristics.FirstOrDefault(c =>
                            c.Id.ToString().Equals(R2D2Protocol.CharacteristicUuid, StringComparison.OrdinalIgnoreCase));
                    }

                    if (_writeCharacteristic != null)
                    {
                        try
                        {
                            _writeCharacteristic.WriteType = Plugin.BLE.Abstractions.CharacteristicWriteType.WithoutResponse;
                        }
                        catch (InvalidOperationException)
                        {
                            System.Diagnostics.Debug.WriteLine("[BLE] WriteWithoutResponse not supported on reconnect.");
                        }
                    }

                    if (IsConnected && _writeCharacteristic != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[BLE] Reconnected to {device.Name} after {attempt} attempt(s).");
                        IsReconnecting = false;
                        _isUserDisconnect = false;
                        StartHealthCheck();
                        // The adapter's DeviceConnected event will fire and notify the ViewModel
                        return;
                    }

                    // Connection partially succeeded but characteristic not found; disconnect and retry
                    _connectedDevice = null;
                    _writeCharacteristic = null;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BLE] Reconnect attempt {attempt} failed: {ex.Message}");
                }
            }

            IsReconnecting = false;
            System.Diagnostics.Debug.WriteLine(
                "[BLE] Auto-reconnect gave up after max attempts.");
        }, token);
    }

    private void StopReconnectLoop()
    {
        IsReconnecting = false;
        if (_reconnectCts != null)
        {
            _reconnectCts.Cancel();
            _reconnectCts.Dispose();
            _reconnectCts = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Cleanup managed resources
            StopHealthCheck();
            StopReconnectLoop();
            _adapter.DeviceConnected -= OnDeviceConnected;
            _adapter.DeviceDisconnected -= OnDeviceDisconnected;
            
            if (_connectedDevice != null)
            {
                _ = DisconnectAsync(); // Fire and forget
            }

            _commandLock.Dispose();
        }

        _disposed = true;
    }
}
