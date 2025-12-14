using Game2048.Core;
using Game2048.Maui.ViewModels;
using Game2048.Maui.Views;

namespace Game2048.Maui;

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

        // Register services
        builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
        builder.Services.AddSingleton<GameConfig>(sp => new GameConfig { Size = 4, WinTile = 2048, AllowContinueAfterWin = true });

        // Register ViewModels
        builder.Services.AddTransient<GameViewModel>();

        // Register Views
        builder.Services.AddTransient<GamePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
