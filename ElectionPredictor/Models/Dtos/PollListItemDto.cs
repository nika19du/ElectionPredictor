namespace ElectionPredictor.Models.Dtos
{
    public class PollListItemDto
    {
        public int Id { get; set; }
        public string ElectionTitle { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string Pollster { get; set; } = string.Empty;
        public DateTime PollDate { get; set; }
        public decimal Percentage { get; set; }
        public int? SampleSize { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
    }
}
