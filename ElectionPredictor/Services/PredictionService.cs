using ElectionPredictor.Data;
using ElectionPredictor.Models;
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
        var polls = await _dbContext.PollEntries
            .Include(x => x.Party)
            .Include(x => x.Election)
            .Where(x => x.Election.Year == electionYear)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var result = polls
            .GroupBy(x => x.Party.Name)
            .Select(g =>
            {
                double weightedSum = 0;
                double totalWeight = 0;

                foreach (var poll in g)
                {
                    var daysOld = (now - poll.PollDate).TotalDays;

                    double recencyWeight = daysOld switch
                    {
                        <= 3 => 1.5,
                        <= 7 => 1.3,
                        <= 14 => 1.1,
                        _ => 0.8
                    };

                    double sampleWeight = poll.SampleSize switch
                    {
                        >= 1500 => 1.2,
                        >= 1000 => 1.0,
                        _ => 0.9
                    };

                    var weight = recencyWeight * sampleWeight;

                    weightedSum += (double)poll.Percentage * weight;
                    totalWeight += weight;
                }

                return new PredictionDto
                {
                    PartyName = g.Key,
                    PredictedPercentage = (decimal)(weightedSum / totalWeight)
                };
            })
            .OrderByDescending(x => x.PredictedPercentage)
            .ToList();

        return result;
    }
}