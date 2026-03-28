using ElectionPredictor.Data;
using ElectionPredictor.Models.Dtos;
using ElectionPredictor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectionPredictor.Services;

public class PredictionService : IPredictionService
{
    private readonly ApplicationDbContext _dbContext;

    public PredictionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PredictionDto>> GetSimplePredictionAsync(int electionYear)
    {
        var now = DateTime.UtcNow;

        var targetElection = await _dbContext.Elections
            .OrderByDescending(x => x.ElectionDate)
            .FirstOrDefaultAsync(x => x.Year == electionYear);

        if (targetElection is null)
            return new List<PredictionDto>();

        var polls = await _dbContext.PollEntries
            .Include(x => x.Party)
            .Where(x => x.ElectionId == targetElection.Id && x.PollDate <= now)
            .ToListAsync();

        var previousElection = await _dbContext.Elections
            .Where(x => x.ElectionDate < targetElection.ElectionDate)
            .OrderByDescending(x => x.ElectionDate)
            .FirstOrDefaultAsync();

        var previousResults = new List<ElectionPredictor.Data.Entities.ElectionResult>();

        if (previousElection is not null)
        {
            previousResults = await _dbContext.ElectionResults
                .Include(x => x.Party)
                .Where(x => x.ElectionId == previousElection.Id)
                .ToListAsync();
        }

        var partyNames = polls.Select(x => x.Party.Name)
            .Union(previousResults.Select(x => x.Party.Name))
            .Distinct()
            .ToList();

        var result = new List<PredictionDto>();

        foreach (var partyName in partyNames)
        {
            var partyPolls = polls
                .Where(x => x.Party.Name == partyName)
                .ToList();

            var lastResult = previousResults
                .FirstOrDefault(x => x.Party.Name == partyName);

            decimal predictedPercentage;

            if (partyPolls.Any())
            {
                double weightedSum = 0;
                double totalWeight = 0;

                foreach (var poll in partyPolls)
                {
                    var daysOld = (now - poll.PollDate).TotalDays;

                    double recencyWeight = daysOld switch
                    {
                        <= 3 => 1.5,
                        <= 7 => 1.3,
                        <= 14 => 1.1,
                        <= 30 => 1.0,
                        _ => 0.8
                    };

                    double sampleWeight = poll.SampleSize switch
                    {
                        >= 1500 => 1.2,
                        >= 1000 => 1.0,
                        > 0 => 0.9,
                        _ => 0.85
                    };

                    var weight = recencyWeight * sampleWeight;

                    weightedSum += (double)poll.Percentage * weight;
                    totalWeight += weight;
                }

                var weightedPollPrediction = totalWeight > 0
                    ? (decimal)(weightedSum / totalWeight)
                    : 0m;

                predictedPercentage = lastResult is not null
                    ? Math.Round(weightedPollPrediction * 0.7m + lastResult.VotePercentage * 0.3m, 2)
                    : Math.Round(weightedPollPrediction, 2);
            }
            else
            {
                predictedPercentage = lastResult?.VotePercentage ?? 0m;
            }

            result.Add(new PredictionDto
            {
                PartyName = partyName,
                PredictedPercentage = predictedPercentage
            });
        }

        return result
            .Where(x => x.PredictedPercentage > 0)
            .OrderByDescending(x => x.PredictedPercentage)
            .ToList();
    }
}