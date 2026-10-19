using System.Globalization;
using VitalBand.App.Models;

namespace VitalBand.App.Services;

public class DataService(ApiService api)
{
    public Task<(List<Measurement>? Data, string? Error)> GetMeasurementsAsync(string? type = null, DateTime? from = null)
    {
        var q = new List<string>();
        if (type != null) q.Add($"type={type}");
        if (from != null) q.Add("from=" + from.Value.ToString("s", CultureInfo.InvariantCulture));
        return api.SendAsync<List<Measurement>>(HttpMethod.Get,
            "measurements" + (q.Count > 0 ? "?" + string.Join("&", q) : ""));
    }

    public Task<(Measurement? Data, string? Error)> CreateMeasurementAsync(Measurement m) =>
        api.SendAsync<Measurement>(HttpMethod.Post, "measurements", m);

    public Task<(Measurement? Data, string? Error)> UpdateMeasurementAsync(Measurement m) =>
        api.SendAsync<Measurement>(HttpMethod.Put, $"measurements/{m.Id}", m);

    public Task<(object? Data, string? Error)> DeleteMeasurementAsync(int id) =>
        api.SendAsync<object>(HttpMethod.Delete, $"measurements/{id}");

    public Task<(List<Threshold>? Data, string? Error)> GetThresholdsAsync() =>
        api.SendAsync<List<Threshold>>(HttpMethod.Get, "thresholds");

    public Task<(List<Threshold>? Data, string? Error)> SaveThresholdsAsync(List<Threshold> items) =>
        api.SendAsync<List<Threshold>>(HttpMethod.Put, "thresholds", items);

    public Task<(List<Reminder>? Data, string? Error)> GetRemindersAsync() =>
        api.SendAsync<List<Reminder>>(HttpMethod.Get, "reminders");

    public Task<(Reminder? Data, string? Error)> CreateReminderAsync(Reminder r) =>
        api.SendAsync<Reminder>(HttpMethod.Post, "reminders", r);

    public Task<(Reminder? Data, string? Error)> UpdateReminderAsync(Reminder r) =>
        api.SendAsync<Reminder>(HttpMethod.Put, $"reminders/{r.Id}", r);

    public Task<(object? Data, string? Error)> DeleteReminderAsync(int id) =>
        api.SendAsync<object>(HttpMethod.Delete, $"reminders/{id}");
}
