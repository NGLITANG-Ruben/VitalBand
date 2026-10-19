using VitalBand.App.Services;

namespace VitalBand.App.Views;

public partial class WelcomePage : ContentPage
{
    private readonly ApiService _api;

    public WelcomePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        UpdateServerLabel();
    }

    private void UpdateServerLabel() =>
        ServerLabel.Text = $"Serveur : {_api.ServerUrl}  ·  modifier";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Session déjà ouverte : on passe directement à l'accueil
        if (await _api.HasSessionAsync())
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnStart(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("login");

    // Permet de pointer l'application vers l'API (autre PC, réseau Wi-Fi, serveur en ligne...)
    private async void OnServerTapped(object? sender, TappedEventArgs e)
    {
        var input = await DisplayPromptAsync("Adresse du serveur",
            "Adresse de l’API VitalBand (ex. http://192.168.1.10:5000)",
            accept: "Enregistrer", cancel: "Annuler", keyboard: Keyboard.Url, initialValue: _api.ServerUrl);
        if (input is null) return;
        if (_api.SetServerUrl(input))
            UpdateServerLabel();
        else
            await DisplayAlertAsync("Adresse invalide", "Saisissez une adresse commençant par http:// ou https://", "OK");
    }
}
