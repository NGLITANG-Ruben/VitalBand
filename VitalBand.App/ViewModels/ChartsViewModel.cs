using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalBand.App.Models;
using VitalBand.App.Services;

namespace VitalBand.App.ViewModels;

public partial class ChartsViewModel(DataService data) : BaseViewModel
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public ChartDrawable Chart { get; } = new();
    public event Action? ChartUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBp))]
    [NotifyPropertyChangedFor(nameof(IsGlycemia))]
    private string tab = "BP";

    [ObservableProperty] private string period = "7";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoData))]
    private bool hasData;

    [ObservableProperty] private string avgText = "—";
    [ObservableProperty] private string minText = "—";
    [ObservableProperty] private string maxText = "—";

    public bool IsBp => Tab == "BP";
    public bool IsGlycemia => Tab == "GLYCEMIA";
    public bool NoData => !HasData;

    public async Task LoadAsync()
    {
        var from = DateTime.Today.AddDays(-(int.Parse(Period) - 1));
        var (list, error) = await data.GetMeasurementsAsync(Tab, from);
        var (th, _) = await data.GetThresholdsAsync();
        if (list is null)
        {
            _ = ShowToastAsync(error ?? "Une erreur est survenue.");
            return;
        }
        var t = th ?? new List<Threshold>();
        decimal? Lo(string k) => t.FirstOrDefault(x => x.Metric == k)?.MinValue;
        decimal? Hi(string k) => t.FirstOrDefault(x => x.Metric == k)?.MaxValue;

        var items = list.OrderBy(m => m.MeasuredAt).ToList();
        Chart.From = from;
        Chart.To = DateTime.Now;
        Chart.Series.Clear();
        Chart.Lines.Clear();

        if (IsBp)
        {
            var bp = items.Where(m => m.Systolic != null && m.Diastolic != null).ToList();
            Chart.Series.Add(Make(bp, m => m.Systolic!.Value, "#0F6B5E", Lo("SYSTOLIC"), Hi("SYSTOLIC")));
            Chart.Series.Add(Make(bp, m => m.Diastolic!.Value, "#3A7CA5", Lo("DIASTOLIC"), Hi("DIASTOLIC")));
            if (Hi("SYSTOLIC") is decimal sh) Chart.Lines.Add(new ChartLine((double)sh, sh.ToString("0")));
            if (Hi("DIASTOLIC") is decimal dh) Chart.Lines.Add(new ChartLine((double)dh, dh.ToString("0")));

            if (bp.Count > 0)
            {
                var lo = bp.MinBy(m => m.Systolic)!;
                var hi = bp.MaxBy(m => m.Systolic)!;
                AvgText = $"{bp.Average(m => m.Systolic!.Value):0}/{bp.Average(m => m.Diastolic!.Value):0}";
                MinText = $"{lo.Systolic}/{lo.Diastolic}";
                MaxText = $"{hi.Systolic}/{hi.Diastolic}";
            }
            else AvgText = MinText = MaxText = "—";
            HasData = bp.Count >= 2;
        }
        else
        {
            var gl = items.Where(m => m.Glucose != null).ToList();
            Chart.Series.Add(Make(gl, m => (double)m.Glucose!.Value, "#0F6B5E", Lo("GLUCOSE"), Hi("GLUCOSE")));
            if (Lo("GLUCOSE") is decimal gmin) Chart.Lines.Add(new ChartLine((double)gmin, gmin.ToString("0.00", Fr)));
            if (Hi("GLUCOSE") is decimal gmax) Chart.Lines.Add(new ChartLine((double)gmax, gmax.ToString("0.00", Fr)));

            if (gl.Count > 0)
            {
                AvgText = string.Format(Fr, "{0:0.00} g/L", gl.Average(m => (double)m.Glucose!.Value));
                MinText = string.Format(Fr, "{0:0.00} g/L", gl.Min(m => m.Glucose));
                MaxText = string.Format(Fr, "{0:0.00} g/L", gl.Max(m => m.Glucose));
            }
            else AvgText = MinText = MaxText = "—";
            HasData = gl.Count >= 2;
        }
        ChartUpdated?.Invoke();
    }

    private static ChartSeries Make(IEnumerable<Measurement> src, Func<Measurement, double> pick,
        string color, decimal? lo, decimal? hi) => new()
    {
        Color = Color.FromArgb(color),
        Points = src.Select(m => new ChartPoint(m.MeasuredAt, pick(m))).ToList(),
        Min = lo is null ? null : (double)lo.Value,
        Max = hi is null ? null : (double)hi.Value
    };

    [RelayCommand]
    private async Task SetTabAsync(string tab)
    {
        Tab = tab;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SetPeriodAsync(string days)
    {
        Period = days;
        await LoadAsync();
    }
}
