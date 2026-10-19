using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class ExportPage : ContentPage
{
    private readonly ExportViewModel _vm;

    public ExportPage(ExportViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshPreviewAsync();
    }
}
