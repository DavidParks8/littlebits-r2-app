namespace LittleBitsR2Controller.Services;

public interface IBluetoothService
{
    Task<bool> IsBluetoothEnabledAsync();
    Task<IEnumerable<BluetoothDevice>> ScanForDevicesAsync();
    Task<bool> ConnectToDeviceAsync(BluetoothDevice device);
    Task DisconnectAsync();
    Task SendCommandAsync(R2D2Command command);
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectionStatusChanged;
}

public record BluetoothDevice(string Id, string Name);

public enum R2D2Command
{
    Stop,
    Forward,
    Backward,
    TurnLeft,
    TurnRight,
    HeadLeft,
    HeadRight,
    HeadCenter
}
