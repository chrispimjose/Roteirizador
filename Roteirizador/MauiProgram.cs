using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.Logging;
using Roteirizador.Services;

namespace Roteirizador
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
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Services
            // Registra um serviço para criar instâncias como injeção
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ViaCepService>();
            builder.Services.AddSingleton<GeocodingService>();

            // Pages
            // Registra a classe MainPage como um singleton no contêiner de DI.
            builder.Services.AddSingleton<MainPage>();
            // Uma nova instância de MapPage é criada cada vez que ela é solicitada
            builder.Services.AddTransient<MapPage>();



            return builder.Build();
        }
    }
}
