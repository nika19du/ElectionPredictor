namespace ElectionPredictor.Services.Interfaces
{
    public interface IWikipediaApiService
    {
        Task<string?> GetPageContentAsync(string pageTitle);

    }
}
