using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LittleBitsR2Controller.Services;
using System.Collections.ObjectModel;

namespace LittleBitsR2Controller.ViewModels;

public partial class ControllerViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService;

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
    }

    [RelayCommand]
    private async Task ScanForDevicesAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning for devices...";
        Devices.Clear();

        try
        {
            if (!await _bluetoothService.IsBluetoothEnabledAsync())
            {
                StatusMessage = "Bluetooth is not enabled";
                return;
            }

            var devices = await _bluetoothService.ScanForDevicesAsync();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            StatusMessage = $"Found {Devices.Count} device(s)";
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
            var success = await _bluetoothService.ConnectToDeviceAsync(SelectedDevice);
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
            await _bluetoothService.DisconnectAsync();
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
        await SendCommandAsync(R2D2Command.Stop);
    }

    [RelayCommand]
    private async Task ForwardAsync()
    {
        await SendCommandAsync(R2D2Command.Forward);
    }

    [RelayCommand]
    private async Task BackwardAsync()
    {
        await SendCommandAsync(R2D2Command.Backward);
    }

    [RelayCommand]
    private async Task TurnLeftAsync()
    {
        await SendCommandAsync(R2D2Command.TurnLeft);
    }

    [RelayCommand]
    private async Task TurnRightAsync()
    {
        await SendCommandAsync(R2D2Command.TurnRight);
    }

    [RelayCommand]
    private async Task HeadLeftAsync()
    {
        await SendCommandAsync(R2D2Command.HeadLeft);
    }

    [RelayCommand]
    private async Task HeadRightAsync()
    {
        await SendCommandAsync(R2D2Command.HeadRight);
    }

    [RelayCommand]
    private async Task HeadCenterAsync()
    {
        await SendCommandAsync(R2D2Command.HeadCenter);
    }

    private async Task SendCommandAsync(R2D2Command command)
    {
        try
        {
            await _bluetoothService.SendCommandAsync(command);
            StatusMessage = $"Command sent: {command}";
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
}
