namespace Codenames.Bot
{
    public class ScheduledEvent
    {
        public TimeSpan Time { get; set; }
        public Func<Task> RunAsync { get; set; }
    }
}
