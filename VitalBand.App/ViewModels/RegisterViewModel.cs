using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class RegisterViewModel(ApiService api) : BaseViewModel
{
    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirm = "";
    [ObservableProperty] private bool accepted;

    [ObservableProperty] private string firstNameError = "";
    [ObservableProperty] private string lastNameError = "";
    [ObservableProperty] private string emailError = "";
    [ObservableProperty] private string passwordError = "";
    [ObservableProperty] private string confirmError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTermsError))]
    private string termsError = "";

    public bool HasTermsError => !string.IsNullOrEmpty(TermsError);

    [RelayCommand]
    private async Task RegisterAsync()
    {
        const string required = "Ce champ est obligatoire.";
        FirstNameError = string.IsNullOrWhiteSpace(FirstName) ? required : "";
        LastNameError = string.IsNullOrWhiteSpace(LastName) ? required : "";
        EmailError = string.IsNullOrWhiteSpace(Email) ? required
            : (!Email.Contains('@') || !Email.Contains('.') ? "Adresse e-mail invalide." : "");
        PasswordError = string.IsNullOrWhiteSpace(Password) ? required
            : (Password.Length < 6 ? "6 caractères minimum." : "");
        ConfirmError = string.IsNullOrWhiteSpace(Confirm) ? required
            : (Confirm != Password ? "Les mots de passe ne correspondent pas." : "");
        TermsError = Accepted ? "" : "Vous devez accepter les conditions.";

        if (new[] { FirstNameError, LastNameError, EmailError, PasswordError, ConfirmError, TermsError }
            .Any(e => e != "")) return;

        IsBusy = true;
        var (data, error) = await api.RegisterAsync(FirstName.Trim(), LastName.Trim(), Email.Trim(), Password);
        IsBusy = false;

        if (data is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        await Shell.Current.GoToAsync("//home");
    }

    [RelayCommand]
    private Task GoLoginAsync() => Shell.Current.GoToAsync("..");
}
