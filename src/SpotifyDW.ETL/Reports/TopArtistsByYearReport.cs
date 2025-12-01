using Dapper;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Report showing top artists by average popularity for a given year.
/// </summary>
public class TopArtistsByYearReport : IReport
{
    public string Name => "Top artists by year";

    public string Description => "Shows the top N artists for a given year by average popularity.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Prompt for year
        Console.Write("Enter year (e.g., 2020): ");
        var yearInput = Console.ReadLine();
        if (!int.TryParse(yearInput, out int year))
        {
            Console.WriteLine("Invalid year. Please enter a valid 4-digit year.");
            return;
        }

        // Prompt for limit
        Console.Write("Enter number of top artists to show (default 10): ");
        var limitInput = Console.ReadLine();
        int limit = 10;
        if (!string.IsNullOrWhiteSpace(limitInput) && int.TryParse(limitInput, out int parsedLimit))
        {
            limit = parsedLimit;
        }

        // Execute query
        var query = @"
            SELECT TOP (@Limit)
                a.ArtistName,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey AND a.IsCurrent = 1
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE d.Year = @Year
            GROUP BY a.ArtistName
            ORDER BY AvgPopularity DESC";

        var results = await connection.QueryAsync<ArtistResult>(query, new { Year = year, Limit = limit });
        var resultList = results.ToList();

        // Display results
        if (resultList.Count == 0)
        {
            Console.WriteLine($"No artists found for year {year}.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Top {limit} Artists for {year}:");
        Console.WriteLine();
        Console.WriteLine($"{"Rank",-6} {"Artist Name",-40} {"Avg Popularity",-16} {"Track Count",-12}");
        Console.WriteLine(new string('-', 80));

        for (int i = 0; i < resultList.Count; i++)
        {
            var result = resultList[i];
            Console.WriteLine($"{i + 1,-6} {result.ArtistName,-40} {result.AvgPopularity,16:F2} {result.TrackCount,12}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {resultList.Count} artist(s)");
    }

    private class ArtistResult
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgPopularity { get; set; }
        public int TrackCount { get; set; }
    }
}
