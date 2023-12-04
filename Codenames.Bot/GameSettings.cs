public class GameSettings
{
    public TimeSpan GameStart { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan VoteStart { get; set; } = TimeSpan.FromHours(18);
    public TimeSpan GameEnd { get; set; } = TimeSpan.FromHours(21);
}