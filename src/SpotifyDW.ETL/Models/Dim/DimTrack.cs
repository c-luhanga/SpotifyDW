namespace SpotifyDW.ETL.Models.Dim;

/// <summary>
/// Dimension table for tracks
/// </summary>
public class DimTrack
{
    public int TrackKey { get; set; }
    public string SpotifyTrackId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public int? TrackNumber { get; set; }
    public int? TrackDurationMs { get; set; }
    public bool? Explicit { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
