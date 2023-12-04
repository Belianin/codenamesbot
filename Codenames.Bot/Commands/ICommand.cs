using Telegram.Bot.Types;

public interface ICommand
{
    string Name { get; }
    Task HandleAsync(Message message, GameUpdateHandler handler);
}
