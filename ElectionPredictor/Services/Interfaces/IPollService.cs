using ElectionPredictor.Models.Dtos;

namespace ElectionPredictor.Services.Interfaces
{
    public interface IPollService
    {
        Task<List<PollListItemDto>> GetAllAsync();

    }
}
