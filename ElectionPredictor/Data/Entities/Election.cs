namespace ElectionPredictor.Data.Entities
{
    public class Election
    {
        public int Id { get; set; }
        public string Type { get; set; } = "Parlamentary";
        public int Year { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ElectionDate { get; set; }
        public ICollection<PollEntry> PollEntries { get; set; } = new List<PollEntry>();
        public ICollection<ElectionResult> Results { get; set; } = new List<ElectionResult>();
    }
}
