using System.Globalization;

namespace VitalBand.App.Models;

public class Measurement
{
    public int Id { get; set; }
    public string Type { get; set; } = "BP";     // BP ou GLYCEMIA
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public decimal? Glucose { get; set; }        // g/L
    public string? Context { get; set; }         // FASTING, BEFORE_MEAL, AFTER_MEAL
    public DateTime MeasuredAt { get; set; }
    public string? Comment { get; set; }
}

public class Threshold
{
    public string Metric { get; set; } = "";     // SYSTOLIC, DIASTOLIC, GLUCOSE
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}

public static class StatusHelper
{
    public static (string Text, bool Ok) Evaluate(Measurement m, IReadOnlyList<Threshold> th)
    {
        Threshold? T(string k) => th.FirstOrDefault(t => t.Metric == k);

        if (m.Type == "BP")
        {
            var s = T("SYSTOLIC");
            var d = T("DIASTOLIC");
            if ((s != null && m.Systolic is int sv && sv > s.MaxValue) ||
                (d != null && m.Diastolic is int dv && dv > d.MaxValue)) return ("Élevée", false);
            if ((s != null && m.Systolic is int sl && sl < s.MinValue) ||
                (d != null && m.Diastolic is int dl && dl < d.MinValue)) return ("Basse", false);
        }
        else
        {
            var g = T("GLUCOSE");
            if (g != null && m.Glucose is decimal gv)
            {
                if (gv > g.MaxValue) return ("Élevée", false);
                if (gv < g.MinValue) return ("Basse", false);
            }
        }
        return ("Normale", true);
    }

    public static string ContextLabel(string? c) => c switch
    {
        "FASTING" => "à jeun",
        "BEFORE_MEAL" => "avant repas",
        "AFTER_MEAL" => "après repas",
        _ => ""
    };
}

// Version "affichable" d'une mesure (textes et couleurs prêts pour l'écran)
public class MeasurementItem
{
    public Measurement Source { get; init; } = new();
    public string Title { get; init; } = "—";
    public string Subtitle { get; init; } = "";
    public string StatusText { get; init; } = "";
    public Color StatusBg { get; init; } = Colors.Transparent;
    public Color StatusFg { get; init; } = Colors.Black;
    public bool HasStatus => StatusText != "";

    public static MeasurementItem Empty(string subtitle) => new() { Subtitle = subtitle };

    public static MeasurementItem From(Measurement m, IReadOnlyList<Threshold> th)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var (text, ok) = StatusHelper.Evaluate(m, th);
        var isBp = m.Type == "BP";
        var title = isBp ? $"{m.Systolic}/{m.Diastolic} mmHg" : string.Format(fr, "{0:0.00} g/L", m.Glucose);
        var sub = $"{(isBp ? "Tension" : "Glycémie")} · {m.MeasuredAt.ToString("d MMM, HH:mm", fr)}";
        if (!isBp && !string.IsNullOrEmpty(m.Context)) sub += " · " + StatusHelper.ContextLabel(m.Context);
        if (!string.IsNullOrEmpty(m.Comment)) sub += " · Note";
        return new MeasurementItem
        {
            Source = m, Title = title, Subtitle = sub, StatusText = text,
            StatusBg = Color.FromArgb(ok ? "#E3F4EE" : "#FDEBD3"),
            StatusFg = Color.FromArgb(ok ? "#0B5B4B" : "#8A4B00")
        };
    }
}
