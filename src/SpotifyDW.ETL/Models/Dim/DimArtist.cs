namespace SpotifyDW.ETL.Models.Dim;

/// <summary>
/// Dimension table for artists
/// </summary>
public class DimArtist
{
    public int ArtistKey { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public int? ArtistPopularity { get; set; }
    public long? ArtistFollowers { get; set; }
    public string? ArtistGenres { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    // SCD2 columns
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public bool IsCurrent { get; set; }
}
