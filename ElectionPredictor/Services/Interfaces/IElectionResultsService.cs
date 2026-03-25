using ElectionPredictor.Models.Dtos;

namespace ElectionPredictor.Services.Interfaces
{
    public interface IElectionResultsService
    {
        Task<List<ElectionResultListItemDto>> GetAllAsync();
    }
}
