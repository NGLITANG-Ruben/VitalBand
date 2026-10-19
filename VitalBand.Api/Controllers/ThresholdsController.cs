using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

public class Threshold
{
    public string Metric { get; set; } = "";    // SYSTOLIC, DIASTOLIC, GLUCOSE
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}

[ApiController, Authorize, Route("thresholds")]
public class ThresholdsController(MySqlConnection db) : ControllerBase
{
    int Uid => int.Parse(User.FindFirst("uid")!.Value);

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.QueryAsync<Threshold>(
            "SELECT metric AS Metric, min_value AS MinValue, max_value AS `MaxValue` " +
            "FROM thresholds WHERE user_id=@uid", new { uid = Uid }));

    [HttpPut]
    public async Task<IActionResult> Save(List<Threshold> items)
    {
        var metrics = new[] { "SYSTOLIC", "DIASTOLIC", "GLUCOSE" };
        foreach (var t in items)
        {
            if (!metrics.Contains(t.Metric))
                return BadRequest(new { message = "Constante inconnue : " + t.Metric });
            if (t.MinValue >= t.MaxValue)
                return BadRequest(new { message = "Le minimum doit être inférieur au maximum." });
        }

        foreach (var t in items)
            await db.ExecuteAsync(
                "INSERT INTO thresholds(user_id,metric,min_value,max_value) " +
                "VALUES(@uid,@Metric,@MinValue,@MaxValue) AS new " +
                "ON DUPLICATE KEY UPDATE min_value=new.min_value, max_value=new.max_value",
                new { uid = Uid, t.Metric, t.MinValue, t.MaxValue });
        return Ok(items);
    }
}
