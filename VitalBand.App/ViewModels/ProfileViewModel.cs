using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class ProfileViewModel(ApiService api, AvatarService avatar, IReminderScheduler scheduler) : BaseViewModel
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string initials = "";
    [ObservableProperty] private ImageSource? avatarSource;

    public void Refresh()
    {
        Name = api.FullName;
        Initials = api.Initials;
        AvatarSource = avatar.Load();
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        var choice = await Shell.Current.DisplayActionSheetAsync("Photo de profil", "Annuler", null,
            "Choisir dans la galerie", "Supprimer la photo");
        if (choice == "Choisir dans la galerie")
        {
            try
            {
                if (await avatar.PickAsync())
                {
                    Refresh();
                    _ = ShowToastAsync("Photo mise à jour.");
                }
            }
            catch (Exception)
            {
                _ = ShowToastAsync("Impossible d’ouvrir la galerie.");
            }
        }
        else if (choice == "Supprimer la photo")
        {
            avatar.Remove();
            Refresh();
        }
    }

    [RelayCommand] private Task GoTipsAsync() => Shell.Current.GoToAsync("tips");
    [RelayCommand] private Task GoThresholdsAsync() => Shell.Current.GoToAsync("thresholds");
    [RelayCommand] private Task GoExportAsync() => Shell.Current.GoToAsync("export");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var yes = await Shell.Current.DisplayAlertAsync("Déconnexion", "Voulez-vous vous déconnecter ?", "Oui", "Non");
        if (!yes) return;
        scheduler.CancelAll();   // plus de rappels pour ce compte
        api.Logout();
        await Shell.Current.GoToAsync("//welcome");
    }
}
