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
        const string electionTitle = "October 2024 Bulgarian parliamentary election";

        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == electionTitle);

        if (election is null)
        {
            election = new Election
            {
                Title = electionTitle,
                Type = "Parliamentary",
                Year = 2024,
                ElectionDate = new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.Elections.Add(election);
            await _dbContext.SaveChangesAsync();
        }

        var rows = GetMockOctober2024PollRows();

        foreach (var row in rows)
        {
            var exists = await _dbContext.PollEntries
                .AnyAsync(x => x.ExternalKey == row.ExternalKey);

            if (exists)
                continue;

            var party = await _dbContext.Parties
                .FirstOrDefaultAsync(x =>
                    x.ShortName == row.PartyShortName ||
                    x.Name == row.PartyName);

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

            _dbContext.PollEntries.Add(new PollEntry
            {
                ElectionId = election.Id,
                PartyId = party.Id,
                Pollster = row.Pollster,
                PollDate = DateTime.SpecifyKind(row.PollDate, DateTimeKind.Utc),
                Percentage = row.Percentage,
                SampleSize = row.SampleSize,
                SourceUrl = row.SourceUrl,
                ExternalKey = row.ExternalKey
            });
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
    }

    public async Task ImportElectionResultsFromParserAsync(
        string pageTitle,
        string electionTitle,
        int year,
        DateTime electionDate)
    {
        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == electionTitle);

        if (election is null)
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

            if (party is null)
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

    public async Task ImportAllHistoricalPollsAsync()
    {
        await ImportMockPollsAsync(
            "April 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 4, 4, 0, 0, 0, DateTimeKind.Utc),
            GetMockApril2021PollRows());

        await ImportMockPollsAsync(
            "July 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            GetMockJuly2021PollRows());

        await ImportMockPollsAsync(
            "November 2021 Bulgarian parliamentary election",
            2021,
            new DateTime(2021, 11, 14, 0, 0, 0, DateTimeKind.Utc),
            GetMockNovember2021PollRows());

        await ImportMockPollsAsync(
            "2022 Bulgarian parliamentary election",
            2022,
            new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            GetMock2022PollRows());

        await ImportMockPollsAsync(
            "2023 Bulgarian parliamentary election",
            2023,
            new DateTime(2023, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            GetMock2023PollRows());

        await ImportMockPollsAsync(
            "June 2024 Bulgarian parliamentary election",
            2024,
            new DateTime(2024, 6, 9, 0, 0, 0, DateTimeKind.Utc),
            GetMockJune2024PollRows());

        await ImportMockPollsAsync(
            "October 2024 Bulgarian parliamentary election",
            2024,
            new DateTime(2024, 10, 27, 0, 0, 0, DateTimeKind.Utc),
            GetMockOctober2024PollRows());
    }

    public async Task Import2026PollsAsync()
    {
        const string pageTitle = "2026_Bulgarian_parliamentary_election";
        var sourceUrl = $"https://en.wikipedia.org/wiki/{pageTitle}";

        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == "2026 Bulgarian parliamentary election");

        if (election is null)
        {
            election = new Election
            {
                Title = "2026 Bulgarian parliamentary election",
                Type = "Parliamentary",
                Year = 2026,
                ElectionDate = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.Elections.Add(election);
            await _dbContext.SaveChangesAsync();
        }

        var html = await _wikipediaParseService.GetParsedHtmlAsync(pageTitle);

        if (string.IsNullOrWhiteSpace(html))
            return;

        var rows = GetMock2026PollRows(sourceUrl);

        foreach (var row in rows)
        {
            var exists = await _dbContext.PollEntries
                .AnyAsync(x => x.ExternalKey == row.ExternalKey);

            if (exists)
                continue;

            var party = await _dbContext.Parties
                .FirstOrDefaultAsync(x =>
                    x.ShortName == row.PartyShortName ||
                    x.Name == row.PartyName);

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

    private async Task ImportMockPollsAsync(
        string electionTitle,
        int year,
        DateTime electionDate,
        List<WikiPollRow> rows)
    {
        var election = await _dbContext.Elections
            .FirstOrDefaultAsync(x => x.Title == electionTitle);

        if (election is null)
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

        foreach (var row in rows)
        {
            var exists = await _dbContext.PollEntries
                .AnyAsync(x => x.ExternalKey == row.ExternalKey);

            if (exists)
                continue;

            var party = await _dbContext.Parties
                .FirstOrDefaultAsync(x =>
                    x.ShortName == row.PartyShortName ||
                    x.Name == row.PartyName);

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

    private static List<WikiPollRow> GetMockApril2021PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/April_2021_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 27.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "apr2021-gerb"
            },
            new()
            {
                PartyName = "There Is Such a People",
                PartyShortName = "ITN",
                Percentage = 16.5m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "apr2021-itn"
            },
            new()
            {
                PartyName = "BSP for Bulgaria",
                PartyShortName = "BSPzB",
                Percentage = 15.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "apr2021-bsp"
            },
            new()
            {
                PartyName = "Democratic Bulgaria",
                PartyShortName = "DB",
                Percentage = 9.2m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "apr2021-db"
            }
        };
    }

    private static List<WikiPollRow> GetMockJuly2021PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/July_2021_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "There Is Such a People",
                PartyShortName = "ITN",
                Percentage = 21.5m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jul2021-itn"
            },
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 23.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jul2021-gerb"
            },
            new()
            {
                PartyName = "BSP for Bulgaria",
                PartyShortName = "BSPzB",
                Percentage = 14.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jul2021-bsp"
            },
            new()
            {
                PartyName = "Democratic Bulgaria",
                PartyShortName = "DB",
                Percentage = 12.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jul2021-db"
            }
        };
    }

    private static List<WikiPollRow> GetMockNovember2021PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/November_2021_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "We Continue the Change",
                PartyShortName = "PP",
                Percentage = 24.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "nov2021-pp"
            },
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 23.5m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "nov2021-gerb"
            },
            new()
            {
                PartyName = "Movement for Rights and Freedoms",
                PartyShortName = "DPS",
                Percentage = 10.8m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "nov2021-dps"
            },
            new()
            {
                PartyName = "BSP for Bulgaria",
                PartyShortName = "BSPzB",
                Percentage = 13.2m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2021, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "nov2021-bsp"
            }
        };
    }

    private static List<WikiPollRow> GetMock2022PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/2022_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 25.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2022-gerb"
            },
            new()
            {
                PartyName = "We Continue the Change",
                PartyShortName = "PP",
                Percentage = 19.8m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2022-pp"
            },
            new()
            {
                PartyName = "Movement for Rights and Freedoms",
                PartyShortName = "DPS",
                Percentage = 13.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2022-dps"
            },
            new()
            {
                PartyName = "Revival",
                PartyShortName = "Revival",
                Percentage = 9.5m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2022-revival"
            }
        };
    }

    private static List<WikiPollRow> GetMock2023PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/2023_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 26.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2023, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2023-gerb"
            },
            new()
            {
                PartyName = "PP–DB",
                PartyShortName = "PP-DB",
                Percentage = 24.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2023, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2023-ppdb"
            },
            new()
            {
                PartyName = "Movement for Rights and Freedoms",
                PartyShortName = "DPS",
                Percentage = 13.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2023, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2023-dps"
            },
            new()
            {
                PartyName = "BSP for Bulgaria",
                PartyShortName = "BSPzB",
                Percentage = 8.8m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2023, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "2023-bsp"
            }
        };
    }

    private static List<WikiPollRow> GetMockJune2024PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/June_2024_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 25.9m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jun2024-gerb"
            },
            new()
            {
                PartyName = "PP–DB",
                PartyShortName = "PP-DB",
                Percentage = 15.1m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jun2024-ppdb"
            },
            new()
            {
                PartyName = "Revival",
                PartyShortName = "Revival",
                Percentage = 14.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "jun2024-revival"
            }
        };
    }

    private static List<WikiPollRow> GetMockOctober2024PollRows()
    {
        const string sourceUrl = "https://en.wikipedia.org/wiki/October_2024_Bulgarian_parliamentary_election";

        return new List<WikiPollRow>
        {
            new()
            {
                PartyName = "GERB–SDS",
                PartyShortName = "GERB-SDS",
                Percentage = 25.52m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-gerb"
            },
            new()
            {
                PartyName = "PP–DB",
                PartyShortName = "PP-DB",
                Percentage = 13.75m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-ppdb"
            },
            new()
            {
                PartyName = "Revival",
                PartyShortName = "Revival",
                Percentage = 13.0m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-revival"
            },
            new()
            {
                PartyName = "DPS–NN",
                PartyShortName = "DPS-NN",
                Percentage = 11.2m,
                Pollster = "Mock historical import",
                PollDate = new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                SampleSize = 1000,
                SourceUrl = sourceUrl,
                ExternalKey = "oct2024-dpsnn"
            }
        };
    }

    private static List<WikiPollRow> GetMock2026PollRows(string sourceUrl)
    {
        return new List<WikiPollRow>
    {
        new()
        {
            PartyName = "GERB–SDS",
            PartyShortName = "GERB-SDS",
            Percentage = 26.4m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-gerb"
        },
        new()
        {
            PartyName = "PP–DB",
            PartyShortName = "PP-DB",
            Percentage = 14.8m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-ppdb"
        },
        new()
        {
            PartyName = "Revival",
            PartyShortName = "Revival",
            Percentage = 13.2m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-revival"
        },
        new()
        {
            PartyName = "DPS–NN",
            PartyShortName = "DPS-NN",
            Percentage = 11.5m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-dpsnn"
        },
        new()
        {
            PartyName = "BSP–OL",
            PartyShortName = "BSP-OL",
            Percentage = 7.4m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-bspol"
        },
        new()
        {
            PartyName = "ITN",
            PartyShortName = "ITN",
            Percentage = 6.3m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-itn"
        },
        new()
        {
            PartyName = "MECh",
            PartyShortName = "MECh",
            Percentage = 3.2m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-mech"
        },
        new()
        {
            PartyName = "Velichie",
            PartyShortName = "Velichie",
            Percentage = 2.4m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-velichie"
        },
        new()
        {
            PartyName = "PB",
            PartyShortName = "PB",
            Percentage = 2.0m,
            Pollster = "Alpha Research",
            PollDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            SampleSize = 1000,
            SourceUrl = sourceUrl,
            ExternalKey = "2026-alpha-pb"
        }
    };
    }
}