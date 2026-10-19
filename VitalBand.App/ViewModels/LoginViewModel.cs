using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class LoginViewModel(ApiService api) : BaseViewModel
{
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string emailError = "";
    [ObservableProperty] private string passwordError = "";

    [RelayCommand]
    private async Task LoginAsync()
    {
        const string required = "Ce champ est obligatoire.";
        EmailError = string.IsNullOrWhiteSpace(Email) ? required : "";
        PasswordError = string.IsNullOrWhiteSpace(Password) ? required : "";
        if (EmailError != "" || PasswordError != "") return;

        IsBusy = true;
        var (data, error) = await api.LoginAsync(Email.Trim(), Password);
        IsBusy = false;

        if (data is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        await Shell.Current.GoToAsync("//home");
    }

    [RelayCommand]
    private Task GoRegisterAsync() => Shell.Current.GoToAsync("register");
}
