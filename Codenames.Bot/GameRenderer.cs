using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Codenames.Bot;

internal static class GameRenderer
{
    public const string Gold = "🥇";
    public const string Silver = "🥈";
    public const string Bronze = "🥉";

    public static string GetWordsMessage(Game game)
    {
        return $"Новая игра!\n\n{RenderWords(game)}";
    }

    private static bool CanVote(IEnumerable<Answer> answers, long userId)
    {
        var count = 0;
        var selfVote = false;
        foreach (var answer in answers)
        {
            count++;
            if (answer.User.Id == userId)
                selfVote = true;

            if (count > 1)
                return true;
        }

        return !selfVote;
    }

    public static (string, IReplyMarkup?) RenderVoteStarted(Game game, long userId)
    {
        var voteableWords = game.Answers
            .GroupBy(x => x.Word)
            .Where(x => CanVote(x, userId))
            .Select(x => x.Key)
            .ToArray();

        if (game.Words.Count == 0)
            return ("Никто не прислал ни один вариант...", null);

        var buttons = voteableWords
            .Select(x => new InlineKeyboardButton(x) { CallbackData = x })
            .Batch(2)
            .ToArray();

        var markup = new InlineKeyboardMarkup(buttons);

        var words = RenderWords(game);
        var riddles = RenderRiddles(game);

        return ($"Прием вариантов окончен!\n\nСлова:\n{words}\n\nГолосуйте за лучшую загадку:\n{riddles}", markup);
    }

    public static (string, IReplyMarkup?)? RenderVoteStartedCommon(Game game)
    {
        var voteableWords = game.Answers
            .GroupBy(x => x.Word)
            .Select(x => x.Key)
            .ToArray();

        if (game.Words.Count == 0)
            return null;

        var buttons = voteableWords
            .Select(x => new InlineKeyboardButton(x) { CallbackData = x })
            .Batch(2)
            .ToArray();
        var markup = new InlineKeyboardMarkup(buttons);

        var words = RenderWords(game);
        var riddles = RenderRiddles(game);

        return ($"Игра №{game.Id}\n\nПрием вариантов окончен!\n\nСлова:\n{words}\n\nГолосуйте за лучшую загадку:\n{riddles}", markup);
    }

    public static string RenderSummary(Game game, ICollection<GameWinners> winners)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Игра №{game.Id}");
        summary.AppendLine();
        summary.AppendLine("Слова:");
        summary.AppendLine(RenderWords(game));
        summary.AppendLine();
        summary.AppendLine("Загадки:");
        summary.AppendLine(RenderRiddles(game));
        summary.AppendLine();
        summary.AppendLine("Победители:");
        summary.AppendLine(RenderWinners(winners));

        return summary.ToString();
    }

    public static string RenderWinners(ICollection<GameWinners> winners)
    {
        var result = new StringBuilder();
        foreach (var winner in winners)
        {
            var emoji = winner.Place switch
            {
                1 => $" {Gold}",
                2 => $" {Silver}",
                3 => $" {Bronze}",
                _ => ""
            };

            foreach (var (word, authors) in winner.Answers)
            {
                var authorsString = string.Join(", ", authors.Select(x => $"@{x.Name}"));

                var voteCountString = FormatVotesCount(winner.Votes);

                var author = authors.Count > 1 ? "Авторы" : "Автор";

                var line = $"{winner.Place}.{emoji} <b>{word}</b> — {winner.Votes} {voteCountString}. {author}: {authorsString}";
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    private static int CompareUsers(UserStatistics a, UserStatistics b)
    {
        var sumA = a.Gold * 3 + a.Silver * 2 + a.Bronze;
        var sumB = b.Gold * 3 + b.Silver * 2 + b.Bronze;

        if (sumA != sumB)
            return sumA - sumB;
        if (a.Gold != b.Gold)
            return a.Gold - b.Gold;
        if (a.Silver != b.Silver)
            return a.Silver - b.Silver;
        if (a.Bronze != b.Bronze)
            return a.Bronze - b.Bronze;

        return a.TotalVotes - b.TotalVotes;
    }

    public static string RenderUsersSummary(ICollection<UserStatistics> statistics)
    {
        var winnerStrings = statistics
            .OrderByDescending(x => x.Gold * 3 + x.Silver * 2 + x.Bronze)
            .ThenByDescending(x => x.Gold)
            .ThenByDescending(x => x.Silver)
            .ThenByDescending(x => x.Bronze)
            .ThenByDescending(x => x.TotalVotes)
            .Select((x, i) =>
            {
                var voteCountString = FormatVotesCount(x.TotalVotes);

                return $"{i + 1}. @{x.User.Name} — {Gold.Repeat(x.Gold)}{Silver.Repeat(x.Silver)}{Bronze.Repeat(x.Bronze)}. {x.TotalVotes} {voteCountString}";
            });

        return string.Join("\n", winnerStrings);
    }

    private static string FormatVotesCount(int count)
    {
        if (count > 10 && count < 15)
            return "голосов";

        return (count % 10) switch
        {
            1 => "голос",
            2 or 3 or 4 => "голоса",
            _ => "голосов"
        };
    }

    private static string RenderWords(Game game)
    {
        return string.Join("\n", game.Words.Select(x => x.IsRiddle ? $"- <b>✅ {x.Value}</b>" : $"- {x.Value}"));
    }

    private static string RenderRiddles(Game game)
    {
        return string.Join("\n", game.Answers.Select(x => $"- {x.Word}").Distinct());
    }
}
