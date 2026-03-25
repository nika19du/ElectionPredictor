using ElectionPredictor.Data;
using ElectionPredictor.Models;
using ElectionPredictor.Models.Dtos;
using ElectionPredictor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectionPredictor.Services;

public class PollService : IPollService
{
    private readonly ApplicationDbContext _dbContext;

    public PollService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PollListItemDto>> GetAllAsync()
    {
        return await _dbContext.PollEntries
            .Include(x => x.Election)
            .Include(x => x.Party)
            .OrderByDescending(x => x.PollDate)
            .Select(x => new PollListItemDto
            {
                Id = x.Id,
                ElectionTitle = x.Election.Title,
                PartyName = x.Party.Name,
                Pollster = x.Pollster,
                PollDate = x.PollDate,
                Percentage = x.Percentage,
                SampleSize = x.SampleSize,
                SourceUrl = x.SourceUrl
            })
            .ToListAsync();
    }
}