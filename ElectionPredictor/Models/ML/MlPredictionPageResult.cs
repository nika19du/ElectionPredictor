namespace ElectionPredictor.Models.ML
{
    public class MlPredictionPageResult
    {
        public double? RSquared { get; set; }
        public double? RootMeanSquaredError { get; set; }
        public double? MeanAbsoluteError { get; set; }
        public int TrainingRowCount { get; set; }
        public List<MlPartyPrediction> Predictions { get; set; } = new();
    }
}
