using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class ThresholdsPage : ContentPage
{
    private readonly ThresholdsViewModel _vm;

    public ThresholdsPage(ThresholdsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
