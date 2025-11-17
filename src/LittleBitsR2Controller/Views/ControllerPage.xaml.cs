using LittleBitsR2Controller.Controls;
using LittleBitsR2Controller.ViewModels;

namespace LittleBitsR2Controller.Views;

public partial class ControllerPage : ContentPage
{
    private readonly ControllerViewModel _viewModel;

    public ControllerPage(ControllerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        
        // Wire up joystick events
        Joystick.PositionChanged += OnJoystickPositionChanged;
        Joystick.Released += OnJoystickReleased;
    }

    private async void OnJoystickPositionChanged(object? sender, JoystickEventArgs e)
    {
        await _viewModel.OnJoystickPositionChanged(e.Drive, e.Turn);
    }

    private async void OnJoystickReleased(object? sender, EventArgs e)
    {
        await _viewModel.OnJoystickReleased();
    }
}
