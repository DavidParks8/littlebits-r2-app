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
    }
}
