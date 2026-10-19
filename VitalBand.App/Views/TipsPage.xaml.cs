using VitalBand.App.Models;
using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class TipsPage : ContentPage
{
    private readonly TipsViewModel _vm;

    public TipsPage(TipsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnFaqTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is FaqItem item)
            item.IsOpen = !item.IsOpen;
    }
}
