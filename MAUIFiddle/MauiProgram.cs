using Microsoft.Extensions.Logging;

namespace MAUIFiddle
{
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
					fonts.AddFont("MPLUSCodeLatin-VariableFont.ttf", "MPLUSCodeLatin");
                    fonts.AddFont("SourceCodePro-VariableFont.ttf", "SourceCodePro");
				});

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if WINDOWS
    builder.ConfigureMauiHandlers(handlers =>
    {
        handlers.AddHandler(typeof(Editor), typeof(MAUIFiddle.NoWrapEditorHandler));
    });
#endif

#if ANDROID
    builder.ConfigureMauiHandlers(handlers =>
    {
        handlers.AddHandler(typeof(Editor), typeof(MAUIFiddle.NoWrapEditorHandler));
    });
#endif

			return builder.Build();
        }
    }
}
