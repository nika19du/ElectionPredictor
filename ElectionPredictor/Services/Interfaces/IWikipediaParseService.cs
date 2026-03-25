namespace ElectionPredictor.Services.Interfaces
{
    public interface IWikipediaParseService
    {
        Task<string?> GetParsedHtmlAsync(string pageTitle);

    }
}
