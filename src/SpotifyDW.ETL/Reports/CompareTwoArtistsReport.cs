using Dapper;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Report comparing statistics between two artists.
/// </summary>
public class CompareTwoArtistsReport : IReport
{
    public string Name => "Compare two artists";

    public string Description => "Compares two artists on popularity and audio features.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Prompt for first artist
        Console.Write("Enter first artist name (can be partial): ");
        var artist1 = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(artist1))
        {
            Console.WriteLine("First artist name cannot be empty.");
            return;
        }

        // Prompt for second artist
        Console.Write("Enter second artist name (can be partial): ");
        var artist2 = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(artist2))
        {
            Console.WriteLine("Second artist name cannot be empty.");
            return;
        }

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

        // Execute query - aggregate stats per artist
        var query = @"
            SELECT 
                a.ArtistName,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                AVG(CAST(f.Energy AS FLOAT)) AS AvgEnergy,
                AVG(CAST(f.Danceability AS FLOAT)) AS AvgDanceability,
                AVG(CAST(f.Valence AS FLOAT)) AS AvgValence,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE (a.ArtistName LIKE '%' + @Artist1 + '%' OR a.ArtistName LIKE '%' + @Artist2 + '%')
              AND (@MinYear IS NULL OR d.Year >= @MinYear)
              AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
            GROUP BY a.ArtistName
            ORDER BY a.ArtistName";

        var results = await connection.QueryAsync<ArtistStats>(query, 
            new { Artist1 = artist1, Artist2 = artist2, MinYear = minYear, MaxYear = maxYear });
        var resultList = results.ToList();

        // Display results
        Console.WriteLine();
        var rangeText = (minYear, maxYear) switch
        {
            (null, null) => "All Years",
            (not null, null) => $"{minYear} onwards",
            (null, not null) => $"Up to {maxYear}",
            (not null, not null) => $"{minYear} to {maxYear}"
        };
        Console.WriteLine($"Artist Comparison ({rangeText}):");
        Console.WriteLine(new string('=', 80));

        if (resultList.Count == 0)
        {
            Console.WriteLine($"No data found for artists matching '{artist1}' or '{artist2}' in the specified range.");
            return;
        }

        // Find best matches for each artist input
        var artist1Match = resultList.FirstOrDefault(r => r.ArtistName.Contains(artist1, StringComparison.OrdinalIgnoreCase));
        var artist2Match = resultList.FirstOrDefault(r => r.ArtistName.Contains(artist2, StringComparison.OrdinalIgnoreCase));

        // If no exact substring match, try to match by proximity
        if (artist1Match == null && resultList.Count > 0)
            artist1Match = resultList[0];
        if (artist2Match == null && resultList.Count > 1)
            artist2Match = resultList[1];
        else if (artist2Match == null && resultList.Count == 1 && artist1Match != null)
            artist2Match = null; // Only one artist found

        // Display Artist 1
        Console.WriteLine();
        if (artist1Match != null)
        {
            Console.WriteLine($"ARTIST 1: {artist1Match.ArtistName}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"  Track Count:        {artist1Match.TrackCount}");
            Console.WriteLine($"  Avg Popularity:     {artist1Match.AvgPopularity:F2}");
            Console.WriteLine($"  Avg Energy:         {artist1Match.AvgEnergy:F2}");
            Console.WriteLine($"  Avg Danceability:   {artist1Match.AvgDanceability:F2}");
            Console.WriteLine($"  Avg Valence:        {artist1Match.AvgValence:F2}");
        }
        else
        {
            Console.WriteLine($"ARTIST 1: No data found for '{artist1}'");
        }

        // Display Artist 2
        Console.WriteLine();
        if (artist2Match != null && artist2Match.ArtistName != artist1Match?.ArtistName)
        {
            Console.WriteLine($"ARTIST 2: {artist2Match.ArtistName}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"  Track Count:        {artist2Match.TrackCount}");
            Console.WriteLine($"  Avg Popularity:     {artist2Match.AvgPopularity:F2}");
            Console.WriteLine($"  Avg Energy:         {artist2Match.AvgEnergy:F2}");
            Console.WriteLine($"  Avg Danceability:   {artist2Match.AvgDanceability:F2}");
            Console.WriteLine($"  Avg Valence:        {artist2Match.AvgValence:F2}");
        }
        else if (artist2Match == null)
        {
            Console.WriteLine($"ARTIST 2: No data found for '{artist2}'");
        }

        // Display comparison summary if both artists found
        if (artist1Match != null && artist2Match != null && artist1Match.ArtistName != artist2Match.ArtistName)
        {
            Console.WriteLine();
            Console.WriteLine("COMPARISON:");
            Console.WriteLine(new string('-', 80));
            
            var popDiff = artist1Match.AvgPopularity - artist2Match.AvgPopularity;
            var morePopular = popDiff > 0 ? artist1Match.ArtistName : artist2Match.ArtistName;
            Console.WriteLine($"  More Popular:       {morePopular} (diff: {Math.Abs(popDiff):F2})");
            
            var energyDiff = artist1Match.AvgEnergy - artist2Match.AvgEnergy;
            var moreEnergetic = energyDiff > 0 ? artist1Match.ArtistName : artist2Match.ArtistName;
            Console.WriteLine($"  More Energetic:     {moreEnergetic} (diff: {Math.Abs(energyDiff):F2})");
            
            var danceDiff = artist1Match.AvgDanceability - artist2Match.AvgDanceability;
            var moreDanceable = danceDiff > 0 ? artist1Match.ArtistName : artist2Match.ArtistName;
            Console.WriteLine($"  More Danceable:     {moreDanceable} (diff: {Math.Abs(danceDiff):F2})");
            
            var valenceDiff = artist1Match.AvgValence - artist2Match.AvgValence;
            var morePositive = valenceDiff > 0 ? artist1Match.ArtistName : artist2Match.ArtistName;
            Console.WriteLine($"  More Positive:      {morePositive} (diff: {Math.Abs(valenceDiff):F2})");
        }

        Console.WriteLine();
        Console.WriteLine($"Total artists matched: {resultList.Count}");
    }

    private class ArtistStats
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgPopularity { get; set; }
        public double AvgEnergy { get; set; }
        public double AvgDanceability { get; set; }
        public double AvgValence { get; set; }
        public int TrackCount { get; set; }
    }
}
