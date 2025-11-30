namespace SpotifyDW.ETL.Models.Fact;

/// <summary>
/// Fact table for track metrics and audio features
/// </summary>
public class FactTrack
{
    public long FactTrackKey { get; set; }
    
    // Foreign keys to dimensions
    public int TrackKey { get; set; }
    public int ArtistKey { get; set; }
    public int AlbumKey { get; set; }
    public int? ReleaseDateKey { get; set; }
    
    // Measures
    public int? TrackPopularity { get; set; }
    
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
    
    public DateTime LoadDate { get; set; }
}
