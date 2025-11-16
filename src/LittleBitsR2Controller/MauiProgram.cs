using LittleBitsR2Controller.Services;
using LittleBitsR2Controller.ViewModels;
using LittleBitsR2Controller.Views;
using Microsoft.Extensions.Logging;

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

		// Register Services
		builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
		
		// Register ViewModels
		builder.Services.AddSingleton<ControllerViewModel>();
		
		// Register Views
		builder.Services.AddSingleton<ControllerPage>();

		return builder.Build();
	}
}
