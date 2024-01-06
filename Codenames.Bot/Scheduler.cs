namespace Codenames.Bot
{
    public interface IGameScheduler
    {
        public Task RunAsync(ICollection<ScheduledEvent> events, CancellationToken cancellationToken);
    }

    internal class Scheduler : IGameScheduler
    {
        public async Task RunAsync(ICollection<ScheduledEvent> events, CancellationToken cancellationToken)
        {
            TimeSpan now;

            try
            {
                //while (!cancellationToken.IsCancellationRequested)
                //{//
                //    foreach (var e in events)
                //    {
                //        await e.RunAsync();
                //        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                //    }
                // }

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
