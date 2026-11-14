using System.Diagnostics;

namespace Bloxstrap
{
    public class SessionRecord
    {
        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public int Pid { get; set; }

        public int GamesJoined { get; set; }
    }

    public class SessionStatsManager : JsonManager<List<SessionRecord>>
    {
        public override string ClassName => "SessionStats";

        public override string LOG_IDENT_CLASS => nameof(SessionStatsManager);

        public override string FileLocation => Path.Combine(Paths.Base, "Sessions.json");

        public void BeginSession(int pid)
        {
            const string LOG_IDENT = "SessionStatsManager::BeginSession";

            FinalizePendingSessions();

            Prop.Add(new SessionRecord { Start = DateTime.Now, Pid = pid });
            Trim();
            Save();

            App.Logger.WriteLine(LOG_IDENT, $"Session started for pid {pid}");
        }

        public void AddGameJoin()
        {
            var session = Prop.LastOrDefault(x => x.End is null);

            if (session is not null)
                session.GamesJoined++;
        }

        public void FinalizePendingSessions()
        {
            bool changed = false;

            foreach (var session in Prop.Where(x => x.End is null))
            {
                try
                {
                    using var process = Process.GetProcessById(session.Pid);

                    if (!process.HasExited)
                        continue;
                }
                catch (Exception)
                {
                }

                session.End = DateTime.Now;
                changed = true;
            }

            if (changed)
                Trim();
        }

        private void Trim()
        {
            while (Prop.Count > 200)
                Prop.RemoveAt(0);
        }
    }
}
