using LittleBitsR2Controller.Services;
using LittleBitsR2Controller.ViewModels;
using LittleBitsR2Controller.Views;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace LittleBitsR2Controller;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Bluetooth dependencies
		builder.Services.AddSingleton<IBluetoothLE>(_ => CrossBluetoothLE.Current);
		builder.Services.AddSingleton<IAdapter>(_ => CrossBluetoothLE.Current.Adapter);
		
		// Register Services
		builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
		
		// Register ViewModels
		builder.Services.AddSingleton<ControllerViewModel>();
		
		// Register Views
		builder.Services.AddSingleton<ControllerPage>();

		return builder.Build();
	}
}
