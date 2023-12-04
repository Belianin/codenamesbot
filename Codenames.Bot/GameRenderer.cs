using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Codenames.Bot
{
    internal class GameRenderer
    {
        public static string GetWordsMessage(Game game)
        {
            return $"Новая игра!\n\n{string.Join("\n", game.Words.Select(x => x.IsRiddle ? $"<b>✅ {x.Value}</b>" : x.Value))}";
        }
        public static (string, IReplyMarkup) GetVotingMessage(Game game)
        {
            var uniqueWords = game.Answers.Select(x => x.Word).Distinct().ToArray();

            if (uniqueWords.Length == 0)
            {
                return ("Никто не прислал ни один вариант...", null);
            }
            else
            {
                var buttons = uniqueWords.Select(x => new InlineKeyboardButton(x)
                {
                    CallbackData = x
                }).Batch(2).ToArray();

                var markup = new InlineKeyboardMarkup(buttons);

                var words = string.Join("\n", game.Words.Select(x => x.IsRiddle ? $"<b>✅ {x.Value}</b>" : x.Value));
                var riddles = string.Join("\n", uniqueWords);

                return ($"Прием вариантов окончен!\nСлова:\n{words}\n\nГолосуйте за лучшую загадку:\n{riddles}", markup);
            }
        }
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> elements, int size)
    {
        var count = 0;
        var result = new List<T>(size);
        foreach ( var element in elements)
        {
            result.Add(element);
            count++;
            if (count == size)
            {
                count = 0;
                yield return result;
                result = new List<T>(size);
            }
        }

        if (count != 0)
            yield return result;
    }
}
