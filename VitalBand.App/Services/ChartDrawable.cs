using System.Globalization;

namespace VitalBand.App.Services;

public record ChartPoint(DateTime Time, double Value);
public record ChartLine(double Value, string Label);

public class ChartSeries
{
    public Color Color { get; init; } = Colors.Black;
    public List<ChartPoint> Points { get; init; } = new();
    public double? Min { get; init; }   // seuil bas (point orange si en dessous)
    public double? Max { get; init; }   // seuil haut (point orange si au dessus)
}

// Dessine les courbes directement (aucune bibliothèque externe)
public class ChartDrawable : IDrawable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    public List<ChartSeries> Series { get; } = new();
    public List<ChartLine> Lines { get; } = new();
    public DateTime From { get; set; } = DateTime.Today.AddDays(-6);
    public DateTime To { get; set; } = DateTime.Now;

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float left = 40, right = 12, top = 12, bottom = 26;
        var plot = new RectF(left, top, rect.Width - left - right, rect.Height - top - bottom);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(rect);

        var values = Series.SelectMany(s => s.Points).Select(p => p.Value)
            .Concat(Lines.Select(l => l.Value)).ToList();
        if (values.Count == 0) return;

        var min = values.Min();
        var max = values.Max();
        if (max - min < 1e-6) { min -= 1; max += 1; }
        else { var pad = (max - min) * 0.12; min -= pad; max += pad; }

        var span = (To - From).TotalSeconds;
        if (span <= 0) span = 1;

        float X(DateTime t) => plot.Left + (float)Math.Clamp((t - From).TotalSeconds / span, 0, 1) * plot.Width;
        float Y(double v) => plot.Bottom - (float)((v - min) / (max - min)) * plot.Height;

        // Grille et axe vertical
        canvas.FontSize = 10;
        for (var i = 0; i <= 4; i++)
        {
            var v = min + (max - min) * i / 4;
            var y = Y(v);
            canvas.StrokeColor = Color.FromArgb("#E3E8E6");
            canvas.StrokeSize = 1;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
            canvas.FontColor = Color.FromArgb("#5C6D69");
            canvas.DrawString(v.ToString(max - min > 10 ? "0" : "0.00", Inv), 0, y - 8, left - 6, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Lignes de seuil en pointillés
        foreach (var l in Lines)
        {
            var y = Y(l.Value);
            canvas.StrokeColor = Color.FromArgb("#9AA8A4");
            canvas.StrokeSize = 1.5f;
            for (var x = plot.Left; x < plot.Right; x += 9)
                canvas.DrawLine(x, y, Math.Min(x + 5, plot.Right), y);   // pointillés
            canvas.FontColor = Color.FromArgb("#5C6D69");
            canvas.DrawString(l.Label, plot.Right - 44, y - 15, 44, 14, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        }

        // Courbes et points
        foreach (var s in Series)
        {
            if (s.Points.Count == 0) continue;
            canvas.StrokeColor = s.Color;
            canvas.StrokeSize = 2.5f;
            if (s.Points.Count > 1)
            {
                var path = new PathF();
                path.MoveTo(X(s.Points[0].Time), Y(s.Points[0].Value));
                foreach (var p in s.Points.Skip(1)) path.LineTo(X(p.Time), Y(p.Value));
                canvas.DrawPath(path);
            }
            foreach (var p in s.Points)
            {
                var outOfRange = (s.Max.HasValue && p.Value > s.Max.Value) || (s.Min.HasValue && p.Value < s.Min.Value);
                canvas.FillColor = outOfRange ? Color.FromArgb("#D9822B") : s.Color;
                canvas.FillCircle(X(p.Time), Y(p.Value), 4.5f);
            }
        }

        // Dates sous le graphique
        canvas.FontColor = Color.FromArgb("#5C6D69");
        var mid = From.AddSeconds(span / 2);
        canvas.DrawString(From.ToString("dd/MM", Inv), plot.Left - 25, plot.Bottom + 4, 50, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
        canvas.DrawString(mid.ToString("dd/MM", Inv), X(mid) - 25, plot.Bottom + 4, 50, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
        canvas.DrawString(To.ToString("dd/MM", Inv), plot.Right - 25, plot.Bottom + 4, 50, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
    }
}
