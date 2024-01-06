
using Codenames;
using Codenames.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly IChannelSender channelSender;
    private readonly ILogger<GameUpdateHandler> logger;

    public GameUpdateHandler(
        GameManager gameManager,
        IUsersRepostiory usersRepostiory,
        IEnumerable<ICommand> commands,
        ILogger<GameUpdateHandler> logger,
        IChannelSender channelSender)
    {
        this.logger = logger;
        this.gameManager = gameManager;
        this.UsersRepostiory = usersRepostiory;
        this.commands = commands.ToDictionary(x => x.Name, x => x);
        this.channelSender = channelSender;
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
            var games = GetDayGames(context);

            var statistics = CountUserStatistics(games);

            var message = $"Статистика за {DateTime.Today:D}:\n\n{GameRenderer.RenderUsersSummary(statistics)}";

            await SendAllAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: 347495801,
                    text: $"<pre>{ex.Message}</pre>",
                    parseMode: ParseMode.Html);
            }
            catch
            {

            }
        }
    }

    private ICollection<Codenames.Game> GetDayGames(CodenamesDbContext context)
    {
        var today = DateTime.UtcNow.Date;
        var nextDay = today.AddDays(1);

        return context.Games
            .Where(x => x.StartedAt > today && x.FinishedAt.HasValue && x.FinishedAt < nextDay)
            .Include(x => x.Answers)
            .ThenInclude(x => x.User)
            .Include(x => x.Votes)
            .ToArray();
    }

    private ICollection<UserStatistics> CountUserStatistics(IEnumerable<Codenames.Game> games)
    {
        var statistics = new Dictionary<long, UserStatistics>();

        foreach (var game in games)
        {
            var winners = GameManager.GetWinners(game);

            foreach (var winner in winners)
            {
                foreach (var participant in winner.Answers.Values.SelectMany(x => x))
                {
                    if (!statistics.ContainsKey(participant.Id))
                        statistics[participant.Id] = new UserStatistics
                        {
                            User = participant
                        };

                    switch (winner.Place)
                    {
                        case 1:
                            statistics[participant.Id].Gold++;
                            break;
                        case 2:
                            statistics[participant.Id].Silver++;
                            break;
                        case 3:
                            statistics[participant.Id].Bronze++;
                            break;
                    }

                    statistics[participant.Id].TotalVotes += winner.Votes;
                }
            }
        }

        return statistics.Values;
    }

    private Regex commandRegex = new Regex(@"^(/\w+?)(\s|$)");

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery is { } inline)
            {
                var result = HandleVote(update.CallbackQuery.From.Id, inline.Data.ToLower().Trim());
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, text: result);
                return;
            }

            if (update.Message is not { } message)
                return;
            if (message.Text == null && message.Caption == null)
                return;
            var messageText = message.Text?.Trim() ?? message.Caption.Trim();

            Console.WriteLine($"Received '{message.Text}' from '{message.From.Id}'");

            var chatId = message.Chat.Id;

            var regexMatch = commandRegex.Match(messageText);
            if (regexMatch.Success && commands.TryGetValue(regexMatch.Groups[1].Value, out var command))
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
            {
                try
                {
                    var result = HandleVote(chatId, messageText);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: result,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
                catch
                {

                }
            }
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

    private string HandleVote(long chatId, string vote)
    {
        var voted = gameManager.Vote(chatId, vote);
        if (voted)
        {
            return $"Вы проголосовали за <b>{vote}</b>";
        }
        else
        {
            return "Такой загадки нет или вы проголосовали за себя";
        }
    }

    private async Task HandleGameStaredAsync(Codenames.Game game)
    {
        var ids = UsersRepostiory.GetAllIds();
        var text = GameRenderer.GetWordsMessage(game);
        foreach (var id in ids)
        {
            try
            {
                var sent = await botClient.SendTextMessageAsync(
                    chatId: id,
                    text: text,
                    parseMode: ParseMode.Html);
                mainMessageIds[id] = sent.MessageId;
            }
            catch
            {

            }
        }
    }

    private async Task HandleVoteStaredAsync(Codenames.Game game)
    {
        Console.WriteLine("Voting started");

        foreach (var id in UsersRepostiory.GetAllIds())
        {
            try
            {
                var (text, markup) = GameRenderer.RenderVoteStarted(game, id);
                await botClient.SendTextMessageAsync(id, text, replyMarkup: markup, parseMode: ParseMode.Html);
            }
            catch
            {

            }
        }

        try
        {
            await channelSender.SendVoteStartedAsync(game);
        }
        catch
        {

        }
    }
    private async Task HandleGameEndedAsync(Codenames.Game game)
    {
        Console.WriteLine("Game ended");

        var winners = GameManager.GetWinners(game);
        var response = winners.Count == 0 ? "Никто не проголосовал..." : GameRenderer.RenderWinners(winners);

        await SendAllAsync(response);

        try
        {
            await channelSender.SendGameSummaryAsync(game);
        }
        catch
        {

        }
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
    
    internal async Task SendAllAsync(string text, IReplyMarkup? replyMarkup = null, ParseMode parseMode = ParseMode.Html, InputFile? photo = null)
    {
        var ids = UsersRepostiory.GetAllIds();

        foreach (var id in ids)
        {
            try
            {
                if (photo == null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: id,
                        text: text,
                        parseMode: parseMode,
                        replyMarkup: replyMarkup);

                }
                else
                {
                    await botClient.SendPhotoAsync(
                        chatId: id,
                        photo: photo,
                        caption: text,
                        parseMode: parseMode,
                        replyMarkup: replyMarkup);
                }
            }
            catch
            {

            }
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}