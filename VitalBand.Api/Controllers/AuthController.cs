using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string FirstName);
public record UserRow(int Id, string FirstName, string PasswordHash);

[ApiController]
[Route("auth")]
public class AuthController(MySqlConnection db, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.FirstName) || string.IsNullOrWhiteSpace(r.LastName) ||
            string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.Password) ||
            r.Password.Length < 6)
            return BadRequest(new { message = "Tous les champs sont obligatoires (mot de passe : 6 caractères minimum)." });

        var email = r.Email.Trim().ToLowerInvariant();
        var exists = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE email=@email", new { email });
        if (exists > 0)
            return Conflict(new { message = "Cet e-mail est déjà utilisé." });

        var hash = BCrypt.Net.BCrypt.HashPassword(r.Password);
        var id = await db.ExecuteScalarAsync<int>(
            "INSERT INTO users(first_name,last_name,email,password_hash) " +
            "VALUES(@FirstName,@LastName,@email,@hash); SELECT LAST_INSERT_ID();",
            new { r.FirstName, r.LastName, email, hash });

        // Seuils par défaut (modifiables ensuite par l'utilisateur)
        await db.ExecuteAsync(
            "INSERT INTO thresholds(user_id,metric,min_value,max_value) VALUES " +
            "(@id,'SYSTOLIC',90,140),(@id,'DIASTOLIC',60,90),(@id,'GLUCOSE',0.70,1.10)",
            new { id });

        return Ok(new AuthResponse(CreateToken(id, email), r.FirstName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        var u = await db.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id AS Id, first_name AS FirstName, password_hash AS PasswordHash " +
            "FROM users WHERE email=@email", new { email });

        if (u is null || !BCrypt.Net.BCrypt.Verify(r.Password ?? "", u.PasswordHash))
            return Unauthorized(new { message = "E-mail ou mot de passe incorrect." });

        return Ok(new AuthResponse(CreateToken(u.Id, email), u.FirstName));
    }

    private string CreateToken(int userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("uid", userId.ToString()),
                new Claim("email", email)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
