using System.Text.Json;
using ElectionPredictor.Services.Interfaces;

namespace ElectionPredictor.Services;

public class WikipediaParseService : IWikipediaParseService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WikipediaParseService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetParsedHtmlAsync(string pageTitle)
    {
        var client = _httpClientFactory.CreateClient("Wikipedia");

        var url =
            "w/api.php" +
            "?action=parse" +
            $"&page={Uri.EscapeDataString(pageTitle)}" +
            "&prop=text" +
            "&formatversion=2" +
            "&format=json" +
            "&origin=*";

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Wikipedia parse API error: {(int)response.StatusCode} {response.StatusCode}. Response: {json}");
        }

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("parse", out var parseElement))
            return null;

        if (!parseElement.TryGetProperty("text", out var textElement))
            return null;

        return textElement.GetString();
    }
}