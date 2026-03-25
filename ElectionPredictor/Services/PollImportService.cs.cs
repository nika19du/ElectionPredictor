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
    private readonly IWikipediaParseService _wikipediaParseService;
    private readonly IElectionResultParser _electionResultParser;

    public PollImportService(
        ApplicationDbContext dbContext,
        IWikipediaApiService wikipediaApiService,
        IWikipediaParseService wikipediaParseService,
        IElectionResultParser electionResultParser)
    {
        _dbContext = dbContext;
        _wikipediaApiService = wikipediaApiService;
        _wikipediaParseService = wikipediaParseService;
        _electionResultParser = electionResultParser;
    }

    public async Task ImportOctober2024ElectionAsync()
    {
        const string pageTitle = "October_2024_Bulgarian_parliamentary_election";
        var sourceUrl = $"https://en.wikipedia.org/wiki/{pageTitle}";

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

        var pageContent = await _wikipediaApiService.GetPageContentAsync(pageTitle);

        if (string.IsNullOrWhiteSpace(pageContent))
            return;

        // Засега още е mock import
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

    public async Task<List<ElectionResultImportRow>> TestParseOctober2024ResultsAsync()
    {
        var html = await _wikipediaParseService
            .GetParsedHtmlAsync("October_2024_Bulgarian_parliamentary_election");

        if (string.IsNullOrWhiteSpace(html))
            return new List<ElectionResultImportRow>();

        var results = _electionResultParser.ParseElectionResults(html);

        return results;
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

    public async Task ImportAllElectionResultsAsync()
    {
        await ImportElectionResultsFromParserAsync(
            "April_2021_Bulgarian_parliamentary_election",
            "April 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 4, 4, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "July_2021_Bulgarian_parliamentary_election",
            "July 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 7, 11, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "November_2021_Bulgarian_parliamentary_election",
            "November 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 11, 14, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "2022_Bulgarian_parliamentary_election",
            "2022 Bulgarian parliamentary election",
            2022,
            new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "2023_Bulgarian_parliamentary_election",
            "2023 Bulgarian parliamentary election",
            2023,
            new DateTime(2023, 4, 2, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "June_2024_Bulgarian_parliamentary_election",
            "June 2024 Bulgarian parliamentary election",
            2024,
            new DateTime(2024, 6, 9, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "October_2024_Bulgarian_parliamentary_election",
            "October 2024 Bulgarian parliamentary election",
            2024,
            new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc));

        await ImportElectionResultsFromParserAsync(
            "2026_Bulgarian_parliamentary_election",
            "2026 Bulgarian parliamentary election",
            2026,
            new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));
    }

    public async Task ImportElectionResultsFromParserAsync(
    string pageTitle,
    string electionTitle,
    int year,
    DateTime electionDate)
    {
        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == electionTitle);

        if (election == null)
        {
            election = new Election
            {
                Title = electionTitle,
                Type = "Parliamentary",
                Year = year,
                ElectionDate = DateTime.SpecifyKind(electionDate, DateTimeKind.Utc)
            };

            _dbContext.Elections.Add(election);
            await _dbContext.SaveChangesAsync();
        }

        var html = await _wikipediaParseService.GetParsedHtmlAsync(pageTitle);

        if (string.IsNullOrWhiteSpace(html))
            return;

        var parsedResults = _electionResultParser.ParseElectionResults(html);

        foreach (var row in parsedResults)
        {
            var party = await _dbContext.Parties
                .FirstOrDefaultAsync(x => x.Name == row.PartyName);

            if (party == null)
            {
                party = new Party
                {
                    Name = row.PartyName,
                    ShortName = row.PartyName
                };

                _dbContext.Parties.Add(party);
                await _dbContext.SaveChangesAsync();
            }

            var exists = await _dbContext.ElectionResults
                .AnyAsync(x => x.ElectionId == election.Id && x.PartyId == party.Id);

            if (exists)
                continue;

            _dbContext.ElectionResults.Add(new ElectionResult
            {
                ElectionId = election.Id,
                PartyId = party.Id,
                VotePercentage = row.VotePercentage,
                Seats = row.Seats
            });
        }

        await _dbContext.SaveChangesAsync();
    }

}