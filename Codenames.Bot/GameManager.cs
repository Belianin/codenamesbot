﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Threading;
using Microsoft.EntityFrameworkCore;

namespace Codenames.Bot
{
    public class GameManager
    {
        private readonly GameSettings settings;
        private readonly CodenamesDbContext dbContext;
        private readonly GameFactory gameFactory;

        private object gameLock = new();

        public GameManager(GameFactory gameFactory, CodenamesDbContext dbContext)
        {
            this.gameFactory = gameFactory;
            this.dbContext = dbContext;
        }

        public event EventHandler<Game> OnGameChanged;
        public event EventHandler<CodenamesDbContext> OnDayEnd;
        public Game Game { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return new Scheduler(new[]
                {
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(9),
                    RunAsync = StartGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(12),
                    RunAsync = StartVoteAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(12.5),
                    RunAsync = EndGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(13),
                    RunAsync = StartGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(16),
                    RunAsync = StartVoteAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(16.5),
                    RunAsync = EndGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(17),
                    RunAsync = StartGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(20),
                    RunAsync = StartVoteAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(20.5),
                    RunAsync = EndGameAsync
                },
                new ScheduledEvent
                {
                    Time = TimeSpan.FromHours(21),
                    RunAsync = () =>
                    {
                        OnDayEnd?.Invoke(this, dbContext);
                        return Task.CompletedTask;
                    }
                }
            }).RunAsync(cancellationToken);
        }

        public RiddleResult AcceptAnswer(long userId, string text)
        {
            var trimmed = text.Trim().ToLower();
            if (trimmed.Contains(' '))
                return RiddleResult.Rejected;

            var user = dbContext.Users.Find(userId);
            if (user == null)
                return RiddleResult.Rejected;

            var currentAnswer = dbContext.Answers
                .FirstOrDefault(x => x.User.Id == userId && x.Game.Id == Game.Id);

            if (currentAnswer == null)
            {
                var answer = new Answer
                {
                    Game = Game,
                    User = user,
                    Word = trimmed,
                    CreationDate = DateTime.UtcNow
                };
                dbContext.Answers.Add(answer);
                dbContext.SaveChanges();

                return RiddleResult.Accepted;
            }
            else
            {
                dbContext.Remove(currentAnswer);
                dbContext.SaveChanges();

                var answer = new Answer
                {
                    Game = Game,
                    User = user,
                    Word = trimmed,
                    CreationDate = DateTime.UtcNow
                };
                dbContext.Answers.Add(answer);
                dbContext.SaveChanges();

                return RiddleResult.Changed;
            }
        }

        public bool Vote(long userId, string text)
        {
            var trimmed = text.Trim().ToLower();

            var answersCount = dbContext.Answers.Where(x => x.Game.Id == Game.Id && x.Word == trimmed).Count();
            if (answersCount == 0)
                return false;

            var user = dbContext.Users.Find(userId);
            if (user == null)
                return false;

            var currentVote = dbContext.Votes
                .FirstOrDefault(x => x.User.Id == userId && x.Game.Id == Game.Id);

            if (currentVote == null)
            {
                var vote = new Vote
                {
                    Game = Game,
                    User = user,
                    Word = trimmed
                };
                dbContext.Votes.Add(vote);
                dbContext.SaveChanges();
            }
            else
            {
                currentVote.Word = trimmed;
                dbContext.SaveChanges();
            }

            return true;
        }

        public static ICollection<GameWinners> GetWinners(Game game)
        {
            var answers = game.Answers.ToLookup(x => x.Word);

            return game.Votes
                .GroupBy(x => x.Word)
                .Select(x => new { Count = x.Count(), Word = x.Key })
                .GroupBy(x => x.Count)
                .Select(x => new GameWinners
                {
                    Votes = x.Key,
                    Answers = x.ToDictionary(y => y.Word, y => (ICollection<User>) answers[y.Word].Select(y => y.User).ToArray())
                })
                .OrderByDescending(x => x.Votes)
                .Select((x, i) =>
                {
                    x.Place = i + 1;
                    return x;
                })
                .ToArray();
        }

        public string GetResultString(Game game)
        {
            var participants = dbContext.Answers
                .Where(x => x.Game.Id == Game.Id)
                .Include(x => x.User)
                .AsNoTracking()
                .ToLookup(x => x.Word);

            var winners = game.Votes
                .GroupBy(x => x.Word)
                .Select(x => new { Count = x.Count(), Word = x.Key })
                .GroupBy(x => x.Count)
                .Select(x => new { Count = x.Key, Words = x.Select(x => x.Word).ToArray() })
                .OrderByDescending(x => x.Count)
                .ToArray();

            if (winners.Length == 0)
                return "Никто не проголосовал...";

            var result = new StringBuilder();
            foreach (var (vote, index) in winners.Select((x, i) => (x, i)))
            {    
                var emoji = index switch
                {
                    0 => " 🥇",
                    1 => " 🥈",
                    2 => " 🥉",
                    _ => ""
                };

                foreach (var word in vote.Words)
                {
                    var authors = string.Join(", ", participants[word].Select(x => $"@{x.User.Name}"));

                    var voteCountString = vote.Count > 10 && vote.Count < 15 ? "голосов" : (vote.Count % 10) switch
                    {
                        1 => "голос",
                        2 or 3 or 4 => "голоса",
                        _ => "голосов"
                    };

                    var author = participants[word].Count() > 1 ? "Авторы" : "Автор";

                    var line = $"{index + 1}.{emoji} <b>{word}</b> — {vote.Count} {voteCountString}. {author}: {authors}";
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        private async Task StartGameAsync()
        {
            var game = await gameFactory.CreateGameAsync();
            lock (gameLock)
            {
                Game = game;
                game.StartedAt = DateTime.UtcNow;
                dbContext.Add(game);
                dbContext.SaveChanges();
            }
            OnGameChanged?.Invoke(this, game);
        }

        private async Task StartVoteAsync()
        {
            lock (gameLock)
            {
                Game.State = GameState.Voting;
                OnGameChanged?.Invoke(this, Game);
            }
        }

        private async Task EndGameAsync()
        {
            lock (gameLock)
            {
                var game = Game;
                game.State = GameState.End;
                game.FinishedAt = DateTime.UtcNow;
                dbContext.SaveChanges();
                OnGameChanged?.Invoke(this, game);
            }
        }
    }
}

