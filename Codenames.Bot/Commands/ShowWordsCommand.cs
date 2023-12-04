
using Codenames;
using Codenames.Bot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class ShowWordsCommand : ICommand
{
    public string Name => "/words";

    public async Task HandleAsync(Message message, GameUpdateHandler handler)
    {
        if (handler.gameManager.Game == null)
        {
            await handler.botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Игра ещё не началась",
                parseMode: ParseMode.Html);
        }
        else if (handler.gameManager.Game.State == GameState.Voting)
        {
            var answer = GameRenderer.GetVotingMessage(handler.gameManager.Game);
            await handler.botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: answer.Item1,
                parseMode: ParseMode.Html,
                replyMarkup: answer.Item2);
        }
        else if (handler.gameManager.Game.State == GameState.Start)
        {
            var sent = await handler.botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: GameRenderer.GetWordsMessage(handler.gameManager.Game),
                parseMode: ParseMode.Html);
            handler.mainMessageIds[message.Chat.Id] = sent.MessageId;
        }
        else
        {
            await handler.botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Игра ещё не началась",
                parseMode: ParseMode.Html);
        }
    }
}
