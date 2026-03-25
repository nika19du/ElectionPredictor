namespace ElectionPredictor.Models.Dtos
{
    public class PredictionDto
    {
        public string PartyName { get; set; } = string.Empty;
        public decimal PredictedPercentage { get; set; }
    }
}
