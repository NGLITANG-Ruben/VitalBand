using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class ChartsPage : ContentPage
{
    private readonly ChartsViewModel _vm;

    public ChartsPage(ChartsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        ChartView.Drawable = vm.Chart;
        vm.ChartUpdated += () => ChartView.Invalidate();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
