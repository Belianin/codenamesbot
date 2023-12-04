namespace Codenames.Bot
{
    internal class Scheduler
    {
        private readonly ScheduledEvent[] events;

        public Scheduler(IEnumerable<ScheduledEvent> events)
        {
            this.events = events
                .OrderBy(x => x.Time)
                .ToArray();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            TimeSpan now;

            try
            {
                //while (!cancellationToken.IsCancellationRequested)
                //{
                //    foreach (var e in events)
                //    {
                //        await e.RunAsync();
                //        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                //    }
                //}

                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var e in events)
                    {
                        now = DateTime.UtcNow.TimeOfDay + TimeSpan.FromHours(3);
                        if (now < e.Time)
                            await Task.Delay(e.Time - now, cancellationToken);

                        await e.RunAsync();
                    }

                    now = DateTime.UtcNow.TimeOfDay + TimeSpan.FromHours(3);
                    //if (now)
                    await Task.Delay(((TimeSpan.FromDays(1) + TimeSpan.FromHours(3)) - now) + TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
