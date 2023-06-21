using Hai.Data;
using Hai.Views;
using Hai.Platforms;
using Microsoft.Extensions.Logging;

namespace Hai;

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
		builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<TodoPage>();
        builder.Services.AddTransient<TodoPage>();

        builder.Services.AddSingleton<TodoItemDatabase>();

#if ANDROID || IOS || WINDOWS || MACCATALYST
        builder.Services.AddSingleton<ISpeechToText, SpeechToTextImplementation>();
#endif
		return builder.Build();
	}

}