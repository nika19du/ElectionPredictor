using ElectionPredictor.Models;
using ElectionPredictor.Services.Interfaces;
using HtmlAgilityPack;

namespace ElectionPredictor.Services;

public class ElectionResultParser : IElectionResultParser
{
    public List<ElectionResultImportRow> ParseElectionResults(string html)
    {
        var results = new List<ElectionResultImportRow>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var table = doc.DocumentNode.SelectSingleNode(
            "//table[contains(@class,'ib-legis-elect-results')]");

        if (table is null)
            return results;

        var rows = table.SelectNodes(".//tr");
        if (rows is null || rows.Count <= 1)
            return results;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes("./th|./td");
            if (cells is null || cells.Count < 5)
                continue;

            // В тази конкретна Wikipedia таблица:
            // 0 = color cell
            // 1 = party
            // 2 = leader
            // 3 = vote %
            // 4 = seats
            // 5 = delta

            var partyText = HtmlEntity.DeEntitize(cells[1].InnerText).Trim();
            var voteText = HtmlEntity.DeEntitize(cells[3].InnerText).Trim();
            var seatsText = HtmlEntity.DeEntitize(cells[4].InnerText).Trim();

            if (string.IsNullOrWhiteSpace(partyText))
                continue;

            if (!decimal.TryParse(
                    voteText.Replace("%", "").Replace(".", ","),
                    out var votePercentage))
            {
                continue;
            }

            if (!int.TryParse(seatsText, out var seats))
                continue;

            results.Add(new ElectionResultImportRow
            {
                PartyName = partyText,
                VotePercentage = votePercentage,
                Seats = seats
            });
        }

        return results;
    }
}