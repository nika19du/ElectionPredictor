namespace ElectionPredictor.Data.Entities
{
    public class PollEntry
    {
        public int Id { get; set; }

        public int ElectionId { get; set; }
        public Election Election { get; set; } = null!;

        public int PartyId { get; set; }
        public Party Party { get; set; } = null!;

        // Агенция / източник
        public string Pollster { get; set; } = string.Empty;

        // Дата на публикуване или край на fieldwork
        public DateTime PollDate { get; set; }

        // % за партията
        public decimal Percentage { get; set; }

        // Размер на извадката
        public int? SampleSize { get; set; }

        // Откъде е взето
        public string SourceUrl { get; set; } = string.Empty;

        // За да не записва едно и също 100 пъти
        public string ExternalKey { get; set; } = string.Empty;
    }
}
