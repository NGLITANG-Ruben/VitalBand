using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class ExportViewModel(ApiService api, DataService data, IPdfExporter exporter) : BaseViewModel
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private string period = "30";
    public bool IsCustom => Period == "CUSTOM";

    [ObservableProperty] private string fromText = DateTime.Today.AddDays(-29).ToString("dd/MM/yyyy", Inv);
    [ObservableProperty] private string toText = DateTime.Today.ToString("dd/MM/yyyy", Inv);
    [ObservableProperty] private string fromError = "";
    [ObservableProperty] private string toError = "";

    [ObservableProperty] private bool includeBp = true;
    [ObservableProperty] private bool includeGlycemia = true;
    [ObservableProperty] private bool includeCharts = true;
    [ObservableProperty] private bool includeComments = true;

    [ObservableProperty] private string previewTitle = "Synthèse";
    [ObservableProperty] private string previewDetail = "";

    partial void OnIncludeBpChanged(bool value) => _ = RefreshPreviewAsync();
    partial void OnIncludeGlycemiaChanged(bool value) => _ = RefreshPreviewAsync();

    private bool TryRange(out DateTime from, out DateTime to)
    {
        FromError = ToError = "";
        if (!IsCustom)
        {
            to = DateTime.Now;
            from = DateTime.Today.AddDays(-(int.Parse(Period) - 1));
            return true;
        }
        from = to = default;
        var okF = DateTime.TryParseExact(FromText.Trim(), "dd/MM/yyyy", Inv, DateTimeStyles.None, out from);
        var okT = DateTime.TryParseExact(ToText.Trim(), "dd/MM/yyyy", Inv, DateTimeStyles.None, out to);
        if (!okF) FromError = "Format JJ/MM/AAAA";
        if (!okT) ToError = "Format JJ/MM/AAAA";
        if (okF && okT && from > to) ToError = "La fin doit suivre le début.";
        if (FromError != "" || ToError != "") return false;
        to = to.Date.AddDays(1).AddSeconds(-1);
        return true;
    }

    private async Task<(List<Measurement>? List, List<Threshold> Th, string? Error)> FetchAsync(DateTime from, DateTime to)
    {
        var (all, error) = await data.GetMeasurementsAsync(null, from);
        var (th, _) = await data.GetThresholdsAsync();
        if (all is null) return (null, th ?? new List<Threshold>(), error);
        var list = all.Where(m => m.MeasuredAt <= to
                                  && ((m.Type == "BP" && IncludeBp) || (m.Type == "GLYCEMIA" && IncludeGlycemia)))
                      .OrderBy(m => m.MeasuredAt).ToList();
        return (list, th ?? new List<Threshold>(), null);
    }

    public async Task RefreshPreviewAsync()
    {
        if (!TryRange(out var from, out var to)) { PreviewDetail = ""; return; }
        var (list, th, _) = await FetchAsync(from, to);
        PreviewTitle = $"Synthèse du {from.ToString("dd/MM/yyyy", Inv)} au {to.ToString("dd/MM/yyyy", Inv)}";
        if (list is null) { PreviewDetail = "Données indisponibles."; return; }
        var outCount = list.Count(m => !StatusHelper.Evaluate(m, th).Ok);
        PreviewDetail = $"{list.Count} mesures · {outCount} hors seuil";
    }

    [RelayCommand]
    private async Task SetPeriodAsync(string p)
    {
        Period = p;
        await RefreshPreviewAsync();
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (!IncludeBp && !IncludeGlycemia)
        {
            _ = ShowToastAsync("Sélectionnez au moins un type de mesure.");
            return;
        }
        if (!TryRange(out var from, out var to)) return;

        IsBusy = true;
        try
        {
            var (list, th, error) = await FetchAsync(from, to);
            if (list is null)
            {
                _ = ShowToastAsync(error ?? "Une erreur est survenue.");
                return;
            }
            if (list.Count == 0)
            {
                _ = ShowToastAsync("Aucune donnée sur cette période.");
                return;
            }

            var path = await exporter.CreateAsync(new PdfReport(api.FullName, from, to, list, th,
                IncludeBp, IncludeGlycemia, IncludeCharts, IncludeComments));
            if (path is null)
            {
                _ = ShowToastAsync("Impossible de générer le PDF.");
                return;
            }
            _ = ShowToastAsync("PDF généré avec succès.");
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Synthèse VitalBand",
                File = new ShareFile(path)
            });
        }
        catch (Exception)
        {
            _ = ShowToastAsync("Impossible de générer le PDF.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");
}
