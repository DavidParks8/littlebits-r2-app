using Plugin.BLE;
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
    
    // R2D2 Service and Characteristic UUIDs based on reverse engineering
    // These UUIDs are typical for BLE devices, may need adjustment based on actual device
    private const string ServiceUuid = "0000ffe0-0000-1000-8000-00805f9b34fb";
    private const string CharacteristicUuid = "0000ffe1-0000-1000-8000-00805f9b34fb";
    
    public event EventHandler<bool>? ConnectionStatusChanged;
    
    public bool IsConnected => _connectedDevice?.State == Plugin.BLE.Abstractions.DeviceState.Connected;

    public BluetoothService()
    {
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        
        _adapter.DeviceConnected += OnDeviceConnected;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
    }

    public Task<bool> IsBluetoothEnabledAsync()
    {
        return Task.FromResult(_bluetoothLE.IsOn);
    }

    public async Task<IEnumerable<BluetoothDevice>> ScanForDevicesAsync()
    {
        var devices = new List<BluetoothDevice>();
        
        if (!await IsBluetoothEnabledAsync())
        {
            return devices;
        }

        var scanResults = new List<IDevice>();
        
        _adapter.DeviceDiscovered += (s, e) =>
        {
            if (e.Device != null && !string.IsNullOrEmpty(e.Device.Name))
            {
                scanResults.Add(e.Device);
            }
        };

        await _adapter.StartScanningForDevicesAsync();
        await Task.Delay(5000); // Scan for 5 seconds
        await _adapter.StopScanningForDevicesAsync();

        foreach (var device in scanResults)
        {
            if (!string.IsNullOrEmpty(device.Name))
            {
                devices.Add(new BluetoothDevice(device.Id.ToString(), device.Name));
            }
        }

        return devices;
    }

    public async Task<bool> ConnectToDeviceAsync(BluetoothDevice device)
    {
        try
        {
            var deviceId = Guid.Parse(device.Id);
            var bleDevice = await _adapter.ConnectToKnownDeviceAsync(deviceId);
            
            if (bleDevice == null)
            {
                return false;
            }

            _connectedDevice = bleDevice;

            // Get the service and characteristic
            var services = await bleDevice.GetServicesAsync();
            var service = services.FirstOrDefault(s => s.Id.ToString().Equals(ServiceUuid, StringComparison.OrdinalIgnoreCase));
            
            if (service != null)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                _writeCharacteristic = characteristics.FirstOrDefault(c => 
                    c.Id.ToString().Equals(CharacteristicUuid, StringComparison.OrdinalIgnoreCase));
            }

            return IsConnected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice != null)
        {
            await _adapter.DisconnectDeviceAsync(_connectedDevice);
            _connectedDevice = null;
            _writeCharacteristic = null;
        }
    }

    public async Task SendCommandAsync(R2D2Command command)
    {
        if (_writeCharacteristic == null || !IsConnected)
        {
            return;
        }

        try
        {
            // Based on the reverse-engineered protocol from meetar/littlebits-r2d2-controls
            // Commands are sent as byte arrays
            byte[] data = command switch
            {
                R2D2Command.Stop => new byte[] { 0x00 },
                R2D2Command.Forward => new byte[] { 0x01, 0xFF }, // Direction + Speed
                R2D2Command.Backward => new byte[] { 0x02, 0xFF },
                R2D2Command.TurnLeft => new byte[] { 0x03, 0xFF },
                R2D2Command.TurnRight => new byte[] { 0x04, 0xFF },
                R2D2Command.HeadLeft => new byte[] { 0x05 },
                R2D2Command.HeadRight => new byte[] { 0x06 },
                R2D2Command.HeadCenter => new byte[] { 0x07 },
                _ => new byte[] { 0x00 }
            };

            await _writeCharacteristic.WriteAsync(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending command: {ex.Message}");
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
