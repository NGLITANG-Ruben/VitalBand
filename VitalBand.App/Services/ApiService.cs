using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VitalBand.App.Models;

namespace VitalBand.App.Services;

public class ApiService
{
    // Le téléphone atteint l'API du PC grâce à : adb reverse tcp:5000 tcp:5000
    private const string BaseUrl = "http://localhost:5000";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public int UserId { get; private set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Initials => $"{First(FirstName)}{First(LastName)}".ToUpperInvariant();
    private static string First(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim()[..1];

    public async Task<bool> HasSessionAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrEmpty(token)) return false;
            FirstName = await SecureStorage.GetAsync("first_name") ?? "";
            LastName = await SecureStorage.GetAsync("last_name") ?? "";
            UserId = ParseUid(token);
            SetToken(token);
            return true;
        }
        catch { return false; }
    }

    public Task<(AuthResponse? Data, string? Error)> LoginAsync(string email, string password) =>
        AuthAsync("auth/login", new { email, password });

    public Task<(AuthResponse? Data, string? Error)> RegisterAsync(string firstName, string lastName, string email, string password) =>
        AuthAsync("auth/register", new { firstName, lastName, email, password });

    private async Task<(AuthResponse? Data, string? Error)> AuthAsync(string url, object body)
    {
        var (data, error) = await SendAsync<AuthResponse>(HttpMethod.Post, url, body);
        if (data is null) return (null, error ?? "Une erreur est survenue.");
        await SecureStorage.SetAsync("token", data.Token);
        await SecureStorage.SetAsync("first_name", data.FirstName);
        await SecureStorage.SetAsync("last_name", data.LastName ?? "");
        FirstName = data.FirstName;
        LastName = data.LastName ?? "";
        UserId = ParseUid(data.Token);
        SetToken(data.Token);
        return (data, null);
    }

    public void Logout()
    {
        SecureStorage.Remove("token");
        SecureStorage.Remove("first_name");
        SecureStorage.Remove("last_name");
        FirstName = "";
        LastName = "";
        UserId = 0;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // Lit l'identifiant de l'utilisateur dans le jeton (utile pour ranger sa photo de profil)
    private static int ParseUid(string token)
    {
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return int.Parse(doc.RootElement.GetProperty("uid").ToString());
        }
        catch { return 0; }
    }

    private void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Méthode générique utilisée par toutes les fonctionnalités (mesures, seuils, rappels)
    public async Task<(T? Data, string? Error)> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            if (body is not null) req.Content = JsonContent.Create(body, mediaType: null, options: Json);
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return (default, await ReadErrorAsync(resp));
            if (resp.StatusCode == HttpStatusCode.NoContent) return (default, null);
            return (await resp.Content.ReadFromJsonAsync<T>(Json), null);
        }
        catch (Exception)
        {
            return (default, "Impossible de joindre le serveur.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorBody>(Json);
            if (!string.IsNullOrEmpty(err?.Message)) return err.Message;
        }
        catch { }
        return resp.StatusCode == HttpStatusCode.Unauthorized
            ? "Session expirée, reconnectez-vous."
            : $"Erreur du serveur ({(int)resp.StatusCode}).";
    }
}
