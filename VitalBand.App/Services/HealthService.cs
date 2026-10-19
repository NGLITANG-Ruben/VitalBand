using System.Text.Json;
using VitalBand.App.Models;

namespace VitalBand.App.Services;

// Lit Resources/Raw/health_content.json : pour ajouter un conseil ou une question, il suffit de modifier ce fichier
public class HealthService
{
    private HealthContent? _content;

    public async Task<HealthContent> LoadAsync()
    {
        if (_content != null) return _content;
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("health_content.json");
            _content = await JsonSerializer.DeserializeAsync<HealthContent>(stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new HealthContent();
        }
        catch
        {
            _content = new HealthContent();
        }
        return _content;
    }

    // Un nouveau conseil chaque jour
    public async Task<HealthTip?> TipOfTheDayAsync()
    {
        var c = await LoadAsync();
        if (c.Tips.Count == 0) return null;
        var d = DateTime.Today;
        return c.Tips[(d.Year + d.DayOfYear) % c.Tips.Count];
    }
}
