using VitalBand.App.ViewModels;

namespace VitalBand.App.Views;

public partial class ReminderEntryPage : ContentPage
{
    public ReminderEntryPage(ReminderEntryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void OnDayClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.BindingContext is DayChip chip)
            chip.IsOn = !chip.IsOn;
    }
}
