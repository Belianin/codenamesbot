namespace Codenames.Bot
{
    public class ScheduledEvent
    {
        public string Name { get; set; }
        public TimeSpan Time { get; set; }
        public Func<Task> RunAsync { get; set; }
    }
}
