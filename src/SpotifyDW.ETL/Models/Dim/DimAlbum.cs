namespace SpotifyDW.ETL.Models.Dim;

/// <summary>
/// Dimension table for albums
/// </summary>
public class DimAlbum
{
    public int AlbumKey { get; set; }
    public string SpotifyAlbumId { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public int ArtistKey { get; set; }
    public string? AlbumType { get; set; }
    public int? AlbumTotalTracks { get; set; }
    public int? ReleaseDateKey { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
