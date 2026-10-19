using VitalBand.App.Models;

namespace VitalBand.App.Services;

public interface IReminderScheduler
{
    Task<bool> EnsurePermissionAsync();
    void ScheduleAll(IEnumerable<Reminder> reminders);
    void CancelAll();
    void ShowTest();
}

public class NoOpReminderScheduler : IReminderScheduler
{
    public Task<bool> EnsurePermissionAsync() => Task.FromResult(true);
    public void ScheduleAll(IEnumerable<Reminder> reminders) { }
    public void CancelAll() { }
    public void ShowTest() { }
}
