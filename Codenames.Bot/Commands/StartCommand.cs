using Telegram.Bot.Types;

public class StartCommand : ICommand
{
    public string Name { get; } = "/start";
    public Task HandleAsync(Message message, GameUpdateHandler handler)
    {
        var name = message.From?.Username ?? "Unknown";
        handler.UsersRepostiory.Add(message.Chat.Id, name);
        return new ShowWordsCommand().HandleAsync(message, handler);
    }
}
