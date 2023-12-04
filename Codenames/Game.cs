namespace Codenames
{
    public class Game
    {

        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public ICollection<Word> Words { get; set; } = new List<Word>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();

        public GameState State { get; set; }
    }
}

public class GameMessage
{
    public string Text { get; set; }
    public long UserId { get; set; }

    public static implicit operator GameMessage(string text) => new GameMessage { Text = text };
}

public enum RiddleResult
{
    Accepted,
    Changed,
    Rejected
}