namespace ElectionPredictor.Models
{
    public class WikiPollRow
    {
        public string PartyName { get; set; } = string.Empty;
        public string PartyShortName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string Pollster { get; set; } = string.Empty;
        public DateTime PollDate { get; set; }
        public int? SampleSize { get; set; }
        public string SourceUrl { get; set; } = string.Empty;

        // Уникален ключ за да не вкарва дубликати
        public string ExternalKey { get; set; } = string.Empty;
    }
}
