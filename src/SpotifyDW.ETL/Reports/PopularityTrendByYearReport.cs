using Dapper;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Report showing average track popularity trend over years.
/// </summary>
public class PopularityTrendByYearReport : IReport
{
    public string Name => "Popularity trend by year";

    public string Description => "Shows average track popularity over all years.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Prompt for minimum year (optional)
        Console.Write("Enter minimum year (leave blank for all): ");
        var minYearInput = Console.ReadLine();
        int? minYear = null;
        if (!string.IsNullOrWhiteSpace(minYearInput) && int.TryParse(minYearInput, out int parsedMinYear))
        {
            minYear = parsedMinYear;
        }

        // Prompt for maximum year (optional)
        Console.Write("Enter maximum year (leave blank for all): ");
        var maxYearInput = Console.ReadLine();
        int? maxYear = null;
        if (!string.IsNullOrWhiteSpace(maxYearInput) && int.TryParse(maxYearInput, out int parsedMaxYear))
        {
            maxYear = parsedMaxYear;
        }

        // Build query with optional filters
        var query = @"
            SELECT 
                d.Year,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE (@MinYear IS NULL OR d.Year >= @MinYear)
              AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
            GROUP BY d.Year
            ORDER BY d.Year";

        var results = await connection.QueryAsync<YearResult>(query, new { MinYear = minYear, MaxYear = maxYear });
        var resultList = results.ToList();

        // Display results
        if (resultList.Count == 0)
        {
            Console.WriteLine("No data found for the specified year range.");
            return;
        }

        Console.WriteLine();
        var rangeText = (minYear, maxYear) switch
        {
            (null, null) => "All Years",
            (not null, null) => $"{minYear} onwards",
            (null, not null) => $"Up to {maxYear}",
            (not null, not null) => $"{minYear} to {maxYear}"
        };
        Console.WriteLine($"Popularity Trend: {rangeText}");
        Console.WriteLine();
        Console.WriteLine($"{"Year",-6} {"Avg Popularity",-16} {"Track Count",-12}");
        Console.WriteLine(new string('-', 40));

        foreach (var result in resultList)
        {
            Console.WriteLine($"{result.Year,-6} {result.AvgPopularity,16:F2} {result.TrackCount,12}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {resultList.Count} year(s)");
    }

    private class YearResult
    {
        public int Year { get; set; }
        public double AvgPopularity { get; set; }
        public int TrackCount { get; set; }
    }
}
