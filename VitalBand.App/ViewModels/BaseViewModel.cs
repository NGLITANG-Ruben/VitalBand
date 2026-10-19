using CommunityToolkit.Mvvm.ComponentModel;

namespace VitalBand.App.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string toastMessage = "";
    [ObservableProperty] private bool toastVisible;

    protected async Task ShowToastAsync(string message)
    {
        ToastMessage = message;
        ToastVisible = true;
        await Task.Delay(3000);
        ToastVisible = false;
    }
}
