using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class HomeViewModel(ApiService api, DataService data, IReminderScheduler scheduler, AvatarService avatar, HealthService health) : BaseViewModel
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    [ObservableProperty] private string greeting = "";
    [ObservableProperty] private string nextReminder = "Aucun rappel actif";
    [ObservableProperty] private string tipTitle = "";
    [ObservableProperty] private string tipText = "";
    [ObservableProperty] private string initials = "";
    [ObservableProperty] private ImageSource? avatarSource;
    [ObservableProperty] private MeasurementItem lastBp = MeasurementItem.Empty("Aucune mesure");
    [ObservableProperty] private MeasurementItem lastGlycemia = MeasurementItem.Empty("Aucune mesure");

    public async Task LoadAsync()
    {
        Greeting = $"Bonjour, {api.FirstName}";
        Initials = api.Initials;
        AvatarSource = avatar.Load();
        var tip = await health.TipOfTheDayAsync();
        if (tip != null) { TipTitle = tip.Title; TipText = tip.Text; }

        // Rappels : reprogrammés à chaque ouverture + prochain rappel affiché
        var (rem, _) = await data.GetRemindersAsync();
        if (rem != null)
        {
            scheduler.ScheduleAll(rem);
            NextReminder = Describe(rem);
        }

        var (list, error) = await data.GetMeasurementsAsync();
        if (list is null)
        {
            if (error != null) _ = ShowToastAsync(error);
            return;
        }
        var (th, _) = await data.GetThresholdsAsync();
        IReadOnlyList<Threshold> t = th ?? new List<Threshold>();

        var bp = list.FirstOrDefault(x => x.Type == "BP");       // l'API trie du plus récent au plus ancien
        var gl = list.FirstOrDefault(x => x.Type == "GLYCEMIA");
        LastBp = bp is null ? MeasurementItem.Empty("Aucune mesure") : MeasurementItem.From(bp, t);
        LastGlycemia = gl is null ? MeasurementItem.Empty("Aucune mesure") : MeasurementItem.From(gl, t);
    }

    private static string Describe(List<Reminder> reminders)
    {
        var now = DateTime.Now;
        Reminder? best = null;
        DateTime? bestAt = null;
        foreach (var r in reminders.Where(x => x.IsActive))
        {
            var at = ReminderHelper.NextTrigger(r.RemindTime.Hours, r.RemindTime.Minutes, r.DaysMask, now);
            if (at != null && (bestAt == null || at < bestAt)) { best = r; bestAt = at; }
        }
        if (best is null) return "Aucun rappel actif";

        var day = bestAt!.Value.Date == now.Date ? "aujourd’hui"
            : bestAt.Value.Date == now.Date.AddDays(1) ? "demain"
            : bestAt.Value.ToString("dddd", Fr);
        return $"{ReminderHelper.TypeLabel(best.Type)} · {best.RemindTime:hh\\:mm} · {day}";
    }

    [RelayCommand]
    private Task NewMeasurementAsync() => Shell.Current.GoToAsync("entry");

    [RelayCommand]
    private Task OpenTipsAsync() => Shell.Current.GoToAsync("tips");
}
