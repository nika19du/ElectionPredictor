namespace ElectionPredictor.Models
{
    public class ElectionResultImportRow
    {
        public string PartyName { get; set; } = string.Empty;
        public decimal VotePercentage { get; set; }
        public int Seats { get; set; }
    }
}
