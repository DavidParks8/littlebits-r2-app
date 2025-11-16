namespace LittleBitsR2Controller.Services;

public interface IBluetoothService
{
    Task<bool> IsBluetoothEnabledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BluetoothDevice>> ScanForDevicesAsync(CancellationToken cancellationToken = default);
    Task<bool> ConnectToDeviceAsync(BluetoothDevice device, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendDriveCommandAsync(double speed, double turn, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectionStatusChanged;
}

public record BluetoothDevice(string Id, string Name);
