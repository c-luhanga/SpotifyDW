using SpotifyDW.ETL.Models.Raw;
using SpotifyDW.ETL.Models.Dim;
using SpotifyDW.ETL.Models.Fact;
using System.Globalization;

namespace SpotifyDW.ETL.Services;

/// <summary>
/// Service responsible for transforming raw data into dimension and fact models
/// </summary>
public class TransformService
{
    public class TransformResult
    {
        public List<DimArtist> Artists { get; set; } = new();
        public List<DimAlbum> Albums { get; set; } = new();
        public List<DimTrack> Tracks { get; set; } = new();
        public List<DimDate> Dates { get; set; } = new();
        public List<FactTrack> Facts { get; set; } = new();
    }
    
    private Dictionary<string, int> _artistKeyMap = new();
    private Dictionary<string, int> _albumKeyMap = new();
    private Dictionary<string, int> _trackKeyMap = new();
    private Dictionary<int, int> _dateKeyMap = new();
    
    /// <summary>
    /// Transforms raw tracks into dimension and fact models
    /// </summary>
    public TransformResult TransformData(List<RawTrack> rawTracks)
    {
        Console.WriteLine("\n=== TRANSFORM PHASE ===");
        
        var result = new TransformResult();
        
        // Step 1: Build DimArtist
        result.Artists = BuildDimArtist(rawTracks);
        Console.WriteLine($"Created {result.Artists.Count} unique artists");
        
        // Step 2: Build DimDate from release dates
        result.Dates = BuildDimDate(rawTracks);
        Console.WriteLine($"Created {result.Dates.Count} unique dates");
        
        // Step 3: Build DimAlbum (requires artist keys)
        result.Albums = BuildDimAlbum(rawTracks);
        Console.WriteLine($"Created {result.Albums.Count} unique albums");
        
        // Step 4: Build DimTrack
        result.Tracks = BuildDimTrack(rawTracks);
        Console.WriteLine($"Created {result.Tracks.Count} unique tracks");
        
        // Step 5: Build FactTrack (requires all dimension keys)
        result.Facts = BuildFactTrack(rawTracks);
        Console.WriteLine($"Created {result.Facts.Count} fact records");
        
        return result;
    }
    
