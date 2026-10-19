using System.Globalization;
using Android.Graphics.Pdf;
using VitalBand.App.Models;
using ACanvas = Android.Graphics.Canvas;
using AColor = Android.Graphics.Color;
using APaint = Android.Graphics.Paint;

namespace VitalBand.App.Services;

// Génération du PDF avec l'outil intégré à Android (aucune bibliothèque externe)
public class AndroidPdfExporter : IPdfExporter
{
    private const int W = 595, H = 842, M = 40;   // page A4 en points, marges
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public Task<string?> CreateAsync(PdfReport r) => Task.Run<string?>(() => Build(r));

    private sealed record Ser(string Color, List<(DateTime T, double V)> Pts, double? Lo, double? Hi);

    private sealed class Writer
    {
        public PdfDocument Doc { get; } = new();
        public PdfDocument.Page? Page { get; private set; }
        public ACanvas Canvas { get; private set; } = null!;
        public int PageNo { get; private set; }
        public float Y { get; set; }

        public void NewPage()
        {
            Finish();
            PageNo++;
            var info = new PdfDocument.PageInfo.Builder(W, H, PageNo).Create()!;
            Page = Doc.StartPage(info)!;
            Canvas = Page.Canvas!;
            Y = M;
        }

        public void Finish()
        {
            if (Page is null) return;
            Canvas.DrawText($"VitalBand · page {PageNo} · Document informatif, il ne remplace pas un avis médical.",
                M, H - 22, P(8, false, "#5C6D69"));
            Doc.FinishPage(Page);
            Page = null;
        }

        public void Ensure(float height)
        {
            if (Y + height > H - 50) NewPage();
        }
    }

    private static APaint P(float size, bool bold = false, string color = "#1D2B28", APaint.Align? align = null)
    {
        var p = new APaint(Android.Graphics.PaintFlags.AntiAlias) { TextSize = size, Color = AColor.ParseColor(color) };
        p.SetTypeface(bold ? Android.Graphics.Typeface.DefaultBold : Android.Graphics.Typeface.Default);
        if (align != null) p.TextAlign = align;
        return p;
    }

    private static APaint Fill(string hex) =>
        new(Android.Graphics.PaintFlags.AntiAlias) { Color = AColor.ParseColor(hex) };

    private static APaint Stroke(string hex, float width)
    {
        var p = new APaint(Android.Graphics.PaintFlags.AntiAlias) { Color = AColor.ParseColor(hex), StrokeWidth = width };
        p.SetStyle(APaint.Style.Stroke);
        return p;
    }

    private static string D(DateTime d) => d.ToString("dd/MM/yyyy", Fr);

    private static List<string> Wrap(string text, APaint p, float maxW)
    {
        var lines = new List<string>();
        foreach (var para in text.Split('\n'))
        {
            var line = "";
            foreach (var word in para.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var test = line.Length == 0 ? word : line + " " + word;
                if (p.MeasureText(test) <= maxW) line = test;
                else
                {
                    if (line.Length > 0) lines.Add(line);
                    line = word;
                }
            }
            lines.Add(line);
        }
        return lines;
    }

    private static void Text(Writer w, string s, float size, bool bold = false, string color = "#1D2B28")
    {
        var p = P(size, bold, color);
        foreach (var line in Wrap(s, p, W - 2 * M))
        {
            w.Ensure(size * 1.6f);
            w.Canvas.DrawText(line, M, w.Y + size, p);
            w.Y += size * 1.6f;
        }
    }

    private static void Section(Writer w, string title)
    {
        w.Y += 10;
        w.Ensure(40);
        w.Canvas.DrawText(title, M, w.Y + 14, P(14, true, "#0F6B5E"));
        w.Canvas.DrawLine(M, w.Y + 20, W - M, w.Y + 20, Stroke("#CDEDE5", 1.2f));
        w.Y += 28;
    }

    private static string Range(Threshold? t, string fmt) =>
        t is null ? "—" : $"{t.MinValue.ToString(fmt, Fr)}–{t.MaxValue.ToString(fmt, Fr)}";

