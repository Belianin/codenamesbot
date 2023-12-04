using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class StopCommand : ICommand
{
    public string Name => "/stop";

    public Task HandleAsync(Message message, GameUpdateHandler handler)
    {
        handler.UsersRepostiory.Delete(message.Chat.Id);

        return handler.botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Игра для вас поставлена на паузу",
            parseMode: ParseMode.Html);
    }
}