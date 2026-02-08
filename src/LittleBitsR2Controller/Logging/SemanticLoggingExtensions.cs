using Microsoft.Extensions.Logging;

namespace LittleBitsR2Controller.Logging;

/// <summary>
/// Centralized logging extension methods using LoggerMessage source generation.
/// </summary>
public static partial class SemanticLoggingExtensions
{
    // Bluetooth device scanning and discovery
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "[BLE] Skipping paired device with non-R2D2 name: '{DeviceName}' ({DeviceId})")]
    public static partial void LogSkippingPairedDevice(this ILogger logger, string deviceName, string deviceId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "[BLE] Paired device found: {DeviceName} ({DeviceId})")]
    public static partial void LogPairedDeviceFound(this ILogger logger, string deviceName, string deviceId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "[BLE] Error checking paired devices: {ErrorMessage}")]
    public static partial void LogErrorCheckingPairedDevices(this ILogger logger, string errorMessage);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "[BLE] Scan discovered: Name='{DeviceName}', Id={DeviceId}")]
    public static partial void LogScanDiscovered(this ILogger logger, string? deviceName, Guid deviceId);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "[BLE] Scan complete. Found {DeviceCount} R2D2 device(s) out of {ScannedCount} scanned.")]
    public static partial void LogScanComplete(this ILogger logger, int deviceCount, int scannedCount);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "[BLE] No devices found with R2D2 service UUID. Running unfiltered scan...")]
    public static partial void LogRunningUnfilteredScan(this ILogger logger);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Debug,
        Message = "[BLE] Unfiltered device: Name='{DeviceName}', Id={DeviceId}")]
    public static partial void LogUnfilteredDevice(this ILogger logger, string deviceName, string deviceId);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Information,
        Message = "[BLE] Unfiltered scan complete. Showing {DeviceCount} named device(s).")]
    public static partial void LogUnfilteredScanComplete(this ILogger logger, int deviceCount);

    // Connection and characteristic configuration
    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Debug,
        Message = "[BLE] WriteWithoutResponse not supported, using default write type.")]
    public static partial void LogWriteWithoutResponseNotSupported(this ILogger logger);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Error,
        Message = "Error connecting to device: {ErrorMessage}")]
    public static partial void LogErrorConnectingToDevice(this ILogger logger, string errorMessage);

    // Command errors
    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Error,
        Message = "Error sending command: {ErrorMessage}")]
    public static partial void LogErrorSendingCommand(this ILogger logger, string errorMessage);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Error,
        Message = "Error sending stop command: {ErrorMessage}")]
    public static partial void LogErrorSendingStopCommand(this ILogger logger, string errorMessage);

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Error,
        Message = "Error sending sound command: {ErrorMessage}")]
    public static partial void LogErrorSendingSoundCommand(this ILogger logger, string errorMessage);

    // Connection status changes
    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Information,
        Message = "[BLE] Unexpected disconnect. Starting auto-reconnect...")]
    public static partial void LogUnexpectedDisconnect(this ILogger logger);

    // Health check and reconnection
    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Debug,
        Message = "[BLE] Connection health monitor started.")]
    public static partial void LogHealthCheckStarted(this ILogger logger);

    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Warning,
        Message = "[BLE] Health check failure #{FailureCount}: {Reason}")]
    public static partial void LogHealthCheckFailure(this ILogger logger, int failureCount, string reason);

    [LoggerMessage(
        EventId = 1017,
        Level = LogLevel.Warning,
        Message = "[BLE] Device presumed offline. Triggering disconnect/reconnect.")]
    public static partial void LogDevicePresumedOffline(this ILogger logger);

    [LoggerMessage(
        EventId = 1018,
        Level = LogLevel.Information,
        Message = "[BLE] Reconnect attempt {Attempt}/{MaxAttempts} to {DeviceName}...")]
    public static partial void LogReconnectAttempt(this ILogger logger, int attempt, int maxAttempts, string deviceName);

    [LoggerMessage(
        EventId = 1019,
        Level = LogLevel.Debug,
        Message = "[BLE] WriteWithoutResponse not supported on reconnect.")]
    public static partial void LogWriteWithoutResponseNotSupportedOnReconnect(this ILogger logger);

    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Information,
        Message = "[BLE] Reconnected to {DeviceName} after {AttemptCount} attempt(s).")]
    public static partial void LogReconnected(this ILogger logger, string deviceName, int attemptCount);

    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Warning,
        Message = "[BLE] Reconnect attempt {Attempt} failed: {ErrorMessage}")]
    public static partial void LogReconnectAttemptFailed(this ILogger logger, int attempt, string errorMessage);

    [LoggerMessage(
        EventId = 1022,
        Level = LogLevel.Warning,
        Message = "[BLE] Auto-reconnect gave up after max attempts.")]
    public static partial void LogReconnectGaveUp(this ILogger logger);
}
