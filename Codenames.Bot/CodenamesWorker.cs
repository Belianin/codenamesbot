using Codenames.Bot;
using Telegram.Bot;

public class CodenamesWorker : BackgroundService
{
    private readonly TelegramBotClient telegramBotClient;
    private readonly GameUpdateHandler gameUpdateHandler;

    public CodenamesWorker(TelegramBotClient telegramBotClient, GameUpdateHandler gameUpdateHandler)
    {
        this.telegramBotClient = telegramBotClient;
        this.gameUpdateHandler = gameUpdateHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        telegramBotClient.StartReceiving(
            updateHandler: gameUpdateHandler,
            cancellationToken: stoppingToken
        );

        gameUpdateHandler.Run(telegramBotClient);

        await stoppingToken.WhenCanceled();
    }

}
