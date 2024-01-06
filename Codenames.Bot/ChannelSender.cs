using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Codenames.Bot;

public class ChannelSender : IChannelSender
{
    private readonly ChannelSenderOptions options;
    private readonly TelegramBotClient telegramBotClient;

    private int lastGameId;
    private int lastMessageId;

    public ChannelSender(ChannelSenderOptions options, TelegramBotClient telegramBotClient)
    {
        this.options = options;
        this.telegramBotClient = telegramBotClient;
    }

    public async Task SendVoteStartedAsync(Game game)
    {
        var response = GameRenderer.RenderVoteStartedCommon(game);
        if (response == null)
            return;

        lastGameId = game.Id;
        try
        {
            var sentMessage = await telegramBotClient.SendTextMessageAsync(
                options.ChannelId,
                response.Value.Item1,
                replyMarkup: response.Value.Item2,
                parseMode: ParseMode.Html);

            lastMessageId = sentMessage.MessageId;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task SendGameSummaryAsync(Game game)
    {
        var winners = GameManager.GetWinners(game);
        var response = GameRenderer.RenderSummary(game, winners);

        try
        {

            await telegramBotClient.DeleteMessageAsync(options.ChannelId, lastMessageId);
        }
        catch
        {

        }

        try
        {
            await telegramBotClient.SendTextMessageAsync(
                options.ChannelId,
                response,
                parseMode: ParseMode.Html);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}