using System;
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
        private readonly IGameScheduler gameScheduler;

        private object gameLock = new();

        public GameManager(GameFactory gameFactory, CodenamesDbContext dbContext, IGameScheduler gameScheduler)
        {
            this.gameFactory = gameFactory;
            this.dbContext = dbContext;
            this.gameScheduler = gameScheduler;
        }

        public event EventHandler<Game> OnGameChanged;
        public event EventHandler<CodenamesDbContext> OnDayEnd;
        public Game Game { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            ScheduledEvent StartGame(double hours)
            {
                return new ScheduledEvent
                {
                    Name = "StartGame",
                    Time = TimeSpan.FromHours(hours),
                    RunAsync = StartGameAsync
                };
            }

            ScheduledEvent StartVote(double hours)
            {
                return new ScheduledEvent
                {
                    Name = "StartVote",
                    Time = TimeSpan.FromHours(hours),
                    RunAsync = StartVoteAsync
                };
            }

            ScheduledEvent EndGame(double hours)
            {
                return new ScheduledEvent
                {
                    Name = "EndGame",
                    Time = TimeSpan.FromHours(hours),
                    RunAsync = EndGameAsync
                };
            }

            ScheduledEvent ShowStatistics(double hours)
            {
                return new ScheduledEvent
                {
                    Name = "ShowStatistics",
                    Time = TimeSpan.FromHours(hours),
                    RunAsync = () =>
                    {
                        OnDayEnd?.Invoke(this, dbContext);
                        return Task.CompletedTask;
                    }
                };
            }

            var lastTime = 7;

            var result = new List<ScheduledEvent>();

            for (var i = 0; i < 3; i++)
            {
                result.Add(StartGame(lastTime));
                result.Add(StartVote(lastTime + 3));
                result.Add(EndGame(lastTime + 3 + 1));
                lastTime += 4;
            }

            result.Add(ShowStatistics(lastTime));

            return gameScheduler.RunAsync(result, cancellationToken);
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

            var answers = dbContext.Answers.Where(x => x.Game.Id == Game.Id && x.Word == trimmed).ToArray();
            if (answers.Length == 0 || (answers.Length == 1 && answers.Single().User.Id == userId))
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

        private async Task StartGameAsync()
        {
            var excludedWords = dbContext.Games
                .OrderByDescending(x => x.FinishedAt)
                .Take(GameFactorySettings.UniqueRiddleGames)
                .SelectMany(x => x.Words)
                .Select(x => x.Value)
                .Distinct()
                .ToHashSet();

            var game = await gameFactory.CreateGameAsync(excludedWords);
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

