using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

public class Reminder
{
    public int Id { get; set; }
    public string Type { get; set; } = "";      // BP ou GLYCEMIA
    public TimeSpan RemindTime { get; set; }    // ex. "08:00:00"
    public int DaysMask { get; set; }           // bit 0 = lundi ... bit 6 = dimanche
    public bool IsActive { get; set; } = true;
}

[ApiController, Authorize, Route("reminders")]
public class RemindersController(MySqlConnection db) : ControllerBase
{
    int Uid => int.Parse(User.FindFirst("uid")!.Value);

    static string? Validate(Reminder r)
    {
        if (r.Type != "BP" && r.Type != "GLYCEMIA") return "Type invalide (BP ou GLYCEMIA).";
        if (r.DaysMask is < 1 or > 127) return "Choisissez au moins un jour.";
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.QueryAsync<Reminder>(
            "SELECT id AS Id, type AS Type, remind_time AS RemindTime, days_mask AS DaysMask, " +
            "is_active AS IsActive FROM reminders WHERE user_id=@uid ORDER BY remind_time",
            new { uid = Uid }));

    [HttpPost]
    public async Task<IActionResult> Create(Reminder r)
    {
        var err = Validate(r);
        if (err != null) return BadRequest(new { message = err });
        r.Id = await db.ExecuteScalarAsync<int>(
            "INSERT INTO reminders(user_id,type,remind_time,days_mask,is_active) " +
            "VALUES(@uid,@Type,@RemindTime,@DaysMask,@IsActive); SELECT LAST_INSERT_ID();",
            new { uid = Uid, r.Type, r.RemindTime, r.DaysMask, r.IsActive });
        return Ok(r);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Reminder r)
    {
        var err = Validate(r);
        if (err != null) return BadRequest(new { message = err });
        var n = await db.ExecuteAsync(
            "UPDATE reminders SET type=@Type, remind_time=@RemindTime, days_mask=@DaysMask, " +
            "is_active=@IsActive WHERE id=@id AND user_id=@uid",
            new { id, uid = Uid, r.Type, r.RemindTime, r.DaysMask, r.IsActive });
        if (n == 0) return NotFound();
        r.Id = id;
        return Ok(r);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await db.ExecuteAsync("DELETE FROM reminders WHERE id=@id AND user_id=@uid",
            new { id, uid = Uid });
        return n == 0 ? NotFound() : NoContent();
    }
}
