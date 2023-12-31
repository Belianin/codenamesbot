using Codenames.WordProviders;

namespace Codenames
{
    public class GameFactory
    {
        private readonly GameFactorySettings settings;
        private readonly IWordsProvider wordsProvider;

        public GameFactory(GameFactorySettings settings, IWordsProvider wordsProvider)
        {
            this.settings = settings;
            this.wordsProvider = wordsProvider;
        }

        public async Task<Game> CreateGameAsync(IReadOnlySet<string> exlcude)
        {
            var allWords = await wordsProvider.GetWordsAsync();
            var random = new Random();
            
            while (true)
            {
                var riddles = random
                    .Pick(allWords.Where(x => !exlcude.Contains(x)).ToList(), settings.RiddleWords)
                    .ToHashSet();
                var words = random
                    .Pick(allWords.Where(x => !riddles.Contains(x)).ToList(), settings.TotalWords - settings.RiddleWords)
                    .Select(x => new Word() { Value = x })
                    .ToArray();

                var result = riddles
                    .Select(x => new Word
                    {
                        Value = x,
                        IsRiddle = true,
                    })
                    .Concat(words)
                    .OrderBy(x => random.Next())
                    .ToArray();

                return new Game
                {
                    Words = result
                };
            }
        }
    }
}