using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class RemindersViewModel(DataService data, IReminderScheduler scheduler) : BaseViewModel
{
    public ObservableCollection<ReminderItem> Items { get; } = new();
    [ObservableProperty] private bool isEmpty;

    public async Task LoadAsync()
    {
        await scheduler.EnsurePermissionAsync();
        var (list, error) = await data.GetRemindersAsync();
        if (list is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        Items.Clear();
        foreach (var r in list) Items.Add(new ReminderItem(r));
        IsEmpty = Items.Count == 0;
        scheduler.ScheduleAll(list);
    }

    // Appelé quand on bascule l'interrupteur d'un rappel
    public async Task SetActiveAsync(ReminderItem item, bool value)
    {
        var r = item.Source;
        if (r.IsActive == value) return;
        var updated = new Reminder { Id = r.Id, Type = r.Type, RemindTime = r.RemindTime, DaysMask = r.DaysMask, IsActive = value };
        var (saved, error) = await data.UpdateReminderAsync(updated);
        if (saved is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            await LoadAsync();
            return;
        }
        r.IsActive = value;
        scheduler.ScheduleAll(Items.Select(i => i.Source));
    }

    [RelayCommand]
    private Task OpenAsync(ReminderItem item) =>
        Shell.Current.GoToAsync("reminder", new Dictionary<string, object> { ["item"] = item.Source });

    [RelayCommand]
    private Task NewAsync() => Shell.Current.GoToAsync("reminder");

    [RelayCommand]
    private void TestNotification()
    {
        scheduler.ShowTest();
        _ = ShowToastAsync("Notification de test envoyée.");
    }
}
