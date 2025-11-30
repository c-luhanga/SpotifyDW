using CsvHelper;
using CsvHelper.Configuration;
using SpotifyDW.ETL.Models.Raw;
using System.Globalization;

namespace SpotifyDW.ETL.Services;

/// <summary>
/// Service responsible for extracting data from CSV files
/// </summary>
public class ExtractService
{
    /// <summary>
    /// Extracts tracks from both CSV files and combines them
    /// </summary>
    public List<RawTrack> ExtractTracks(string spotifyDataPath, string trackDataPath)
    {
        Console.WriteLine("\n=== EXTRACT PHASE ===");
        
        // Read spotify_data_clean.csv (contemporary data)
        var contemporaryTracks = ReadSpotifyDataClean(spotifyDataPath);
        Console.WriteLine($"Loaded {contemporaryTracks.Count} tracks from contemporary data (spotify_data_clean.csv)");
        
        // Read track_data_final.csv (historic data)
        var historicTracks = ReadTrackDataFinal(trackDataPath);
        Console.WriteLine($"Loaded {historicTracks.Count} tracks from historic data (track_data_final.csv)");
        
        // Combine both datasets
        var allTracks = contemporaryTracks.Concat(historicTracks).ToList();
        Console.WriteLine($"Total tracks loaded: {allTracks.Count}");
        
        return allTracks;
    }
    
    /// <summary>
    /// Reads spotify_data_clean.csv
    /// Format: track_duration_min (float), explicit as TRUE/FALSE, genres as comma-separated
    /// </summary>
    private List<RawTrack> ReadSpotifyDataClean(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };
        
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        var records = new List<RawTrack>();
        csv.Read();
        csv.ReadHeader();
        
        while (csv.Read())
        {
            var track = new RawTrack
            {
                TrackId = csv.GetField<string>("track_id") ?? string.Empty,
                TrackName = csv.GetField<string>("track_name") ?? string.Empty,
                TrackNumber = csv.GetField<int?>("track_number"),
                TrackPopularity = csv.GetField<int?>("track_popularity"),
                TrackDurationMin = csv.GetField<double?>("track_duration_min"),
                Explicit = ParseExplicit(csv.GetField<string>("explicit")),
                
                ArtistName = csv.GetField<string>("artist_name") ?? string.Empty,
                ArtistPopularity = csv.GetField<int?>("artist_popularity"),
                ArtistFollowers = csv.GetField<long?>("artist_followers"),
                ArtistGenres = csv.GetField<string>("artist_genres") ?? string.Empty,
                
                AlbumId = csv.GetField<string>("album_id") ?? string.Empty,
                AlbumName = csv.GetField<string>("album_name") ?? string.Empty,
                AlbumReleaseDate = csv.GetField<string>("album_release_date") ?? string.Empty,
                AlbumTotalTracks = csv.GetField<int?>("album_total_tracks"),
                AlbumType = csv.GetField<string>("album_type") ?? string.Empty
            };
            
            // Convert duration from minutes to milliseconds for consistency
            if (track.TrackDurationMin.HasValue)
            {
                track.TrackDurationMs = (int)(track.TrackDurationMin.Value * 60 * 1000);
            }
            
            records.Add(track);
        }
        
        return records;
    }
    
    /// <summary>
    /// Reads track_data_final.csv
    /// Format: track_duration_ms (int), explicit as True/False, genres as JSON array
    /// </summary>
    private List<RawTrack> ReadTrackDataFinal(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };
        
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        var records = new List<RawTrack>();
        csv.Read();
        csv.ReadHeader();
        
        while (csv.Read())
        {
            var track = new RawTrack
            {
                TrackId = csv.GetField<string>("track_id") ?? string.Empty,
                TrackName = csv.GetField<string>("track_name") ?? string.Empty,
                TrackNumber = csv.GetField<int?>("track_number"),
                TrackPopularity = csv.GetField<int?>("track_popularity"),
                TrackDurationMs = csv.GetField<int?>("track_duration_ms"),
                Explicit = ParseExplicit(csv.GetField<string>("explicit")),
                
                ArtistName = csv.GetField<string>("artist_name") ?? string.Empty,
                ArtistPopularity = ParseNullableDouble(csv.GetField<string>("artist_popularity")),
                ArtistFollowers = ParseNullableLong(csv.GetField<string>("artist_followers")),
                ArtistGenres = csv.GetField<string>("artist_genres") ?? string.Empty,
                
                AlbumId = csv.GetField<string>("album_id") ?? string.Empty,
                AlbumName = csv.GetField<string>("album_name") ?? string.Empty,
                AlbumReleaseDate = csv.GetField<string>("album_release_date") ?? string.Empty,
                AlbumTotalTracks = csv.GetField<int?>("album_total_tracks"),
                AlbumType = csv.GetField<string>("album_type") ?? string.Empty
            };
            
            records.Add(track);
        }
        
        return records;
    }
    
    /// <summary>
    /// Parses explicit field that can be TRUE/FALSE or True/False
    /// </summary>
    private bool? ParseExplicit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
            
        return value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("True", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Parses nullable int from string (handles float values from CSV)
    /// </summary>
    private int? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
            
        if (double.TryParse(value, out var result))
            return (int)result;
            
        return null;
    }
    
    /// <summary>
    /// Parses nullable long from string (handles float values from CSV)
    /// </summary>
    private long? ParseNullableLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
            
        if (double.TryParse(value, out var result))
            return (long)result;
            
        return null;
    }
}
