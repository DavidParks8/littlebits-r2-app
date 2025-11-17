using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LittleBitsR2Controller.Services;
using System.Collections.ObjectModel;

namespace LittleBitsR2Controller.ViewModels;

public partial class ControllerViewModel : ObservableObject, IDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;
    private DateTime _lastCommandTime = DateTime.MinValue;
    private const int CommandThrottleMs = 150;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private BluetoothDevice? _selectedDevice;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ObservableCollection<BluetoothDevice> Devices { get; } = new();

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
    private async Task ForwardAsync()
    {
        await SendDriveCommandAsync(1.0, 0.0);
    }

    [RelayCommand]
    private async Task BackwardAsync()
    {
        await SendDriveCommandAsync(-1.0, 0.0);
    }

    [RelayCommand]
    private async Task TurnLeftAsync()
    {
        await SendDriveCommandAsync(0.5, -1.0);
    }

    [RelayCommand]
    private async Task TurnRightAsync()
    {
        await SendDriveCommandAsync(0.5, 1.0);
    }

    [RelayCommand]
    private async Task PlaySoundAsync(string soundName)
    {
        try
        {
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await _bluetoothService.SendSoundCommandAsync(soundName, token);
            StatusMessage = $"Playing sound: {soundName}";
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
            // Throttle commands to prevent overloading the control hub
            // Skip commands that come too quickly instead of blocking the UI
            var timeSinceLastCommand = (DateTime.Now - _lastCommandTime).TotalMilliseconds;
            if (timeSinceLastCommand < CommandThrottleMs)
            {
                // Ignore this command to keep UI responsive
                return;
            }

            _lastCommandTime = DateTime.Now;
            var token = _cancellationTokenSource?.Token ?? CancellationToken.None;
            await _bluetoothService.SendDriveCommandAsync(speed, turn, token);
            StatusMessage = $"Driving: speed={speed:F1}, turn={turn:F1}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sending command: {ex.Message}";
        }
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        IsConnected = isConnected;
        StatusMessage = isConnected ? "Connected" : "Disconnected";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _bluetoothService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}
