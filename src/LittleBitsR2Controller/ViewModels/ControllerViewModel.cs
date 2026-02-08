using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LittleBitsR2Controller.Services;
using LittleBitsR2Controller.Views;
using System.Collections.ObjectModel;

namespace LittleBitsR2Controller.ViewModels;

public partial class ControllerViewModel : ObservableObject, IDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;
    private DateTime _lastCommandTime = DateTime.MinValue;
    private const int CommandThrottleMs = 150;
    private System.Threading.Timer? _safetyTimer;
    private bool _hasActiveCommand;
    private CancellationTokenSource? _throttleCts;
    private bool _joystickEngaged;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isReconnecting;

    [ObservableProperty]
    private BluetoothDevice? _selectedDevice;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _showDeviceSelector;

    public ObservableCollection<BluetoothDevice> Devices { get; } = new();

    public IReadOnlyList<string> SoundEffectNames { get; } = [.. R2D2Protocol.SoundEffects.Keys];

    public ControllerViewModel(IBluetoothService bluetoothService)
    {
        _bluetoothService = bluetoothService;
        _bluetoothService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [RelayCommand]
    private async Task ScanForDevicesAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning for devices...";
        Devices.Clear();
        SelectedDevice = null;

        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            
            if (!await _bluetoothService.IsBluetoothEnabledAsync(token))
            {
                StatusMessage = "Bluetooth is not enabled";
                return;
            }

            var devices = await _bluetoothService.ScanForDevicesAsync(token);
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            StatusMessage = $"Found {Devices.Count} device(s)";
            ShowDeviceSelector = Devices.Count > 1;

            // Auto-connect if only one device is found
            if (Devices.Count == 1)
            {
                SelectedDevice = Devices[0];
                StatusMessage = "Auto-connecting to single device...";
                await ConnectAsync();
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedDevice == null)
            return;

        StatusMessage = $"Connecting to {SelectedDevice.Name}...";

        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            var success = await _bluetoothService.ConnectToDeviceAsync(SelectedDevice, token);
            if (success)
            {
                IsConnected = true;
                StatusMessage = $"Connected to {SelectedDevice.Name}";
            }
            else
            {
                StatusMessage = $"Failed to connect to {SelectedDevice.Name}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Connection cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error connecting: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        StatusMessage = "Disconnecting...";

        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await _bluetoothService.DisconnectAsync(token);
            IsConnected = false;
            StatusMessage = "Disconnected";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error disconnecting: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        _hasActiveCommand = false;

        // Cancel any deferred throttle command so it doesn't fire after the stop
        // and send a stale turn/drive value, which causes motor oscillation.
        _throttleCts?.Cancel();
        _throttleCts?.Dispose();
        _throttleCts = null;

        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await _bluetoothService.StopAsync(token);
            StatusMessage = "Stopped";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sending stop command: {ex.Message}";
        }
    }

    [RelayCommand]
    private Task JoystickMovedAsync(JoystickPosition? position)
    {
        if (position == null) return Task.CompletedTask;
        _joystickEngaged = true;
        return SendDriveCommandAsync(position.Speed, position.Turn);
    }

    [RelayCommand]
    private async Task JoystickReleasedAsync()
    {
        _joystickEngaged = false;
        _hasActiveCommand = false;
        await StopAsync();
    }

    [RelayCommand]
    private async Task PlaySoundAsync(string? soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await _bluetoothService.SendSoundCommandAsync(soundName, token);
            StatusMessage = $"Playing: {soundName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing sound: {ex.Message}";
        }
    }

    private async Task SendDriveCommandAsync(double speed, double turn)
    {
        try
        {
            // Trailing-edge throttle: if within cooldown, schedule the command
            // to fire after the remaining cooldown. This ensures the last command
            // is always delivered (matching the original JS throttle).
            var now = DateTime.Now;
            var timeSinceLastCommand = (now - _lastCommandTime).TotalMilliseconds;

            if (timeSinceLastCommand < CommandThrottleMs)
            {
                // Cancel any previously deferred command
                _throttleCts?.Cancel();
                _throttleCts?.Dispose();
                _throttleCts = new CancellationTokenSource();
                var deferToken = _throttleCts.Token;
                var delayMs = (int)(CommandThrottleMs - timeSinceLastCommand);

                // Fire-and-forget the deferred send
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delayMs, deferToken);
                        if (!deferToken.IsCancellationRequested)
                        {
                            await MainThread.InvokeOnMainThreadAsync(
                                () => ExecuteDriveCommandAsync(speed, turn));
                        }
                    }
                    catch (OperationCanceledException) { /* superseded by a newer command */ }
                });
                return;
            }

            await ExecuteDriveCommandAsync(speed, turn);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sending command: {ex.Message}";
        }
    }

    private async Task ExecuteDriveCommandAsync(double speed, double turn)
    {
        _lastCommandTime = DateTime.Now;
        _hasActiveCommand = true;
        var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
        await _bluetoothService.SendDriveCommandAsync(speed, turn, token);
        StatusMessage = $"Driving: speed={speed:F1}, turn={turn:F1}";
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = isConnected;
            IsReconnecting = _bluetoothService.IsReconnecting;

            if (isConnected)
            {
                StatusMessage = SelectedDevice != null
                    ? $"Connected to {SelectedDevice.Name}"
                    : "Connected";
            }
            else if (_bluetoothService.IsReconnecting)
            {
                StatusMessage = "Connection lost. Reconnecting...";
            }
            else
            {
                StatusMessage = "Disconnected";
            }
        });
    }

    partial void OnIsConnectedChanged(bool value)
    {
        if (value)
        {
            ShowDeviceSelector = false;
            _safetyTimer = new System.Threading.Timer(SafetyTimerCallback, null, 1000, 500);
        }
        else
        {
            _safetyTimer?.Dispose();
            _safetyTimer = null;
            _hasActiveCommand = false;
        }
    }

    private void SafetyTimerCallback(object? state)
    {
        if (!_hasActiveCommand || !IsConnected) return;

        // Don't auto-stop while the joystick is actively held — the device
        // retains its last commanded state, so no re-send is needed.
        if (_joystickEngaged) return;

        var timeSinceLastCommand = (DateTime.Now - _lastCommandTime).TotalMilliseconds;
        if (timeSinceLastCommand > 1000)
        {
            _hasActiveCommand = false;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
                    await _bluetoothService.StopAsync(token);
                    StatusMessage = "Auto-stopped (safety timeout)";
                }
                catch { /* Safety mechanism - swallow errors */ }
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _safetyTimer?.Dispose();
        _throttleCts?.Cancel();
        _throttleCts?.Dispose();
        _bluetoothService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}
