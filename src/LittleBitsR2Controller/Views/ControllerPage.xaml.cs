using LittleBitsR2Controller.ViewModels;

namespace LittleBitsR2Controller.Views;

public partial class ControllerPage : ContentPage
{
    public ControllerPage(ControllerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
