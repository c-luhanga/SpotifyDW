namespace SpotifyDW.ETL.Models.Raw;

/// <summary>
/// Represents a raw track record from the CSV files.
/// Maps to columns in track_data_final.csv and spotify_data_clean.csv
/// </summary>
public class RawTrack
{
    public string TrackId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public int? TrackNumber { get; set; }
    public int? TrackPopularity { get; set; }
    public int? TrackDurationMs { get; set; }
    public double? TrackDurationMin { get; set; }
    public bool? Explicit { get; set; }
    
    public string ArtistName { get; set; } = string.Empty;
    public int? ArtistPopularity { get; set; }
    public long? ArtistFollowers { get; set; }
    public string ArtistGenres { get; set; } = string.Empty;
    
    public string AlbumId { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string AlbumReleaseDate { get; set; } = string.Empty;
    public int? AlbumTotalTracks { get; set; }
    public string AlbumType { get; set; } = string.Empty;
    
    // Audio features (optional - for future use)
    public double? Energy { get; set; }
    public double? Danceability { get; set; }
    public double? Valence { get; set; }
    public double? Loudness { get; set; }
    public double? Tempo { get; set; }
    public double? Acousticness { get; set; }
    public double? Instrumentalness { get; set; }
    public double? Liveness { get; set; }
    public double? Speechiness { get; set; }
}
