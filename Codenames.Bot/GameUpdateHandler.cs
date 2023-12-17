
using Codenames;
using Codenames.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class GameUpdateHandler : IUpdateHandler
{
    public IUsersRepostiory UsersRepostiory { get; }
    internal TelegramBotClient botClient;

    internal GameManager gameManager;

    public readonly Dictionary<long, int> mainMessageIds = new();
    private readonly Dictionary<string, ICommand> commands;
    private readonly ILogger<GameUpdateHandler> logger;

    public GameUpdateHandler(
        GameManager gameManager,
        IUsersRepostiory usersRepostiory,
        IEnumerable<ICommand> commands,
        ILogger<GameUpdateHandler> logger)
    {
        this.logger = logger;
        this.gameManager = gameManager;
        this.UsersRepostiory = usersRepostiory;
        this.commands = commands.ToDictionary(x => x.Name, x => x);

    }

    public void Run(TelegramBotClient botClient)
    {
        this.botClient = botClient;
        gameManager.OnGameChanged += async (sender, game) => await HandleGameUpdateAsync(game);
        gameManager.OnDayEnd += async (sender, game) => await HandleDayEndAsync(game);
        gameManager.RunAsync(CancellationToken.None).Wait();
    }

    private async Task HandleDayEndAsync(CodenamesDbContext context)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var nextDay = today.AddDays(1);
            var games = context.Games
                .Where(x => x.StartedAt > today && x.FinishedAt.HasValue && x.FinishedAt < nextDay)
                .Include(x => x.Answers)
                .ThenInclude(x => x.User)
                .Include(x => x.Votes)
                .ToArray();

            var goldMedals = new Dictionary<long, int>();
            var sivlerMedals = new Dictionary<long, int>();
            var bronzeMedals = new Dictionary<long, int>();

            foreach (var game in games)
            {
                var winners = GameManager.GetWinners(game);

                foreach (var winner in winners)
                {
                    var dict = winner.Place switch
                    {
                        1 => goldMedals,
                        2 => sivlerMedals,
                        3 => bronzeMedals
                    };

                    foreach (var participan in winner.Answers.Values.SelectMany(x => x))
                    {
                        if (!dict.ContainsKey(participan.Id))
                            dict[participan.Id] = 1;
                        else
                            dict[participan.Id]++;
                    }
                }
            }

            string Repeat(string text, int count)
            {
                var stringBuilder = new StringBuilder();

                for (var i = 0; i < count; i++)
                {
                    stringBuilder.Append(text);
                }

                return stringBuilder.ToString();
            }

            var allWinnerIds = goldMedals.Keys.Concat(sivlerMedals.Keys).Concat(bronzeMedals.Keys)
                .Distinct()
                .Select(x => new { Id = x, Gold = goldMedals.GetValueOrDefault(x), Silver = sivlerMedals.GetValueOrDefault(x), Bronze = bronzeMedals.GetValueOrDefault(x) })
                .OrderByDescending(x => x.Gold * 3 + x.Silver * 2 + x.Bronze)
                .Select((x, i) =>
                {
                    return $"{i + 1}. @{users[x.Id].Name} — {Repeat("🥇", x.Gold)}{Repeat("🥈", x.Silver)}{Repeat("🥉", x.Bronze)}";
                });

            var message = $"Статистика за {DateTime.Today:D}:\n\n{string.Join("\n", allWinnerIds)}";

            await SendAllAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await botClient.SendTextMessageAsync(
                chatId: 347495801,
                text: $"<pre>{ex.Message}</pre>",
                parseMode: ParseMode.Html);
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery is { } inline)
            {
                await HandleVoteAsync(update.CallbackQuery.Message.Chat.Id, inline.Data.ToLower().Trim(), cancellationToken);
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            messageText = messageText.Trim();

            Console.WriteLine($"Received '{message.Text}' from '{message.From.Id}'");

            var chatId = message.Chat.Id;

            if (commands.TryGetValue(messageText, out var command))
            {
                await command.HandleAsync(message, this);
                return;
            }

            if (gameManager.Game == null || !UsersRepostiory.Has(chatId))
                return;

            if (messageText.Any(x => !char.IsLetter(x)))
            {
                await botClient.SendTextMessageAsync(chatId, "Пожалуйста, пришлите слово состоящее только из букв");
                return;
            }    
            if (messageText.Length > 20)
            {
                await botClient.SendTextMessageAsync(chatId, "Пожалуйста, пришлите слово короче 21 символа");
                return;
            }

            Console.WriteLine($"Game state is '{gameManager.Game.State}'");
            if (gameManager.Game.State == GameState.Start)
                await HandleRiddleAsync(chatId, messageText, cancellationToken);
            else if (gameManager.Game.State == GameState.Voting)
                await HandleVoteAsync(chatId, messageText, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private Task HandleGameUpdateAsync(Codenames.Game game)
    {
        logger.LogInformation("Game state is changed to {state}", game.State);
        if (game.State == GameState.Start)
            return HandleGameStaredAsync(game);
        else if (game.State == GameState.Voting)
            return HandleVoteStaredAsync(game);
        else if (game.State == GameState.End)
            return HandleGameEndedAsync(game);

        return Task.CompletedTask;
    }

    private async Task HandleRiddleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var result = gameManager.AcceptAnswer(chatId, text);

        if (result == RiddleResult.Rejected)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Загадайте слова <b>одним</b> словом",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Слово <b>{text}</b> принято!\nЕсли хотите изменить загадку, просто пришлите новое слово.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            if (result == RiddleResult.Accepted)
            {
                var count = gameManager.Game.Answers.Count;
                var countString = count > 10 && count < 15 ? "ответов" : (count % 10) switch
                {
                    1 => "ответ",
                    2 or 3 or 4 => "ответа",
                    _ => "ответов"
                };
                var newText = $"Новая игра!\n\n{string.Join("\n", gameManager.Game.Words.Select(x => x.IsRiddle ? $"<b>✅ {x.Value}</b>" : x.Value))}\n\nПолучено <b>{count}</b> {countString}";
                await UpdateMainMessagesAsync(newText);
            }
        }
    }

    private async Task HandleVoteAsync(long chatId, string vote, CancellationToken cancellationToken)
    {
        var voted = gameManager.Vote(chatId, vote);
        if (voted)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Вы проголосовали за <b>{vote}</b>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Такой загадки нет!",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleGameStaredAsync(Codenames.Game game)
    {
        var ids = UsersRepostiory.GetAllIds();
        var text = GameRenderer.GetWordsMessage(game);
        foreach (var id in ids)
        {
            var sent = await botClient.SendTextMessageAsync(
                chatId: id,
                text: text,
                parseMode: ParseMode.Html);
            mainMessageIds[id] = sent.MessageId;
        }
    }

    private async Task HandleVoteStaredAsync(Codenames.Game game)
    {
        Console.WriteLine("Voting started");

        var uniqueWords = game.Answers?.Select(x => x.Word.ToLower()).Distinct().ToArray() ?? Array.Empty<string>();

        if (uniqueWords.Length == 0)
        {
            await SendAllAsync("Никто не прислал ни один вариант...");
        }
        else
        {
            var (text, markup) = GameRenderer.GetVotingMessage(game);

            var words = string.Join("\n", game.Words.Select(x => x.IsRiddle ? $"<b>✅ {x.Value}</b>" : x.Value));
            var riddles = string.Join("\n", uniqueWords);
            await SendAllAsync(text, replyMarkup: markup);
        }
    }
    private async Task HandleGameEndedAsync(Codenames.Game game)
    {
        Console.WriteLine("Game ended");

        var response = gameManager.GetResultString(game);
        await SendAllAsync(response);
    }

    private async Task UpdateMainMessagesAsync(string text)
    {
        foreach (var (chatId, messageId) in mainMessageIds)
        {
            try
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    messageId,
                    text: text,
                    parseMode: ParseMode.Html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    
    internal async Task SendAllAsync(string text, IReplyMarkup? replyMarkup = null)
    {
        var ids = UsersRepostiory.GetAllIds();

        foreach (var id in ids)
        {
            await botClient.SendTextMessageAsync(
                chatId: id,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}