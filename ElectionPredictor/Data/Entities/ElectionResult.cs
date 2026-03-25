namespace ElectionPredictor.Data.Entities
{
    public class ElectionResult
    {
        public int Id { get; set; }

        public int ElectionId { get; set; }
        public Election Election { get; set; } = null!;

        public int PartyId { get; set; }
        public Party Party { get; set; } = null!;

        // Реален резултат от изборите
        public decimal VotePercentage { get; set; }

        public int Seats { get; set; }
    }
}
