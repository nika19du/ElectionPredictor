namespace ElectionPredictor.Models.Dtos
{
    public class ElectionResultListItemDto
    {
        public int Id { get; set; }
        public string ElectionTitle { get; set; } = string.Empty;
        public int ElectionYear { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public decimal VotePercentage { get; set; }
        public int Seats { get; set; }
    }
}
