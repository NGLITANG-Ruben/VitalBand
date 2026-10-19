namespace VitalBand.App.Models;

public record AuthResponse(string Token, string FirstName, string? LastName);
public record ErrorBody(string? Message);
