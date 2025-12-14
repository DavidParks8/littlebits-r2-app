# LittleBits R2D2 Controller App

A cross-platform .NET MAUI application to control the unsupported LittleBits R2D2 robot via Bluetooth.

## Projects in This Repository

### 1. LittleBits R2D2 Controller
The original app to control the LittleBits R2D2 robot using Bluetooth Low Energy.

### 2. Game 2048
A modern .NET MAUI implementation of the classic 2048 puzzle game.

---

## LittleBits R2D2 Controller

### Overview

This application provides a virtual controller to control the LittleBits R2D2 robot using Bluetooth Low Energy (BLE). The Bluetooth protocol is based on reverse-engineered specifications from [meetar/littlebits-r2d2-controls](https://github.com/meetar/littlebits-r2d2-controls).

### Features

- Cross-platform support (Android, iOS, macOS, Windows)
- Bluetooth Low Energy connectivity
- Virtual controller with movement controls:
  - Forward/Backward movement
  - Left/Right turning
  - Head movement controls
  - Stop command
- Device scanning and connection management

---

## Game 2048

### Overview

A cross-platform implementation of the popular 2048 puzzle game built with .NET MAUI. Features a clean, testable core engine and a polished user interface with animations and gesture support.

### Features

- **Classic 2048 Gameplay**: Merge tiles to reach 2048 (and beyond!)
- **Cross-platform**: Runs on Android, iOS, and Windows
- **Modern UI**: Clean design with smooth animations
- **Input Support**:
  - Touch: Swipe gestures on mobile/tablet
  - Keyboard: Arrow keys + WASD on desktop
- **Persistence**: Saves game state and best score
- **Undo/Redo**: Full undo/redo support with history
- **Accessibility**: Light/dark theme support

### Architecture

The 2048 game is structured into three main components:

1. **Game2048.Core** (`src/Game2048.Core/`): 
   - Pure .NET library with no UI dependencies
   - Fully testable game engine
   - Implements classic 2048 rules with configurable board size
   - Supports undo/redo, serialization, and game state management

2. **Game2048.Maui** (`src/Game2048.Maui/`):
   - .NET MAUI application
   - MVVM architecture using CommunityToolkit.Mvvm
   - Responsive UI that adapts to different screen sizes
   - Gesture and keyboard input support

3. **Game2048.Core.Tests** (`tests/Game2048.Core.Tests/`):
   - Comprehensive unit tests for game engine
   - Tests for move mechanics, merge logic, win/lose conditions
   - Deterministic random testing for spawn behavior

---

## Technology Stack

- .NET 10
- .NET MAUI (Multi-platform App UI)
- Central Package Management (Directory.Packages.props)
- CommunityToolkit.Mvvm for modern MVVM patterns with source generators
- Dependency Injection (built-in Microsoft.Extensions.DependencyInjection)
- MSTest for testing

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

Game2048.Core/
├── GameEngine.cs      # Main game logic
├── GameState.cs       # Game state representation
├── GameConfig.cs      # Configuration
└── IRandomSource.cs   # RNG abstraction for testing

Game2048.Maui/
├── ViewModels/        # MVVM view models
├── Views/             # XAML UI pages
├── Converters/        # Value converters for data binding
├── Platforms/         # Platform-specific code
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

# Build the entire solution
dotnet build

# Build specific projects
dotnet build src/Game2048.Core/Game2048.Core.csproj
dotnet build src/Game2048.Maui/Game2048.Maui.csproj

# Build for specific platform (MAUI apps only, on appropriate OS)
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-android
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-ios
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-windows10.0.19041.0
```

### Running Tests

```bash
# Run all tests in the solution
dotnet build tests/Game2048.Core.Tests/Game2048.Core.Tests.csproj
./tests/Game2048.Core.Tests/bin/Debug/net10.0/Game2048.Core.Tests

# Run tests for a specific project
dotnet build tests/LittleBitsR2Controller.Tests/LittleBitsR2Controller.Tests.csproj
./tests/LittleBitsR2Controller.Tests/bin/Debug/net10.0/LittleBitsR2Controller.Tests
```

### Running the Game 2048 MAUI App

#### Android
```bash
# Build and deploy to connected device/emulator
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-android -t:Run
```

#### Windows
```bash
# Build and run on Windows
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

#### iOS
```bash
# Requires macOS with Xcode
dotnet build src/Game2048.Maui/Game2048.Maui.csproj -f net10.0-ios -t:Run
```

**Note**: iOS deployment requires a Mac with Xcode and proper provisioning profiles.

## Usage

### R2D2 Controller

1. Launch the application on your device
2. Tap "Scan for Devices" to search for nearby R2D2 robots
   - Only devices with names starting with "w32" will be shown
   - If only one device is found, it will auto-connect
   - A loading indicator will show while scanning
3. If multiple devices are found, select your R2D2 device from the list and tap "Connect"
4. Use the virtual controller buttons to control your robot:
   - Forward/Backward: Move the robot
   - Turn Left/Right: Rotate the robot
   - Stop: Immediately stop all movement
   - Note: Movement buttons are only enabled when connected

### Game 2048

1. Launch the Game 2048 app on your device
2. Use swipe gestures (mobile/tablet) or arrow keys/WASD (desktop) to move tiles
3. Merge tiles with the same number to create larger numbers
4. Try to reach the 2048 tile to win!
5. Features:
   - **New Game**: Start a fresh game
   - **Undo**: Take back your last move
   - **Redo**: Redo an undone move
   - **Auto-save**: Your game progress is saved automatically

## Permissions

### Android (R2D2 Controller)
- Bluetooth
- Bluetooth Admin
- Bluetooth Scan
- Bluetooth Connect
- Location (required for Bluetooth scanning)

### Android (Game 2048)
- No special permissions required

### iOS/macOS (R2D2 Controller)
- Bluetooth usage descriptions in Info.plist

### iOS/macOS (Game 2048)
- No special permissions required

### Windows
- R2D2 Controller: Bluetooth device capability
- Game 2048: No special permissions required

## License

See LICENSE file for details.
