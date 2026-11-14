using Bloxstrap.Models.Persistable;

namespace Bloxstrap
{
    public static class ProfileScheduler
    {
        public static bool IsActive(Settings settings, DateTime time)
        {
            if (!settings.ScheduleEnabled)
                return false;

            int hour = time.Hour;

            if (settings.ScheduleStartHour == settings.ScheduleEndHour)
                return true;

            if (settings.ScheduleStartHour < settings.ScheduleEndHour)
                return hour >= settings.ScheduleStartHour && hour < settings.ScheduleEndHour;

            return hour >= settings.ScheduleStartHour || hour < settings.ScheduleEndHour;
        }

        public static void ApplyPendingProfile(FastFlagManager manager)
        {
            var settings = App.Settings.Prop;
            string profile = settings.ScheduleProfile;

            if (FlagProfiles.Names.Contains(profile))
                FlagProfiles.Apply(manager, profile);
            else
                App.CustomProfiles.Apply(profile);
        }
    }
}
