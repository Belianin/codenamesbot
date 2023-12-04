namespace Codenames.WordProviders
{
    public class CacheWordsProvider : IWordsProvider
    {
        private readonly CacheWordsProviderSettings settings;
        private readonly IWordsProvider wordsProvider;
        private IList<string> cache;
        private DateTime updatedAt;

        public CacheWordsProvider(CacheWordsProviderSettings settings, IWordsProvider wordsProvider)
        {
            this.settings = settings;
            this.wordsProvider = wordsProvider;
        }

        public async Task<IList<string>> GetWordsAsync()
        {
            var now = DateTime.Now;

            if (cache == null || now > updatedAt.Add(settings.UpdateInterval))
            {
                cache = await wordsProvider.GetWordsAsync();
                updatedAt = now;
            }

            return cache;
        }
    }
}