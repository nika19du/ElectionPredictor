using ElectionPredictor.Models.Dtos;

namespace ElectionPredictor.Services.Interfaces
{
    public interface IPredictionService
    {
        Task<List<PredictionDto>> GetSimplePredictionAsync(int electionYear);

    }
}
