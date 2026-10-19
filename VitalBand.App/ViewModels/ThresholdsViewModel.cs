using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class ThresholdsViewModel(DataService data) : BaseViewModel
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [ObservableProperty] private string sysMin = "";
    [ObservableProperty] private string sysMax = "";
    [ObservableProperty] private string diaMin = "";
    [ObservableProperty] private string diaMax = "";
    [ObservableProperty] private string gluMin = "";
    [ObservableProperty] private string gluMax = "";
    [ObservableProperty] private string sysError = "";
    [ObservableProperty] private string diaError = "";
    [ObservableProperty] private string gluError = "";

    public async Task LoadAsync()
    {
        var (th, error) = await data.GetThresholdsAsync();
        if (th is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        string F(string metric, bool max)
        {
            var t = th.FirstOrDefault(x => x.Metric == metric);
            return t is null ? "" : (max ? t.MaxValue : t.MinValue).ToString("0.##", Inv);
        }
        SysMin = F("SYSTOLIC", false); SysMax = F("SYSTOLIC", true);
        DiaMin = F("DIASTOLIC", false); DiaMax = F("DIASTOLIC", true);
        GluMin = F("GLUCOSE", false); GluMax = F("GLUCOSE", true);
    }

    private static bool Parse(string s, out decimal v) =>
        decimal.TryParse((s ?? "").Replace(',', '.'), NumberStyles.Float, Inv, out v);

    private static bool TryPair(string a, string b, out decimal min, out decimal max, out string error)
    {
        min = 0; max = 0; error = "";
        if (!Parse(a, out min) || !Parse(b, out max)) { error = "Valeurs invalides."; return false; }
        if (min >= max) { error = "Le minimum doit être inférieur au maximum."; return false; }
        return true;
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        SysMin = "90"; SysMax = "140";
        DiaMin = "60"; DiaMax = "90";
        GluMin = "0.7"; GluMax = "1.1";
        SysError = DiaError = GluError = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var okS = TryPair(SysMin, SysMax, out var s1, out var s2, out var eS);
        var okD = TryPair(DiaMin, DiaMax, out var d1, out var d2, out var eD);
        var okG = TryPair(GluMin, GluMax, out var g1, out var g2, out var eG);
        SysError = eS; DiaError = eD; GluError = eG;
        if (!okS || !okD || !okG) return;

        var list = new List<Threshold>
        {
            new() { Metric = "SYSTOLIC", MinValue = s1, MaxValue = s2 },
            new() { Metric = "DIASTOLIC", MinValue = d1, MaxValue = d2 },
            new() { Metric = "GLUCOSE", MinValue = g1, MaxValue = g2 }
        };

        IsBusy = true;
        var (saved, error) = await data.SaveThresholdsAsync(list);
        IsBusy = false;
        _ = ShowToastAsync(saved is null ? (error ?? "Une erreur est survenue.") : "Seuils enregistrés avec succès.");
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");
}
