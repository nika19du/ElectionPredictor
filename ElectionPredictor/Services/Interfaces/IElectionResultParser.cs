using ElectionPredictor.Models;

namespace ElectionPredictor.Services.Interfaces
{
    public interface IElectionResultParser
    {
        List<ElectionResultImportRow> ParseElectionResults(string html);

    }
}
