using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class EntryPage : ContentPage
{
    public EntryPage(EntryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