    private static string Build(PdfReport r)
    {
        var th = r.Thresholds;
        Threshold? T(string k) => th.FirstOrDefault(t => t.Metric == k);
        var bp = r.Measurements.Where(m => m.Type == "BP").ToList();
        var gl = r.Measurements.Where(m => m.Type == "GLYCEMIA").ToList();

        var w = new Writer();
        w.NewPage();
        w.Canvas.DrawRect(0, 0, W, 100, Fill("#073F3A"));
        w.Canvas.DrawText("VitalBand", M, 48, P(26, true, "#FFFFFF"));
        w.Canvas.DrawText("Journal de constantes · synthèse pour la consultation", M, 70, P(11, false, "#CDEDE5"));
        w.Y = 120;
        Text(w, $"Patient : {r.PatientName}", 12, true);
        Text(w, $"Période : du {D(r.From)} au {D(r.To)}", 11);
        Text(w, $"Généré le {DateTime.Now.ToString("dd/MM/yyyy 'à' HH:mm", Fr)}", 10, false, "#5C6D69");

        if (r.IncludeBp)
        {
            Section(w, "Tension artérielle");
            if (bp.Count == 0) Text(w, "Aucune mesure sur la période.", 11);
            else
            {
                var lo = bp.MinBy(m => m.Systolic)!;
                var hi = bp.MaxBy(m => m.Systolic)!;
                var outCount = bp.Count(m => !StatusHelper.Evaluate(m, th).Ok);
                Text(w, $"{bp.Count} mesures · moyenne {bp.Average(m => (double)(m.Systolic ?? 0)):0}/{bp.Average(m => (double)(m.Diastolic ?? 0)):0} mmHg" +
                        $" · min {lo.Systolic}/{lo.Diastolic} · max {hi.Systolic}/{hi.Diastolic}", 11);
                Text(w, $"{outCount} mesure(s) hors seuils (seuils : systolique {Range(T("SYSTOLIC"), "0.##")} mmHg, diastolique {Range(T("DIASTOLIC"), "0.##")} mmHg).", 11,
                    false, outCount > 0 ? "#8A4B00" : "#1D2B28");

                if (r.IncludeCharts && bp.Count >= 2)
                {
                    w.Ensure(190);
                    DrawChart(w.Canvas, M, w.Y, W - 2 * M, 170, r.From, r.To,
                        new List<Ser>
                        {
                            new("#0F6B5E", bp.Select(m => (m.MeasuredAt, (double)(m.Systolic ?? 0))).ToList(),
                                (double?)T("SYSTOLIC")?.MinValue, (double?)T("SYSTOLIC")?.MaxValue),
                            new("#3A7CA5", bp.Select(m => (m.MeasuredAt, (double)(m.Diastolic ?? 0))).ToList(),
                                (double?)T("DIASTOLIC")?.MinValue, (double?)T("DIASTOLIC")?.MaxValue)
                        },
                        new List<(double, string)>
                        {
                            ((double)(T("SYSTOLIC")?.MaxValue ?? 0), "max sys"),
                            ((double)(T("DIASTOLIC")?.MaxValue ?? 0), "max dia")
                        }.Where(x => x.Item1 > 0).ToList());
                    w.Y += 180;
                    Text(w, "Vert : systolique · Bleu : diastolique · Points orange : hors seuil · Pointillés : seuils maximum", 8, false, "#5C6D69");
                }
            }
        }

        if (r.IncludeGlycemia)
        {
            Section(w, "Glycémie");
            if (gl.Count == 0) Text(w, "Aucune mesure sur la période.", 11);
            else
            {
                var vals = gl.Select(m => (double)(m.Glucose ?? 0)).ToList();
                var outCount = gl.Count(m => !StatusHelper.Evaluate(m, th).Ok);
                Text(w, $"{gl.Count} mesures · moyenne {vals.Average().ToString("0.00", Fr)} g/L · min {vals.Min().ToString("0.00", Fr)} · max {vals.Max().ToString("0.00", Fr)}", 11);
                Text(w, $"{outCount} mesure(s) hors seuils (seuils : {Range(T("GLUCOSE"), "0.00")} g/L).", 11,
                    false, outCount > 0 ? "#8A4B00" : "#1D2B28");

                if (r.IncludeCharts && gl.Count >= 2)
                {
                    w.Ensure(190);
                    var g = T("GLUCOSE");
                    var lines = new List<(double, string)>();
                    if (g != null) { lines.Add(((double)g.MinValue, "min")); lines.Add(((double)g.MaxValue, "max")); }
                    DrawChart(w.Canvas, M, w.Y, W - 2 * M, 170, r.From, r.To,
                        new List<Ser> { new("#0F6B5E", gl.Select(m => (m.MeasuredAt, (double)(m.Glucose ?? 0))).ToList(),
                            (double?)g?.MinValue, (double?)g?.MaxValue) }, lines);
                    w.Y += 180;
                    Text(w, "Points orange : hors seuil · Pointillés : seuils minimum et maximum", 8, false, "#5C6D69");
                }
            }
        }

        // Tableau détaillé
        Section(w, "Détail des mesures");
        float[] cx = { M, M + 98, M + 160, M + 258, M + 316 };
        var commentW = W - M - cx[4];

        void Header()
        {
            w.Canvas.DrawRect(M, w.Y, W - M, w.Y + 18, Fill("#E3F4EE"));
            var hp = P(9, true, "#073F3A");
            w.Canvas.DrawText("Date", cx[0] + 4, w.Y + 12, hp);
            w.Canvas.DrawText("Type", cx[1], w.Y + 12, hp);
            w.Canvas.DrawText("Valeur", cx[2], w.Y + 12, hp);
            w.Canvas.DrawText("État", cx[3], w.Y + 12, hp);
            if (r.IncludeComments) w.Canvas.DrawText("Commentaire", cx[4], w.Y + 12, hp);
            w.Y += 20;
        }

        w.Ensure(60);
        Header();
        foreach (var m in r.Measurements)
        {
            var comment = r.IncludeComments ? (m.Comment ?? "") : "";
            var cp = P(8.5f, false, "#5C6D69");
            var lines = comment.Length > 0 ? Wrap(comment, cp, commentW - 4) : new List<string>();
            var rowH = Math.Max(1, lines.Count) * 11f + 8;
            if (w.Y + rowH > H - 50)
            {
                w.NewPage();
                Header();
            }

            var st = StatusHelper.Evaluate(m, th);
            var ty = w.Y + 12;
            var value = m.Type == "BP"
                ? $"{m.Systolic}/{m.Diastolic} mmHg"
                : $"{(m.Glucose ?? 0).ToString("0.00", Fr)} g/L" + (string.IsNullOrEmpty(m.Context) ? "" : $" ({StatusHelper.ContextLabel(m.Context)})");

            w.Canvas.DrawText(m.MeasuredAt.ToString("dd/MM/yyyy HH:mm", Fr), cx[0] + 4, ty, P(9));
            w.Canvas.DrawText(ReminderHelper.TypeLabel(m.Type), cx[1], ty, P(9));
            w.Canvas.DrawText(value, cx[2], ty, P(9));
            w.Canvas.DrawText(st.Text, cx[3], ty, P(9, !st.Ok, st.Ok ? "#0B5B4B" : "#8A4B00"));
            for (var i = 0; i < lines.Count; i++)
                w.Canvas.DrawText(lines[i], cx[4], ty + i * 11, cp);

            w.Canvas.DrawLine(M, w.Y + rowH, W - M, w.Y + rowH, Stroke("#E3E8E6", 0.6f));
            w.Y += rowH;
        }

        w.Finish();
        var path = System.IO.Path.Combine(FileSystem.CacheDirectory, $"VitalBand_synthese_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        using (var fs = System.IO.File.Create(path))
            w.Doc.WriteTo(fs);
        w.Doc.Close();
        return path;
    }

    private static void DrawChart(ACanvas c, float x, float y, float width, float height,
        DateTime from, DateTime to, List<Ser> series, List<(double V, string Label)> lines)
    {
        const float left = 40, right = 10, top = 8, bottom = 22;
        var px = x + left;
        var py = y + top;
        var pw = width - left - right;
        var ph = height - top - bottom;

        var values = series.SelectMany(s => s.Pts).Select(p => p.V).Concat(lines.Select(l => l.V)).ToList();
        if (values.Count == 0) return;
        var min = values.Min();
        var max = values.Max();
        if (max - min < 1e-6) { min -= 1; max += 1; }
        else { var pad = (max - min) * 0.12; min -= pad; max += pad; }
        var span = (to - from).TotalSeconds;
        if (span <= 0) span = 1;

        float X(DateTime t) => px + (float)Math.Clamp((t - from).TotalSeconds / span, 0, 1) * pw;
        float Y(double v) => py + ph - (float)((v - min) / (max - min)) * ph;

        c.DrawRect(x, y, x + width, y + height, Stroke("#E3E8E6", 0.8f));
        var grid = Stroke("#E3E8E6", 0.6f);
        var lbl = P(8, false, "#5C6D69", APaint.Align.Right);
        for (var i = 0; i <= 4; i++)
        {
            var v = min + (max - min) * i / 4;
            c.DrawLine(px, Y(v), px + pw, Y(v), grid);
            c.DrawText(v.ToString(max - min > 10 ? "0" : "0.00", Fr), px - 4, Y(v) + 3, lbl);
        }

        var dash = Stroke("#9AA8A4", 1f);
        dash.SetPathEffect(new Android.Graphics.DashPathEffect(new float[] { 5, 4 }, 0));
        foreach (var l in lines) c.DrawLine(px, Y(l.V), px + pw, Y(l.V), dash);

        foreach (var s in series)
        {
            var pts = s.Pts.OrderBy(p => p.T).ToList();
            var line = Stroke(s.Color, 1.8f);
            for (var i = 1; i < pts.Count; i++)
                c.DrawLine(X(pts[i - 1].T), Y(pts[i - 1].V), X(pts[i].T), Y(pts[i].V), line);
            foreach (var p in pts)
            {
                var outOfRange = (s.Hi.HasValue && p.V > s.Hi.Value) || (s.Lo.HasValue && p.V < s.Lo.Value);
                c.DrawCircle(X(p.T), Y(p.V), 3f, Fill(outOfRange ? "#D9822B" : s.Color));
            }
        }

        var xl = P(8, false, "#5C6D69", APaint.Align.Center);
        var mid = from.AddSeconds(span / 2);
        c.DrawText(from.ToString("dd/MM", Fr), px, y + height - 6, xl);
        c.DrawText(mid.ToString("dd/MM", Fr), X(mid), y + height - 6, xl);
        c.DrawText(to.ToString("dd/MM", Fr), px + pw, y + height - 6, xl);
    }
}
