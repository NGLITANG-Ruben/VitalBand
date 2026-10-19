using VitalBand.App.Views;

namespace VitalBand.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("entry", typeof(EntryPage));
        Routing.RegisterRoute("thresholds", typeof(ThresholdsPage));
        Routing.RegisterRoute("reminder", typeof(ReminderEntryPage));
        Routing.RegisterRoute("export", typeof(ExportPage));
        Routing.RegisterRoute("tips", typeof(TipsPage));
    }
}
