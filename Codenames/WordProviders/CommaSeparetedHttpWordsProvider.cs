namespace Codenames.WordProviders
{
    public class CommaSeparetedHttpWordsProvider : IWordsProvider
    {
        private readonly HttpClient httpClient;

        public CommaSeparetedHttpWordsProvider(Uri uri)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = uri;
        }

        public async Task<IList<string>> GetWordsAsync()
        {
            var response = await httpClient.GetAsync((Uri?)null);

            var content = await response.Content.ReadAsStringAsync()!;

            return content.Split(",");
        }
    }
}