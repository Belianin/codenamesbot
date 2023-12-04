namespace Codenames.WordProviders
{
    public interface IWordsProvider
    {
        Task<IList<string>> GetWordsAsync();
    }
}