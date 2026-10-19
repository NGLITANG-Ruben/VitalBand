using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using VitalBand.App.Models;

namespace VitalBand.App.Services;

// Rappels programmés avec AlarmManager + notifications Android (aucune bibliothèque externe)
public class AndroidReminderScheduler : IReminderScheduler
{
    private const string ChannelId = "vitalband_reminders";
    private const string PrefIds = "sched_ids";
    private const string PrefAsked = "notif_asked";
    private static Context Ctx => Platform.AppContext;

    public Task<bool> EnsurePermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return Task.FromResult(true);
        if (Ctx.CheckSelfPermission("android.permission.POST_NOTIFICATIONS") == Android.Content.PM.Permission.Granted)
            return Task.FromResult(true);
        if (!Preferences.Get(PrefAsked, false))
        {
            Preferences.Set(PrefAsked, true);
            Platform.CurrentActivity?.RequestPermissions(new[] { "android.permission.POST_NOTIFICATIONS" }, 1001);
        }
        return Task.FromResult(false);
    }

    public void ScheduleAll(IEnumerable<Reminder> reminders)
    {
        CancelAll();
        var ids = new List<string>();
        foreach (var r in reminders.Where(x => x.IsActive))
        {
            Schedule(Ctx, r.Id, r.Type, r.RemindTime.Hours, r.RemindTime.Minutes, r.DaysMask);
            ids.Add(r.Id.ToString());
        }
        Preferences.Set(PrefIds, string.Join(",", ids));
    }

    public void CancelAll()
    {
        var am = (AlarmManager)Ctx.GetSystemService(Context.AlarmService)!;
        foreach (var s in Preferences.Get(PrefIds, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var pi = PendingIntent.GetBroadcast(Ctx, int.Parse(s),
                new Intent(Ctx, typeof(ReminderReceiver)), PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
            if (pi is null) continue;
            am.Cancel(pi);
            pi.Cancel();
        }
        Preferences.Set(PrefIds, "");
    }

    public void ShowTest() => Notify(Ctx, 999, "BP");

    internal static void Schedule(Context ctx, int id, string type, int hour, int minute, int mask)
    {
        var next = ReminderHelper.NextTrigger(hour, minute, mask, DateTime.Now);
        if (next is null) return;

        var intent = new Intent(ctx, typeof(ReminderReceiver));
        intent.PutExtra("id", id);
        intent.PutExtra("type", type);
        intent.PutExtra("hour", hour);
        intent.PutExtra("minute", minute);
        intent.PutExtra("mask", mask);

        var pi = PendingIntent.GetBroadcast(ctx, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        var am = (AlarmManager)ctx.GetSystemService(Context.AlarmService)!;
        var ms = new DateTimeOffset(next.Value).ToUnixTimeMilliseconds();
        am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, ms, pi);
    }

    internal static void Notify(Context ctx, int id, string type)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var mgr = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;
            if (mgr.GetNotificationChannel(ChannelId) is null)
                mgr.CreateNotificationChannel(new NotificationChannel(ChannelId, "Rappels de mesure", NotificationImportance.High));
        }

        var launch = ctx.PackageManager?.GetLaunchIntentForPackage(ctx.PackageName!);
        var content = launch is null ? null
            : PendingIntent.GetActivity(ctx, id, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var text = type == "BP"
            ? "C’est l’heure de mesurer votre tension."
            : "C’est l’heure de mesurer votre glycémie.";

        var notification = new NotificationCompat.Builder(ctx, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)!
            .SetContentTitle("VitalBand")!
            .SetContentText(text)!
            .SetAutoCancel(true)!
            .SetPriority(NotificationCompat.PriorityHigh)!
            .SetContentIntent(content)!
            .Build()!;

        NotificationManagerCompat.From(ctx)!.Notify(1000 + id, notification);
    }
}
