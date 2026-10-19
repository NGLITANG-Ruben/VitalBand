namespace VitalBand.App.Models;

public class Reminder
{
    public int Id { get; set; }
    public string Type { get; set; } = "BP";       // BP ou GLYCEMIA
    public TimeSpan RemindTime { get; set; }       // ex. 08:00
    public int DaysMask { get; set; } = 127;       // bit 0 = lundi ... bit 6 = dimanche
    public bool IsActive { get; set; } = true;
}

public static class ReminderHelper
{
    private static readonly string[] DayNames = { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" };

    public static string TypeLabel(string type) => type == "BP" ? "Tension" : "Glycémie";

    public static string DaysText(int mask)
    {
        if (mask == 127) return "Tous les jours";
        if (mask == 31) return "Lun – Ven";
        return string.Join(", ", Enumerable.Range(0, 7).Where(i => (mask & (1 << i)) != 0).Select(i => DayNames[i]));
    }

    // Prochaine date de déclenchement après "now" (null si aucun jour choisi)
    public static DateTime? NextTrigger(int hour, int minute, int mask, DateTime now)
    {
        for (var d = 0; d <= 7; d++)
        {
            var candidate = now.Date.AddDays(d).AddHours(hour).AddMinutes(minute);
            if (candidate <= now.AddSeconds(5)) continue;
            var bit = ((int)candidate.DayOfWeek + 6) % 7;   // lundi = 0
            if ((mask & (1 << bit)) != 0) return candidate;
        }
        return null;
    }
}

public class ReminderItem(Reminder source)
{
    public Reminder Source { get; } = source;
    public string Title => $"{ReminderHelper.TypeLabel(Source.Type)} · {Source.RemindTime:hh\\:mm}";
    public string DaysText => ReminderHelper.DaysText(Source.DaysMask);
    public bool IsActive => Source.IsActive;
}
