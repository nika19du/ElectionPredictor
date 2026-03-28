using Microsoft.ML.Data;

namespace ElectionPredictor.Models.ML;

public class ElectionTrainingRow
{
    public float DaysBeforeElection { get; set; }
    public float PollAverage { get; set; }
    public float LatestPollPercentage { get; set; }
    public float PollTrend { get; set; }
    public float PreviousResultPercentage { get; set; }
    public float PollCount { get; set; }
    public float AverageSampleSize { get; set; }

    public string PartyName { get; set; } = string.Empty;

    public float Label { get; set; }
}

public class ElectionPredictionOutput
{
    [ColumnName("Score")]
    public float Score { get; set; }
}