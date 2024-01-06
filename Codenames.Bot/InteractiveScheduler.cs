using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    internal class InteractiveScheduler : IGameScheduler
    {
        public async Task RunAsync(ICollection<ScheduledEvent> events, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var e in events)
                {
                    Console.WriteLine($"Start {e.Name}?");
                    Console.ReadKey(true);
                    await e.RunAsync();
                }
            }
        }
    }
}
