using Dapper;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Report showing top tracks for a given artist.
/// </summary>
public class TopTracksForArtistReport : IReport
{
    public string Name => "Top tracks for an artist";

    public string Description => "Shows the top tracks for a given artist name pattern.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Prompt for artist name
        Console.Write("Enter artist name or partial name: ");
        var artistName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(artistName))
        {
            Console.WriteLine("Artist name cannot be empty.");
            return;
        }

        // Prompt for limit
        Console.Write("Enter number of top tracks to show (default 20): ");
        var limitInput = Console.ReadLine();
        int limit = 20;
        if (!string.IsNullOrWhiteSpace(limitInput) && int.TryParse(limitInput, out int parsedLimit))
        {
            limit = parsedLimit;
        }

        // Execute query
        var query = @"
            SELECT TOP (@Limit)
                t.TrackName,
                a.ArtistName,
                d.Year,
                f.TrackPopularity AS Popularity
            FROM FactTrack f
            JOIN DimTrack t ON f.TrackKey = t.TrackKey
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE a.ArtistName LIKE '%' + @ArtistName + '%'
            ORDER BY f.TrackPopularity DESC";

        var results = await connection.QueryAsync<TrackResult>(query, new { ArtistName = artistName, Limit = limit });
        var resultList = results.ToList();

        // Display results
        if (resultList.Count == 0)
        {
            Console.WriteLine($"No tracks found for artist matching '{artistName}'.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Top {limit} Tracks for '{artistName}':");
        Console.WriteLine();
        Console.WriteLine($"{"Track Name",-45} | {"Artist Name",-30} | {"Year",-6} | {"Popularity",-10}");
        Console.WriteLine(new string('-', 100));

        foreach (var result in resultList)
        {
            Console.WriteLine($"{result.TrackName,-45} | {result.ArtistName,-30} | {result.Year,-6} | {result.Popularity,-10}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {resultList.Count} track(s)");
    }

    private class TrackResult
    {
        public string TrackName { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Popularity { get; set; }
    }
}
