using ElectionPredictor.Data;
using ElectionPredictor.Data.Entities;
using ElectionPredictor.Models;
using ElectionPredictor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectionPredictor.Services;

public class PollImportService : IPollImportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWikipediaApiService _wikipediaApiService;

    public PollImportService(
        ApplicationDbContext dbContext,
        IWikipediaApiService wikipediaApiService)
    {
        _dbContext = dbContext;
        _wikipediaApiService = wikipediaApiService;
    }

    public async Task ImportOctober2024ElectionAsync()
    {
        const string pageTitle = "October_2024_Bulgarian_parliamentary_election";
        var sourceUrl = $"https://en.wikipedia.org/wiki/{pageTitle}";

        // 1. Създаваш/намираш изборите
        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == "October 2024 Bulgarian parliamentary election");

        if (election is null)
        {
            election = new Election
            {
                Title = "October 2024 Bulgarian parliamentary election",
                Type = "Parliamentary",
                Year = 2024,
                ElectionDate = new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.Elections.Add(election);
            await _dbContext.SaveChangesAsync();
        }

        // 2. Вземаш суровото съдържание
        var pageContent = await _wikipediaApiService.GetPageContentAsync(pageTitle);

        if (string.IsNullOrWhiteSpace(pageContent))
            return;

        // 3. За начало - временно mock парсване.
        // После тук ще замениш с истински parser.
        var rows = GetMockPollRows(sourceUrl);

        foreach (var row in rows)
        {
            var exists = await _dbContext.PollEntries
                .AnyAsync(x => x.ExternalKey == row.ExternalKey);

            if (exists)
                continue;

            var party = await _dbContext.Parties
                .FirstOrDefaultAsync(x => x.ShortName == row.PartyShortName);

            if (party is null)
            {
                party = new Party
                {
                    Name = row.PartyName,
                    ShortName = row.PartyShortName
                };

                _dbContext.Parties.Add(party);
                await _dbContext.SaveChangesAsync();
            }

            var pollEntry = new PollEntry
            {
                ElectionId = election.Id,
                PartyId = party.Id,
                Pollster = row.Pollster,
                PollDate = DateTime.SpecifyKind(row.PollDate, DateTimeKind.Utc),
                Percentage = row.Percentage,
                SampleSize = row.SampleSize,
                SourceUrl = row.SourceUrl,
                ExternalKey = row.ExternalKey
            };

            _dbContext.PollEntries.Add(pollEntry);
        }

        await _dbContext.SaveChangesAsync();
    }

    private static List<WikiPollRow> GetMockPollRows(string sourceUrl)
    {
        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 25.52m,
                Pollster = "Wikipedia import",
                PollDate = new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = null,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-gerb-results"
            },
            new()
            {
                PartyName = "PP–DB",
                PartyShortName = "PP-DB",
                Percentage = 13.75m,
                Pollster = "Wikipedia import",
                PollDate = new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = null,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-ppdb-results"
            }
        };
    }
}