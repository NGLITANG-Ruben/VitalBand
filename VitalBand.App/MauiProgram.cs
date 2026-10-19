using Microsoft.Extensions.Logging;
using VitalBand.App.Services;
using VitalBand.App.ViewModels;
using VitalBand.App.Views;

namespace VitalBand.App;

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

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<DataService>();
        builder.Services.AddSingleton<AvatarService>();
        builder.Services.AddSingleton<HealthService>();
#if ANDROID
        builder.Services.AddSingleton<IReminderScheduler, AndroidReminderScheduler>();
        builder.Services.AddSingleton<IPdfExporter, AndroidPdfExporter>();
#else
        builder.Services.AddSingleton<IReminderScheduler, NoOpReminderScheduler>();
        builder.Services.AddSingleton<IPdfExporter, NoOpPdfExporter>();
#endif

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<EntryViewModel>();
        builder.Services.AddTransient<ChartsViewModel>();
        builder.Services.AddTransient<ThresholdsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<RemindersViewModel>();
        builder.Services.AddTransient<ReminderEntryViewModel>();
        builder.Services.AddTransient<ExportViewModel>();
        builder.Services.AddTransient<TipsViewModel>();

        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<EntryPage>();
        builder.Services.AddTransient<ChartsPage>();
        builder.Services.AddTransient<ThresholdsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<RemindersPage>();
        builder.Services.AddTransient<ReminderEntryPage>();
        builder.Services.AddTransient<ExportPage>();
        builder.Services.AddTransient<TipsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
