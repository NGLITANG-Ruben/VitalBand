using VitalBand.App.Services;

namespace VitalBand.App.Views;

public partial class WelcomePage : ContentPage
{
    private readonly ApiService _api;

    public WelcomePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Session déjà ouverte : on passe directement à l'accueil
        if (await _api.HasSessionAsync())
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnStart(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("login");
}
