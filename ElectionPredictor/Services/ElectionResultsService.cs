using ElectionPredictor.Data;
using ElectionPredictor.Models;
using ElectionPredictor.Models.Dtos;
using ElectionPredictor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectionPredictor.Services;

public class ElectionResultsService : IElectionResultsService
{
    private readonly ApplicationDbContext _dbContext;

    public ElectionResultsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ElectionResultListItemDto>> GetAllAsync()
    {
        return await _dbContext.ElectionResults
            .Include(x => x.Election)
            .Include(x => x.Party)
            .OrderByDescending(x => x.Election.ElectionDate)
            .ThenByDescending(x => x.VotePercentage)
            .Select(x => new ElectionResultListItemDto
            {
                Id = x.Id,
                ElectionTitle = x.Election.Title,
                ElectionYear = x.Election.Year,
                PartyName = x.Party.Name,
                VotePercentage = x.VotePercentage,
                Seats = x.Seats
            })
            .ToListAsync();
    }
}