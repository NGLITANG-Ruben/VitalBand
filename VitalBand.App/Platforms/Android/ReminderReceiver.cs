using Android.App;
using Android.Content;

namespace VitalBand.App.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;
        var id = intent.GetIntExtra("id", 0);
        var type = intent.GetStringExtra("type") ?? "BP";

        AndroidReminderScheduler.Notify(context, id, type);
        // Programme directement la prochaine occurrence
        AndroidReminderScheduler.Schedule(context, id, type,
            intent.GetIntExtra("hour", 8), intent.GetIntExtra("minute", 0), intent.GetIntExtra("mask", 127));
    }
}
