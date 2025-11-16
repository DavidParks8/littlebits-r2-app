# LittleBits R2D2 Controller App

A cross-platform .NET MAUI application to control the unsupported LittleBits R2D2 robot via Bluetooth.

## Overview

This application provides a virtual controller to control the LittleBits R2D2 robot using Bluetooth Low Energy (BLE). The Bluetooth protocol is based on reverse-engineered specifications from [meetar/littlebits-r2d2-controls](https://github.com/meetar/littlebits-r2d2-controls).

## Features

- Cross-platform support (Android, iOS, macOS, Windows)
- Bluetooth Low Energy connectivity
- Virtual controller with movement controls:
  - Forward/Backward movement
  - Left/Right turning
  - Head movement controls
  - Stop command
- Device scanning and connection management

## Technology Stack

- .NET 10
- .NET MAUI (Multi-platform App UI)
- Central Package Management (Directory.Packages.props)
- CommunityToolkit.Mvvm for modern MVVM patterns with source generators
- Dependency Injection (built-in Microsoft.Extensions.DependencyInjection)
- Plugin.BLE for cross-platform Bluetooth support

## Modern Development Patterns

This application demonstrates modern .NET development practices:

- **Dependency Injection**: All services and view models are registered in the DI container
- **MVVM with Source Generators**: Uses CommunityToolkit.Mvvm with `[ObservableProperty]` and `[RelayCommand]` attributes for boilerplate-free code
- **Interface-based Design**: Services are defined through interfaces for testability and maintainability
- **Async/Await**: All I/O operations use modern async patterns
- **Disposal Pattern**: Proper resource cleanup with IDisposable implementation
- **Central Package Management**: All NuGet package versions managed centrally for consistency

## Project Structure

```
LittleBitsR2Controller/
├── Services/           # Bluetooth service implementation
├── ViewModels/        # MVVM view models
├── Views/             # XAML UI pages
├── Converters/        # Value converters for data binding
├── Platforms/         # Platform-specific code
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
└── Resources/         # App resources (images, fonts, styles)
```

## Building the Application

### Prerequisites

- .NET 10 SDK
- MAUI workload installed: `sudo dotnet workload install maui-android` (or platform-specific workload)
- For Android: Android SDK
- For iOS/Mac: Xcode
- For Windows: Visual Studio 2022 with MAUI workload

### Build Commands

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Build for specific platform (on appropriate OS)
dotnet build -f net10.0-android
dotnet build -f net10.0-ios
dotnet build -f net10.0-maccatalyst
dotnet build -f net10.0-windows10.0.19041.0
```

## Usage

1. Launch the application on your device
2. Tap "Scan for Devices" to search for nearby R2D2 robots
3. Select your R2D2 device from the list
4. Tap "Connect" to establish Bluetooth connection
5. Use the virtual controller buttons to control your robot:
   - Forward/Backward: Move the robot
   - Turn Left/Right: Rotate the robot
   - Head controls: Move R2D2's head
   - Stop: Immediately stop all movement

## Permissions

### Android
- Bluetooth
- Bluetooth Admin
- Bluetooth Scan
- Bluetooth Connect
- Location (required for Bluetooth scanning)

### iOS/macOS
- Bluetooth usage descriptions in Info.plist

### Windows
- Bluetooth device capability

## License

See LICENSE file for details.
