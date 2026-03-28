using ElectionPredictor.Data;
using ElectionPredictor.Data.Entities;
using ElectionPredictor.Models.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ElectionPredictor.Services;

public class MlPredictionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MLContext _mlContext;

    public MlPredictionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Normalizes party names so that small textual differences
    /// do not break matching between polls and election results.
    /// Example:
    /// "GERB–SDS" -> "gerb-sds"
    /// "GERB - SDS" -> "gerb-sds"
    /// </summary>
    private static string NormalizePartyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name
            .Replace("–", "-")
            .Replace("—", "-")
            .Replace("−", "-")
            .Replace(" ", "")
            .Trim()
            .ToLowerInvariant();
    }

    /// <summary>
    /// Builds one ML training row for a single party in a given election.
    /// The row is based on aggregated poll information plus the previous election result.
    /// The label is the real vote percentage from the current election.
    /// </summary>
    private static ElectionTrainingRow? BuildTrainingRowForParty(
        Election election,
        string normalizedPartyName,
        List<PollEntry> partyPolls,
        List<ElectionResult> currentResults,
        List<ElectionResult> previousResults)
    {
        if (!partyPolls.Any())
            return null;

        // Find the actual election result for the same party in the current election.
        // Matching is done by normalized party name, not by PartyId.
        var actualResult = currentResults.FirstOrDefault(x =>
            x.Party != null &&
            NormalizePartyName(x.Party.Name) == normalizedPartyName);

        if (actualResult is null)
            return null;

        // Find the same party in the previous election results.
        var previousResult = previousResults.FirstOrDefault(x =>
            x.Party != null &&
            NormalizePartyName(x.Party.Name) == normalizedPartyName);

        var orderedPolls = partyPolls
            .OrderBy(x => x.PollDate)
            .ToList();

        if (!orderedPolls.Any())
            return null;

        var latestPoll = orderedPolls.Last();
        var oldestPoll = orderedPolls.First();

        // Calculate how many days before election the latest poll was published.
        // If the value becomes slightly negative due to timezone differences,
        // clamp it to 0 instead of discarding the row.
        var daysBeforeElection = (float)(election.ElectionDate - latestPoll.PollDate).TotalDays;
        daysBeforeElection = Math.Max(daysBeforeElection, 0);

        var pollAverage = orderedPolls.Average(x => (float)x.Percentage);
        var latestPollPercentage = (float)latestPoll.Percentage;
        var pollTrend = (float)(latestPoll.Percentage - oldestPoll.Percentage);
        var pollCount = orderedPolls.Count;
        var averageSampleSize = (float)orderedPolls.Average(x => x.SampleSize ?? 0);

        return new ElectionTrainingRow
        {
            DaysBeforeElection = daysBeforeElection,
            PollAverage = pollAverage,
            LatestPollPercentage = latestPollPercentage,
            PollTrend = pollTrend,
            PreviousResultPercentage = previousResult is null ? 0f : (float)previousResult.VotePercentage,
            PollCount = pollCount,
            AverageSampleSize = averageSampleSize,
            PartyName = normalizedPartyName,

            // Label = real final result for this party in this election
            Label = (float)actualResult.VotePercentage
        };
    }

    /// <summary>
    /// Builds the ML dataset from historical elections.
    /// IMPORTANT:
    /// Training data can only be built for elections where we have:
    /// 1) polls for that election
    /// 2) real election results for that same election
    /// 3) previous election results
    /// </summary>
    public async Task<List<ElectionTrainingRow>> BuildTrainingDataAsync()
    {
        var rows = new List<ElectionTrainingRow>();

        var elections = await _dbContext.Elections
            .OrderBy(x => x.ElectionDate)
            .ToListAsync();

        foreach (var election in elections)
        {
            // We need a previous election to provide PreviousResultPercentage.
            var previousElection = elections
                .Where(x => x.ElectionDate < election.ElectionDate)
                .OrderByDescending(x => x.ElectionDate)
                .FirstOrDefault();

            if (previousElection is null)
                continue;

            // Load polls for the current election.
            var polls = await _dbContext.PollEntries
                .Include(x => x.Party)
                .Where(x => x.ElectionId == election.Id)
                .ToListAsync();

            if (!polls.Any())
                continue;

            // Load real results for the current election.
            var currentResults = await _dbContext.ElectionResults
                .Include(x => x.Party)
                .Where(x => x.ElectionId == election.Id)
                .ToListAsync();

            if (!currentResults.Any())
                continue;

            // Load real results for the previous election.
            var previousResults = await _dbContext.ElectionResults
                .Include(x => x.Party)
                .Where(x => x.ElectionId == previousElection.Id)
                .ToListAsync();

            // Group poll entries by normalized party name.
            // This avoids issues when PartyId differs between polls and results.
            var partyGroups = polls
                .Where(x => x.Party != null && !string.IsNullOrWhiteSpace(x.Party.Name))
                .GroupBy(x => NormalizePartyName(x.Party!.Name));

            foreach (var group in partyGroups)
            {
                var normalizedPartyName = group.Key;

                if (string.IsNullOrWhiteSpace(normalizedPartyName))
                    continue;

                var row = BuildTrainingRowForParty(
                    election,
                    normalizedPartyName,
                    group.ToList(),
                    currentResults,
                    previousResults);

                if (row is not null)
                    rows.Add(row);
            }
        }

        // Debug output - useful while developing
        Console.WriteLine($"Training rows built: {rows.Count}");
        foreach (var row in rows)
        {
            Console.WriteLine(
                $"Party={row.PartyName}, PollAvg={row.PollAverage}, Latest={row.LatestPollPercentage}, Label={row.Label}");
        }

        return rows;
    }

    /// <summary>
    /// Trains the ML.NET regression model.
    /// If the dataset is too small, train on all rows and skip metrics.
    /// </summary>
    public async Task<(ITransformer Model, RegressionMetrics? Metrics, int TrainingRowCount)> TrainModelAsync()
    {
        var trainingRows = await BuildTrainingDataAsync();

        if (!trainingRows.Any())
        {
            throw new InvalidOperationException(
                "No training data found for ML. Make sure you have historical polls and matching election results for the same election.");
        }

        var data = _mlContext.Data.LoadFromEnumerable(trainingRows);

        //var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(new[]
        //    {
        //        new InputOutputColumnPair("PartyNameEncoded", nameof(ElectionTrainingRow.PartyName))
        //    })
        //    .Append(_mlContext.Transforms.Concatenate(
        //        "Features",
        //        nameof(ElectionTrainingRow.DaysBeforeElection),
        //        nameof(ElectionTrainingRow.PollAverage),
        //        nameof(ElectionTrainingRow.LatestPollPercentage),
        //        nameof(ElectionTrainingRow.PollTrend),
        //        nameof(ElectionTrainingRow.PreviousResultPercentage),
        //        nameof(ElectionTrainingRow.PollCount),
        //        nameof(ElectionTrainingRow.AverageSampleSize),
        //        "PartyNameEncoded"))
        //    .Append(_mlContext.Regression.Trainers.Sdca(
        //        labelColumnName: nameof(ElectionTrainingRow.Label),
        //        featureColumnName: "Features"));

        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(new[]
        {
            new InputOutputColumnPair("PartyNameEncoded", nameof(ElectionTrainingRow.PartyName))
        })
        .Append(_mlContext.Transforms.Concatenate(
            "Features",
            nameof(ElectionTrainingRow.PollAverage),
            nameof(ElectionTrainingRow.PreviousResultPercentage),
            nameof(ElectionTrainingRow.DaysBeforeElection),
            "PartyNameEncoded"))
        .Append(_mlContext.Regression.Trainers.Sdca(
        labelColumnName: nameof(ElectionTrainingRow.Label),
        featureColumnName: "Features"));

        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        var modelWithSplit = pipeline.Fit(split.TrainSet);
        var predictions = modelWithSplit.Transform(split.TestSet);

        var metrics = _mlContext.Regression.Evaluate(
            predictions,
            labelColumnName: nameof(ElectionTrainingRow.Label));

        return (modelWithSplit, metrics, trainingRows.Count);
    }

    /// <summary>
    /// Uses the trained model to predict results for a target election year.
    /// Prediction is based on aggregated polls per party for that election.
    /// </summary>
    private async Task<List<MlPartyPrediction>> PredictForElectionAsync(
        int electionYear,
        ITransformer model)
    {
        var targetElection = await _dbContext.Elections
            .Where(x => x.Year == electionYear)
            .OrderByDescending(x => x.ElectionDate)
            .FirstOrDefaultAsync();

        if (targetElection is null)
            return new List<MlPartyPrediction>();

        var previousElection = await _dbContext.Elections
            .Where(x => x.ElectionDate < targetElection.ElectionDate)
            .OrderByDescending(x => x.ElectionDate)
            .FirstOrDefaultAsync();

        if (previousElection is null)
            return new List<MlPartyPrediction>();

        var polls = await _dbContext.PollEntries
            .Include(x => x.Party)
            .Where(x => x.ElectionId == targetElection.Id)
            .ToListAsync();

        Console.WriteLine($"Prediction polls count: {polls.Count}");

        if (!polls.Any())
            return new List<MlPartyPrediction>();

        var previousResults = await _dbContext.ElectionResults
            .Include(x => x.Party)
            .Where(x => x.ElectionId == previousElection.Id)
            .ToListAsync();

        var predictionEngine =
            _mlContext.Model.CreatePredictionEngine<ElectionTrainingRow, ElectionPredictionOutput>(model);

        var result = new List<MlPartyPrediction>();

        // Group prediction polls by normalized party name.
        var partyGroups = polls
            .Where(x => x.Party != null && !string.IsNullOrWhiteSpace(x.Party.Name))
            .GroupBy(x => NormalizePartyName(x.Party!.Name));

        foreach (var group in partyGroups)
        {
            var normalizedPartyName = group.Key;
            var firstPollParty = group.First().Party;

            Console.WriteLine($"Party group: {normalizedPartyName}, polls: {group.Count()}");

            if (firstPollParty is null)
                continue;

            var orderedPolls = group
                .OrderBy(x => x.PollDate)
                .ToList();

            if (!orderedPolls.Any())
                continue;

            var latestPoll = orderedPolls.Last();
            var oldestPoll = orderedPolls.First();

            var daysBeforeElection = (float)(targetElection.ElectionDate - latestPoll.PollDate).TotalDays;

            // Avoid losing prediction rows because of tiny timezone differences.
            daysBeforeElection = Math.Max(daysBeforeElection, 0);

            Console.WriteLine($"DaysBeforeElection for {normalizedPartyName}: {daysBeforeElection}");

            var previousResult = previousResults.FirstOrDefault(x =>
                x.Party != null &&
                NormalizePartyName(x.Party.Name) == normalizedPartyName);

            var input = new ElectionTrainingRow
            {
                DaysBeforeElection = daysBeforeElection,
                PollAverage = orderedPolls.Average(x => (float)x.Percentage),
                LatestPollPercentage = (float)latestPoll.Percentage,
                PollTrend = (float)(latestPoll.Percentage - oldestPoll.Percentage),
                PreviousResultPercentage = previousResult is null ? 0f : (float)previousResult.VotePercentage,
                PollCount = orderedPolls.Count,
                AverageSampleSize = (float)orderedPolls.Average(x => x.SampleSize ?? 0),
                PartyName = normalizedPartyName
            };

            var prediction = predictionEngine.Predict(input);

            //result.Add(new MlPartyPrediction
            //{
            //    Party = firstPollParty.Name,

            //    // Clamp prediction to a valid percentage range.
            //    PredictedPercentage = Math.Clamp(prediction.Score, 0f, 100f)
            //});

            Console.WriteLine($"Raw prediction for {firstPollParty.Name}: {prediction.Score}");

            result.Add(new MlPartyPrediction
            {
                Party = firstPollParty.Name,
                PredictedPercentage = prediction.Score
            });
        }

        // IMPORTANT:
        // Do NOT filter by > 0 here, otherwise all predictions may disappear
        // when the model is weak or the dataset is too small.
        return result
            .OrderByDescending(x => x.PredictedPercentage)
            .ToList();
    }

    /// <summary>
    /// Main method used by the Razor page.
    /// It trains the model, evaluates it (if possible), and predicts for the selected year.
    /// </summary>
    public async Task<MlPredictionPageResult> TrainEvaluateAndPredictAsync(int electionYear)
    {
        var (model, metrics, trainingRowCount) = await TrainModelAsync();
        var predictions = await PredictForElectionAsync(electionYear, model);

        return new MlPredictionPageResult
        {
            RSquared = metrics?.RSquared,
            RootMeanSquaredError = metrics?.RootMeanSquaredError,
            MeanAbsoluteError = metrics?.MeanAbsoluteError,
            TrainingRowCount = trainingRowCount,
            Predictions = predictions
        };
    }
}