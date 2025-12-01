using Dapper;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Report showing average audio feature profile for an artist.
/// </summary>
public class AudioProfileReport : IReport
{
    public string Name => "Audio profile of an artist";

    public string Description => "Shows average audio feature profile (energy, danceability, valence, tempo) for an artist.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Prompt for artist name
        Console.Write("Enter artist name (can be partial): ");
        var artistName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(artistName))
        {
            Console.WriteLine("Artist name cannot be empty.");
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

        // Execute query
        var query = @"
                        SELECT 
                                a.ArtistName,
                                AVG(CAST(f.Energy AS FLOAT)) AS AvgEnergy,
                                AVG(CAST(f.Danceability AS FLOAT)) AS AvgDanceability,
                                AVG(CAST(f.Valence AS FLOAT)) AS AvgValence,
                                AVG(CAST(f.Tempo AS FLOAT)) AS AvgTempo,
                                AVG(CAST(f.Acousticness AS FLOAT)) AS AvgAcousticness,
                                AVG(CAST(f.Instrumentalness AS FLOAT)) AS AvgInstrumentalness,
                                AVG(CAST(f.Liveness AS FLOAT)) AS AvgLiveness,
                                AVG(CAST(f.Speechiness AS FLOAT)) AS AvgSpeechiness,
                                AVG(CAST(f.Loudness AS FLOAT)) AS AvgLoudness,
                                COUNT(*) AS TrackCount
                        FROM FactTrack f
                        JOIN DimArtist a ON f.ArtistKey = a.ArtistKey AND a.IsCurrent = 1
                        JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
                        WHERE a.ArtistName LIKE '%' + @ArtistName + '%'
                            AND (@MinYear IS NULL OR d.Year >= @MinYear)
                            AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
                        GROUP BY a.ArtistName
                        ORDER BY a.ArtistName";

        var results = await connection.QueryAsync<ArtistProfile>(query, 
            new { ArtistName = artistName, MinYear = minYear, MaxYear = maxYear });
        var resultList = results.ToList();

        // Display results
        if (resultList.Count == 0)
        {
            Console.WriteLine($"No data found for artist matching '{artistName}' in the specified range.");
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
        Console.WriteLine($"Audio Profile for '{artistName}' ({rangeText}):");
        Console.WriteLine(new string('=', 80));

        foreach (var profile in resultList)
        {
            Console.WriteLine();
            Console.WriteLine($"Artist: {profile.ArtistName}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"  Track Count:          {profile.TrackCount}");
            Console.WriteLine();
            Console.WriteLine("  Audio Features:");
            Console.WriteLine($"    Energy:             {profile.AvgEnergy:F3} (0 = calm, 1 = energetic)");
            Console.WriteLine($"    Danceability:       {profile.AvgDanceability:F3} (0 = not danceable, 1 = very danceable)");
            Console.WriteLine($"    Valence:            {profile.AvgValence:F3} (0 = sad/negative, 1 = happy/positive)");
            Console.WriteLine($"    Tempo:              {profile.AvgTempo:F1} BPM");
            Console.WriteLine($"    Acousticness:       {profile.AvgAcousticness:F3} (0 = not acoustic, 1 = acoustic)");
            Console.WriteLine($"    Instrumentalness:   {profile.AvgInstrumentalness:F3} (0 = vocals, 1 = instrumental)");
            Console.WriteLine($"    Liveness:           {profile.AvgLiveness:F3} (0 = studio, 1 = live performance)");
            Console.WriteLine($"    Speechiness:        {profile.AvgSpeechiness:F3} (0 = music, 1 = spoken word)");
            Console.WriteLine($"    Loudness:           {profile.AvgLoudness:F2} dB");
            Console.WriteLine();
            Console.WriteLine("  Profile Summary:");
            Console.WriteLine($"    Overall Vibe:       {GetVibeDescription(profile)}");
            Console.WriteLine($"    Energy Level:       {GetEnergyLevel(profile.AvgEnergy)}");
            Console.WriteLine($"    Mood:               {GetMoodDescription(profile.AvgValence)}");
        }

        Console.WriteLine();
        if (resultList.Count > 1)
        {
            Console.WriteLine($"Note: {resultList.Count} artists matched your search.");
        }
    }

    private static string GetVibeDescription(ArtistProfile profile)
    {
        if (profile.AvgEnergy > 0.7 && profile.AvgDanceability > 0.7)
            return "High-energy dance music";
        if (profile.AvgEnergy < 0.3 && profile.AvgAcousticness > 0.5)
            return "Mellow acoustic";
        if (profile.AvgDanceability > 0.7)
            return "Dance-oriented";
        if (profile.AvgEnergy > 0.7)
            return "High-energy";
        if (profile.AvgAcousticness > 0.5)
            return "Acoustic-leaning";
        return "Balanced mix";
    }

    private static string GetEnergyLevel(double energy)
    {
        return energy switch
        {
            >= 0.8 => "Very High",
            >= 0.6 => "High",
            >= 0.4 => "Moderate",
            >= 0.2 => "Low",
            _ => "Very Low"
        };
    }

    private static string GetMoodDescription(double valence)
    {
        return valence switch
        {
            >= 0.8 => "Very Positive/Happy",
            >= 0.6 => "Positive/Upbeat",
            >= 0.4 => "Neutral",
            >= 0.2 => "Melancholic/Somber",
            _ => "Dark/Sad"
        };
    }

    private class ArtistProfile
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgEnergy { get; set; }
        public double AvgDanceability { get; set; }
        public double AvgValence { get; set; }
        public double AvgTempo { get; set; }
        public double AvgAcousticness { get; set; }
        public double AvgInstrumentalness { get; set; }
        public double AvgLiveness { get; set; }
        public double AvgSpeechiness { get; set; }
        public double AvgLoudness { get; set; }
        public int TrackCount { get; set; }
    }
}
