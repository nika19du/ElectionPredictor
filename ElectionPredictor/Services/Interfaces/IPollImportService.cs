using ElectionPredictor.Models;

namespace ElectionPredictor.Services.Interfaces
{
    public interface IPollImportService
    {
        Task ImportOctober2024ElectionAsync();

        Task<List<ElectionResultImportRow>> TestParseOctober2024ResultsAsync();

        Task ImportElectionResultsFromParserAsync(
        string pageTitle,
        string electionTitle,
        int year,
        DateTime electionDate);

        Task ImportAllElectionResultsAsync();

    }
}
