using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class EntryViewModel(DataService data) : BaseViewModel, IQueryAttributable
{
    private int _id;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [ObservableProperty] private string title = "Nouvelle mesure";
    [ObservableProperty] private bool isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGlycemia))]
    private bool isBp = true;
    public bool IsGlycemia => !IsBp;

    [ObservableProperty] private string systolic = "";
    [ObservableProperty] private string diastolic = "";
    [ObservableProperty] private string pulse = "";
    [ObservableProperty] private string glucose = "";
    [ObservableProperty] private string context = "FASTING";
    [ObservableProperty] private string comment = "";
    [ObservableProperty] private string dateText = DateTime.Now.ToString("dd/MM/yyyy", Inv);
    [ObservableProperty] private string timeText = DateTime.Now.ToString("HH:mm", Inv);

    [ObservableProperty] private string systolicError = "";
    [ObservableProperty] private string diastolicError = "";
    [ObservableProperty] private string pulseError = "";
    [ObservableProperty] private string glucoseError = "";
    [ObservableProperty] private string dateError = "";
    [ObservableProperty] private string timeError = "";

    // Appelé par Shell quand on ouvre une mesure existante pour la modifier
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("item", out var o) || o is not Measurement m) return;
        _id = m.Id;
        IsEditing = true;
        Title = "Modifier la mesure";
        IsBp = m.Type == "BP";
        Systolic = m.Systolic?.ToString() ?? "";
        Diastolic = m.Diastolic?.ToString() ?? "";
        Pulse = m.Pulse?.ToString() ?? "";
        Glucose = m.Glucose?.ToString("0.00", Inv) ?? "";
        Context = m.Context ?? "FASTING";
        Comment = m.Comment ?? "";
        DateText = m.MeasuredAt.ToString("dd/MM/yyyy", Inv);
        TimeText = m.MeasuredAt.ToString("HH:mm", Inv);
    }

    [RelayCommand] private void SelectBp() => IsBp = true;
    [RelayCommand] private void SelectGlycemia() => IsBp = false;
    [RelayCommand] private void SetContext(string c) => Context = c;
    [RelayCommand] private Task CancelAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveAsync()
    {
        const string required = "Ce champ est obligatoire.";
        const string invalid = "Nombre invalide.";
        SystolicError = DiastolicError = PulseError = GlucoseError = DateError = TimeError = "";
        int? sys = null, dia = null, pul = null;
        decimal? glu = null;

        if (IsBp)
        {
            if (string.IsNullOrWhiteSpace(Systolic)) SystolicError = required;
            else if (int.TryParse(Systolic, out var s)) sys = s;
            else SystolicError = invalid;

            if (string.IsNullOrWhiteSpace(Diastolic)) DiastolicError = required;
            else if (int.TryParse(Diastolic, out var d)) dia = d;
            else DiastolicError = invalid;

            if (!string.IsNullOrWhiteSpace(Pulse))
            {
                if (int.TryParse(Pulse, out var p)) pul = p;
                else PulseError = invalid;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Glucose)) GlucoseError = required;
            else if (decimal.TryParse(Glucose.Replace(',', '.'), NumberStyles.Float, Inv, out var g)) glu = g;
            else GlucoseError = invalid;
        }

        if (!DateTime.TryParseExact($"{DateText.Trim()} {TimeText.Trim()}", "dd/MM/yyyy HH:mm",
                Inv, DateTimeStyles.None, out var when))
        {
            DateError = "Format JJ/MM/AAAA";
            TimeError = "Format HH:MM";
        }

        if (new[] { SystolicError, DiastolicError, PulseError, GlucoseError, DateError, TimeError }
            .Any(e => e != "")) return;

        var m = new Measurement
        {
            Id = _id,
            Type = IsBp ? "BP" : "GLYCEMIA",
            Systolic = IsBp ? sys : null,
            Diastolic = IsBp ? dia : null,
            Pulse = IsBp ? pul : null,
            Glucose = IsBp ? null : glu,
            Context = IsBp ? null : Context,
            MeasuredAt = when,
            Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim()
        };

        IsBusy = true;
        var (saved, error) = IsEditing ? await data.UpdateMeasurementAsync(m) : await data.CreateMeasurementAsync(m);
        IsBusy = false;
        if (saved is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }

        // Alerte si la valeur sort des seuils personnalisés
        var (th, _) = await data.GetThresholdsAsync();
        var item = MeasurementItem.From(saved, th ?? new List<Threshold>());
        if (item.StatusText != "Normale")
            await Shell.Current.DisplayAlertAsync("Valeur hors de vos seuils",
                $"Votre mesure ({item.Title}) est en dehors de vos seuils personnalisés. " +
                "Pensez à en parler à votre médecin.", "OK");

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync("Supprimer", "Supprimer cette mesure ?", "Supprimer", "Annuler");
        if (!ok) return;
        var (_, error) = await data.DeleteMeasurementAsync(_id);
        if (error != null)
        {
            _ = ShowToastAsync(error);
            return;
        }
        await Shell.Current.GoToAsync("..");
    }
}
