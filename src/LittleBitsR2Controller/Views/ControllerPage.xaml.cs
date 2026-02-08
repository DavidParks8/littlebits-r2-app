using LittleBitsR2Controller.ViewModels;

namespace LittleBitsR2Controller.Views;

public partial class ControllerPage : ContentPage
{
    public ControllerPage(ControllerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ControllerViewModel vm && !vm.IsConnected && !vm.IsScanning)
        {
            await vm.ScanForDevicesCommand.ExecuteAsync(null);
        }
    }
}
