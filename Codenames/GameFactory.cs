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

        public async Task<Game> CreateGameAsync()
        {
            var allWords = await wordsProvider.GetWordsAsync();
            var random = new Random();
            
            var words = random
                .Pick(allWords, settings.TotalWords)
                .Select(x => new Word() { Value = x})
                .ToArray();
            foreach (var riddle in random.Pick(words, settings.RiddleWords))
                riddle.IsRiddle = true;

            return new Game { Words = words};
        }
    }
}