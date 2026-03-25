using System.Text.Json;
using ElectionPredictor.Services.Interfaces;

namespace ElectionPredictor.Services;

public class WikipediaApiService : IWikipediaApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WikipediaApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetPageContentAsync(string pageTitle)
    {
        var client = _httpClientFactory.CreateClient("Wikipedia");

        var url =
            "w/api.php" +
            "?action=query" +
            "&prop=revisions" +
            $"&titles={Uri.EscapeDataString(pageTitle)}" +
            "&rvslots=main" +
            "&rvprop=content" +
            "&formatversion=2" +
            "&format=json";

        var response = await client.GetAsync(url);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Wikipedia API error: {(int)response.StatusCode} {response.StatusCode}. Response: {responseContent}");
        }

        using var doc = JsonDocument.Parse(responseContent);

        var pages = doc.RootElement
            .GetProperty("query")
            .GetProperty("pages");

        if (pages.GetArrayLength() == 0)
            return null;

        var page = pages[0];

        if (!page.TryGetProperty("revisions", out var revisions))
            return null;

        if (revisions.GetArrayLength() == 0)
            return null;

        return revisions[0]
            .GetProperty("slots")
            .GetProperty("main")
            .GetProperty("content")
            .GetString();
    }
}