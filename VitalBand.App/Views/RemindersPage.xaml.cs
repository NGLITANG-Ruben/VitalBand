using VitalBand.App.Models;
using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class RemindersPage : ContentPage
{
    private readonly RemindersViewModel _vm;

    public RemindersPage(RemindersViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is Switch sw && sw.BindingContext is ReminderItem item)
            await _vm.SetActiveAsync(item, e.Value);
    }
}
