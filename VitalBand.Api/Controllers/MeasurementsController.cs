using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

public class Measurement
{
    public int Id { get; set; }
    public string Type { get; set; } = "";      // BP ou GLYCEMIA
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public decimal? Glucose { get; set; }       // g/L
    public string? Context { get; set; }        // FASTING, BEFORE_MEAL, AFTER_MEAL
    public DateTime MeasuredAt { get; set; }
    public string? Comment { get; set; }
}

[ApiController, Authorize, Route("measurements")]
public class MeasurementsController(MySqlConnection db) : ControllerBase
{
    int Uid => int.Parse(User.FindFirst("uid")!.Value);

    const string Select =
        "SELECT id AS Id, type AS Type, systolic AS Systolic, diastolic AS Diastolic, " +
        "pulse AS Pulse, glucose AS Glucose, context AS Context, measured_at AS MeasuredAt, " +
        "comment AS Comment FROM measurements";

    static string? Validate(Measurement m)
    {
        if (m.Type == "BP")
        {
            if (m.Systolic is null || m.Diastolic is null)
                return "Systolique et diastolique sont obligatoires.";
            if (m.Systolic is < 50 or > 300 || m.Diastolic is < 30 or > 200 || m.Systolic <= m.Diastolic)
                return "Valeurs de tension improbables.";
        }
        else if (m.Type == "GLYCEMIA")
        {
            if (m.Glucose is null) return "La glycémie est obligatoire.";
            if (m.Glucose is < 0.2m or > 6m) return "Valeur de glycémie improbable.";
        }
        else return "Type invalide (BP ou GLYCEMIA).";

        if (m.Context != null && !new[] { "FASTING", "BEFORE_MEAL", "AFTER_MEAL" }.Contains(m.Context))
            return "Moment de mesure invalide.";
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List(string? type,
        [FromQuery(Name = "from")] DateTime? fromDate, [FromQuery(Name = "to")] DateTime? toDate)
    {
        var sql = Select + " WHERE user_id=@uid AND (@type IS NULL OR type=@type)" +
                  " AND (@fromDate IS NULL OR measured_at>=@fromDate)" +
                  " AND (@toDate IS NULL OR measured_at<=@toDate) ORDER BY measured_at DESC";
        return Ok(await db.QueryAsync<Measurement>(sql, new { uid = Uid, type, fromDate, toDate }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Measurement m)
    {
        var err = Validate(m);
        if (err != null) return BadRequest(new { message = err });
        if (m.MeasuredAt == default) m.MeasuredAt = DateTime.Now;

        m.Id = await db.ExecuteScalarAsync<int>(
            "INSERT INTO measurements(user_id,type,systolic,diastolic,pulse,glucose,context,measured_at,comment) " +
            "VALUES(@uid,@Type,@Systolic,@Diastolic,@Pulse,@Glucose,@Context,@MeasuredAt,@Comment); " +
            "SELECT LAST_INSERT_ID();",
            new { uid = Uid, m.Type, m.Systolic, m.Diastolic, m.Pulse, m.Glucose, m.Context, m.MeasuredAt, m.Comment });
        return Ok(m);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Measurement m)
    {
        var err = Validate(m);
        if (err != null) return BadRequest(new { message = err });

        var n = await db.ExecuteAsync(
            "UPDATE measurements SET type=@Type, systolic=@Systolic, diastolic=@Diastolic, pulse=@Pulse, " +
            "glucose=@Glucose, context=@Context, measured_at=@MeasuredAt, comment=@Comment " +
            "WHERE id=@id AND user_id=@uid",
            new { id, uid = Uid, m.Type, m.Systolic, m.Diastolic, m.Pulse, m.Glucose, m.Context, m.MeasuredAt, m.Comment });
        if (n == 0) return NotFound();
        m.Id = id;
        return Ok(m);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await db.ExecuteAsync("DELETE FROM measurements WHERE id=@id AND user_id=@uid",
            new { id, uid = Uid });
        return n == 0 ? NotFound() : NoContent();
    }
}
