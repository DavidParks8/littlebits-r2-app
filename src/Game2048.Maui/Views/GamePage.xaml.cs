using Game2048.Maui.ViewModels;

namespace Game2048.Maui.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _viewModel;

    public GamePage(GameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Add keyboard support
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Focus the page to receive keyboard events
        this.Focus();
    }

    protected override bool OnKeyDown(string keyName)
    {
        // Handle keyboard input for desktop platforms
        switch (keyName)
        {
            case "Up":
            case "W":
                _viewModel.MoveCommand.Execute("Up");
                return true;
            case "Down":
            case "S":
                _viewModel.MoveCommand.Execute("Down");
                return true;
            case "Left":
            case "A":
                _viewModel.MoveCommand.Execute("Left");
                return true;
            case "Right":
            case "D":
                _viewModel.MoveCommand.Execute("Right");
                return true;
            default:
                return base.OnKeyDown(keyName);
        }
    }
}
