using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace LittleBitsR2Controller.Services;

public class BluetoothService : IBluetoothService, IDisposable
{
    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private ICharacteristic? _writeCharacteristic;
    private bool _disposed;
    
    public event EventHandler<bool>? ConnectionStatusChanged;
    
    public bool IsConnected => _connectedDevice?.State == Plugin.BLE.Abstractions.DeviceState.Connected;

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

        var scanResults = new List<IDevice>();
        
        EventHandler<Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs>? deviceDiscoveredHandler = null;
        deviceDiscoveredHandler = (s, e) =>
        {
            if (e.Device != null && !string.IsNullOrEmpty(e.Device.Name))
            {
                scanResults.Add(e.Device);
            }
        };
        
        try
        {
            _adapter.DeviceDiscovered += deviceDiscoveredHandler;

            await _adapter.StartScanningForDevicesAsync(cancellationToken: cancellationToken);
            await Task.Delay(5000, cancellationToken); // Scan for 5 seconds
            await _adapter.StopScanningForDevicesAsync();

            foreach (var device in scanResults)
            {
                if (!string.IsNullOrEmpty(device.Name))
                {
                    devices.Add(new BluetoothDevice(device.Id.ToString(), device.Name));
                }
            }
        }
        finally
        {
            // Remove event handler to prevent memory leaks
            if (deviceDiscoveredHandler != null)
            {
                _adapter.DeviceDiscovered -= deviceDiscoveredHandler;
            }
        }

        return devices;
    }

    public async Task<bool> ConnectToDeviceAsync(BluetoothDevice device, CancellationToken cancellationToken = default)
    {
        try
        {
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

            return IsConnected && _writeCharacteristic != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connectedDevice != null)
        {
            await _adapter.DisconnectDeviceAsync(_connectedDevice);
            _connectedDevice = null;
            _writeCharacteristic = null;
        }
    }

    public async Task SendDriveCommandAsync(double speed, double turn, CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        try
        {
            // Send drive command (speed: -1.0 to 1.0)
            var driveData = R2D2Protocol.GetDriveCommand(speed);
            await _writeCharacteristic.WriteAsync(driveData);
            
            // Small delay between commands
            await Task.Delay(20, cancellationToken);
            
            // Send turn command (turn: -1.0 to 1.0)
            var turnData = R2D2Protocol.GetTurnCommand(turn);
            await _writeCharacteristic.WriteAsync(turnData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending command: {ex.Message}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        try
        {
            var stopData = R2D2Protocol.GetStopCommand();
            await _writeCharacteristic.WriteAsync(stopData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending stop command: {ex.Message}");
        }
    }

    private void OnDeviceConnected(object? sender, DeviceEventArgs e)
    {
        ConnectionStatusChanged?.Invoke(this, true);
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        _connectedDevice = null;
        _writeCharacteristic = null;
        ConnectionStatusChanged?.Invoke(this, false);
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
            _adapter.DeviceConnected -= OnDeviceConnected;
            _adapter.DeviceDisconnected -= OnDeviceDisconnected;
            
            if (_connectedDevice != null)
            {
                _ = DisconnectAsync(); // Fire and forget
            }
        }

        _disposed = true;
    }
}