    /// <summary>
    /// Builds DimArtist list by grouping raw tracks by normalized artist name
    /// </summary>
    private List<DimArtist> BuildDimArtist(List<RawTrack> rawTracks)
    {
        var artists = rawTracks
            .GroupBy(t => NormalizeString(t.ArtistName))
            .Select((g, index) => 
            {
                var firstTrack = g.First();
                var artistKey = index + 1; // Temporary surrogate key
                
                _artistKeyMap[g.Key] = artistKey;
                
                return new DimArtist
                {
                    ArtistKey = artistKey,
                    ArtistName = g.Key,
                    ArtistPopularity = g.Max(t => t.ArtistPopularity),
                    ArtistFollowers = g.Max(t => t.ArtistFollowers),
                    ArtistGenres = NormalizeGenres(firstTrack.ArtistGenres),
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
            })
            .OrderBy(a => a.ArtistName)
            .ToList();
            
        return artists;
    }
    
    /// <summary>
    /// Builds DimAlbum list by grouping by SpotifyAlbumId (unique identifier)
    /// </summary>
    private List<DimAlbum> BuildDimAlbum(List<RawTrack> rawTracks)
    {
        var albums = rawTracks
            .Where(t => !string.IsNullOrWhiteSpace(t.AlbumId))
            .GroupBy(t => t.AlbumId!.Trim())
            .Select((g, index) =>
            {
                var firstTrack = g.First();
                var albumKey = index + 1; // Temporary surrogate key
                var artistName = NormalizeString(firstTrack.ArtistName);
                var artistKey = _artistKeyMap.GetValueOrDefault(artistName, 0);
                
                var albumMapKey = $"{g.Key}|{NormalizeString(firstTrack.AlbumName)}|{artistName}";
                _albumKeyMap[albumMapKey] = albumKey;
                
                // Parse release date
                var releaseDateKey = ParseReleaseDateKey(firstTrack.AlbumReleaseDate);
                
                return new DimAlbum
                {
                    AlbumKey = albumKey,
                    SpotifyAlbumId = g.Key,
                    AlbumName = NormalizeString(firstTrack.AlbumName),
                    ArtistKey = artistKey,
                    AlbumType = firstTrack.AlbumType?.Trim(),
                    AlbumTotalTracks = firstTrack.AlbumTotalTracks,
                    ReleaseDateKey = releaseDateKey,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
            })
            .OrderBy(a => a.AlbumName)
            .ToList();
            
        return albums;
    }
    
    /// <summary>
    /// Builds DimTrack list by grouping by track ID
    /// </summary>
    private List<DimTrack> BuildDimTrack(List<RawTrack> rawTracks)
    {
        var tracks = rawTracks
            .GroupBy(t => new
            {
                TrackId = t.TrackId?.Trim() ?? string.Empty,
                TrackName = NormalizeString(t.TrackName)
            })
            .Select((g, index) =>
            {
                var firstTrack = g.First();
                var trackKey = index + 1; // Temporary surrogate key
                
                _trackKeyMap[g.Key.TrackId] = trackKey;
                
                return new DimTrack
                {
                    TrackKey = trackKey,
                    SpotifyTrackId = g.Key.TrackId,
                    TrackName = g.Key.TrackName,
                    TrackNumber = firstTrack.TrackNumber,
                    TrackDurationMs = firstTrack.TrackDurationMs,
                    Explicit = firstTrack.Explicit,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
            })
            .OrderBy(t => t.TrackName)
            .ToList();
            
        return tracks;
    }
    
    /// <summary>
    /// Builds DimDate from unique release dates
    /// </summary>
    private List<DimDate> BuildDimDate(List<RawTrack> rawTracks)
    {
        var dates = rawTracks
            .Select(t => t.AlbumReleaseDate)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .Select(dateStr => ParseDate(dateStr))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .Select(date =>
            {
                var dateKey = int.Parse(date.ToString("yyyyMMdd"));
                _dateKeyMap[dateKey] = dateKey;
                
                return new DimDate
                {
                    DateKey = dateKey,
                    Date = date,
                    Year = date.Year,
                    Month = date.Month,
                    MonthName = date.ToString("MMMM"),
                    Quarter = (date.Month - 1) / 3 + 1,
                    DayOfWeek = (int)date.DayOfWeek + 1,
                    DayName = date.ToString("dddd"),
                    IsWeekend = date.DayOfWeek == System.DayOfWeek.Saturday || 
                               date.DayOfWeek == System.DayOfWeek.Sunday,
                    CreatedDate = DateTime.Now
                };
            })
            .OrderBy(d => d.DateKey)
            .ToList();
            
        return dates;
    }
    
    /// <summary>
    /// Builds FactTrack by looking up dimension keys
    /// </summary>
    private List<FactTrack> BuildFactTrack(List<RawTrack> rawTracks)
    {
        var facts = rawTracks
            .Select((raw, index) =>
            {
                var artistKey = _artistKeyMap.GetValueOrDefault(NormalizeString(raw.ArtistName), 0);
                var trackKey = _trackKeyMap.GetValueOrDefault(raw.TrackId?.Trim() ?? string.Empty, 0);
                
                var albumMapKey = $"{raw.AlbumId?.Trim() ?? string.Empty}|{NormalizeString(raw.AlbumName)}|{NormalizeString(raw.ArtistName)}";
                var albumKey = _albumKeyMap.GetValueOrDefault(albumMapKey, 0);
                
                var releaseDateKey = ParseReleaseDateKey(raw.AlbumReleaseDate);
                
                return new FactTrack
                {
                    FactTrackKey = index + 1,
                    TrackKey = trackKey,
                    ArtistKey = artistKey,
                    AlbumKey = albumKey,
                    ReleaseDateKey = releaseDateKey,
                    TrackPopularity = raw.TrackPopularity,
                    Energy = raw.Energy,
                    Danceability = raw.Danceability,
                    Valence = raw.Valence,
                    Loudness = raw.Loudness,
                    Tempo = raw.Tempo,
                    Acousticness = raw.Acousticness,
                    Instrumentalness = raw.Instrumentalness,
                    Liveness = raw.Liveness,
                    Speechiness = raw.Speechiness,
                    LoadDate = DateTime.Now
                };
            })
            .Where(f => f.TrackKey > 0 && f.ArtistKey > 0 && f.AlbumKey > 0)
            .ToList();
            
        return facts;
    }
    
    #region Helper Methods
    
    /// <summary>
    /// Normalizes string by trimming and title-casing
    /// </summary>
    private string NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
            
        var trimmed = value.Trim();
        
        // Use title case for consistency
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(trimmed.ToLower());
    }
    
    /// <summary>
    /// Normalizes genre strings (handles both comma-separated and JSON array formats)
    /// </summary>
    private string? NormalizeGenres(string? genres)
    {
        if (string.IsNullOrWhiteSpace(genres) || genres == "N/A")
            return null;
            
        // Remove brackets and quotes from JSON-like format
        genres = genres.Replace("[", "").Replace("]", "").Replace("'", "").Replace("\"", "");
        
        return genres.Trim();
    }
    
    /// <summary>
    /// Parses date string to DateTime
    /// </summary>
    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;
            
        if (DateTime.TryParse(dateStr, out var date))
            return date;
            
        return null;
    }
    
    /// <summary>
    /// Parses release date and returns DateKey (YYYYMMDD format)
    /// </summary>
    private int? ParseReleaseDateKey(string? dateStr)
    {
        var date = ParseDate(dateStr);
        if (!date.HasValue)
            return null;
            
        return int.Parse(date.Value.ToString("yyyyMMdd"));
    }
    
    #endregion
}
