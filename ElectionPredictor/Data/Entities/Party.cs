namespace ElectionPredictor.Data.Entities
{
    public class Party
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;

        public ICollection<PollEntry> PollEntries { get; set; } = new List<PollEntry>();
        public ICollection<ElectionResult> ElectionResult { get; set; } = new List<ElectionResult>();

    }
}
