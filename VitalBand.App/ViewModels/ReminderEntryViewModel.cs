using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class DayChip : ObservableObject
{
    public string Label { get; }
    public int Bit { get; }
    [ObservableProperty] private bool isOn;

    public DayChip(string label, int bit, bool on)
    {
        Label = label;
        Bit = bit;
        isOn = on;
    }
}

public partial class ReminderEntryViewModel(DataService data) : BaseViewModel, IQueryAttributable
{
    private int _id;

    public List<DayChip> Days { get; } =
        new[] { "L", "M", "M", "J", "V", "S", "D" }.Select((l, i) => new DayChip(l, i, true)).ToList();

    [ObservableProperty] private string title = "Nouveau rappel";
    [ObservableProperty] private bool isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGlycemia))]
    private bool isBp = true;
    public bool IsGlycemia => !IsBp;

    [ObservableProperty] private string timeText = "08:00";
    [ObservableProperty] private string timeError = "";
    [ObservableProperty] private bool isActive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDaysError))]
    private string daysError = "";
    public bool HasDaysError => !string.IsNullOrEmpty(DaysError);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("item", out var o) || o is not Reminder r) return;
        _id = r.Id;
        IsEditing = true;
        Title = "Modifier le rappel";
        IsBp = r.Type == "BP";
        TimeText = r.RemindTime.ToString(@"hh\:mm");
        IsActive = r.IsActive;
        foreach (var d in Days) d.IsOn = (r.DaysMask & (1 << d.Bit)) != 0;
    }

    [RelayCommand] private void SelectBp() => IsBp = true;
    [RelayCommand] private void SelectGlycemia() => IsBp = false;
    [RelayCommand] private Task CancelAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveAsync()
    {
        TimeError = "";
        DaysError = "";

        if (!DateTime.TryParseExact(TimeText.Trim(), "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            TimeError = "Format HH:MM (ex. 08:00)";
        var mask = Days.Where(d => d.IsOn).Sum(d => 1 << d.Bit);
        if (mask == 0) DaysError = "Choisissez au moins un jour.";
        if (TimeError != "" || DaysError != "") return;

        var r = new Reminder
        {
            Id = _id,
            Type = IsBp ? "BP" : "GLYCEMIA",
            RemindTime = t.TimeOfDay,
            DaysMask = mask,
            IsActive = IsActive
        };

        IsBusy = true;
        var (saved, error) = IsEditing ? await data.UpdateReminderAsync(r) : await data.CreateReminderAsync(r);
        IsBusy = false;
        if (saved is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        await Shell.Current.GoToAsync("..");   // la liste se recharge et reprogramme les alarmes
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync("Supprimer", "Supprimer ce rappel ?", "Supprimer", "Annuler");
        if (!ok) return;
        var (_, error) = await data.DeleteReminderAsync(_id);
        if (error != null)
        {
            _ = ShowToastAsync(error);
            return;
        }
        await Shell.Current.GoToAsync("..");
    }
}
