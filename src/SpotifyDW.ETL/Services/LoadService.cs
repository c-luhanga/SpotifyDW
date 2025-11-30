using Dapper;
using Microsoft.Data.SqlClient;
using SpotifyDW.ETL.Models.Dim;
using SpotifyDW.ETL.Models.Fact;
using System.Data;

namespace SpotifyDW.ETL.Services;

/// <summary>
/// Service responsible for loading data into the data warehouse
/// </summary>
public class LoadService
{
    private readonly string _connectionString;
    
    public LoadService(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <summary>
    /// Loads all dimension and fact data into the database
    /// </summary>
    public void LoadData(TransformService.TransformResult data)
    {
        Console.WriteLine("\n=== LOAD PHASE ===");
        
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Step 1: Load DimArtist and get real keys
            var artistKeyMap = LoadDimArtist(connection, transaction, data.Artists);
            Console.WriteLine($"Loaded {artistKeyMap.Count} artists into DimArtist");
            
            // Step 2: Load DimDate
            var dateKeyMap = LoadDimDate(connection, transaction, data.Dates);
            Console.WriteLine($"Loaded {dateKeyMap.Count} dates into DimDate");
            
            // Step 3: Load DimAlbum with real artist keys
            var albumKeyMap = LoadDimAlbum(connection, transaction, data.Albums, artistKeyMap, dateKeyMap);
            Console.WriteLine($"Loaded {albumKeyMap.Count} albums into DimAlbum");
            
            // Step 4: Load DimTrack and get real keys
            var trackKeyMap = LoadDimTrack(connection, transaction, data.Tracks);
            Console.WriteLine($"Loaded {trackKeyMap.Count} tracks into DimTrack");
            
            // Step 5: Load FactTrack with real dimension keys
            var factCount = LoadFactTrack(connection, transaction, data.Facts, trackKeyMap, artistKeyMap, albumKeyMap);
            Console.WriteLine($"Loaded {factCount} fact records into FactTrack");
            
            transaction.Commit();
            Console.WriteLine("\n✓ Load phase completed successfully!");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"\n❌ Load phase failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Loads DimArtist and returns mapping of temp key to real key
    /// </summary>
    private Dictionary<int, int> LoadDimArtist(SqlConnection connection, SqlTransaction transaction, List<DimArtist> artists)
    {
        var keyMap = new Dictionary<int, int>();
        
        const string sql = @"
            INSERT INTO dbo.DimArtist (ArtistName, ArtistPopularity, ArtistFollowers, ArtistGenres, CreatedDate, ModifiedDate)
            OUTPUT INSERTED.ArtistKey, INSERTED.ArtistName
            VALUES (@ArtistName, @ArtistPopularity, @ArtistFollowers, @ArtistGenres, @CreatedDate, @ModifiedDate)";
        
        foreach (var artist in artists)
        {
            var result = connection.QuerySingle<(int ArtistKey, string ArtistName)>(
                sql,
                new
                {
                    artist.ArtistName,
                    artist.ArtistPopularity,
                    artist.ArtistFollowers,
                    artist.ArtistGenres,
                    artist.CreatedDate,
                    artist.ModifiedDate
                },
                transaction);
            
            keyMap[artist.ArtistKey] = result.ArtistKey;
        }
        
        return keyMap;
    }
    
    /// <summary>
    /// Loads DimDate and returns mapping
    /// </summary>
    private Dictionary<int, int> LoadDimDate(SqlConnection connection, SqlTransaction transaction, List<DimDate> dates)
    {
        var keyMap = new Dictionary<int, int>();
        
        const string sql = @"
            INSERT INTO dbo.DimDate (DateKey, Date, Year, Month, MonthName, Quarter, DayOfWeek, DayName, IsWeekend, CreatedDate)
            VALUES (@DateKey, @Date, @Year, @Month, @MonthName, @Quarter, @DayOfWeek, @DayName, @IsWeekend, @CreatedDate)";
        
        foreach (var date in dates)
        {
            connection.Execute(sql, date, transaction);
            keyMap[date.DateKey] = date.DateKey; // DateKey is the business key, no surrogate
        }
        
        return keyMap;
    }
    
    /// <summary>
    /// Loads DimAlbum with real artist keys and returns mapping
    /// </summary>
    private Dictionary<int, int> LoadDimAlbum(
        SqlConnection connection, 
        SqlTransaction transaction, 
        List<DimAlbum> albums, 
        Dictionary<int, int> artistKeyMap,
        Dictionary<int, int> dateKeyMap)
    {
        var keyMap = new Dictionary<int, int>();
        
        const string sql = @"
            INSERT INTO dbo.DimAlbum (SpotifyAlbumId, AlbumName, ArtistKey, AlbumType, AlbumTotalTracks, ReleaseDateKey, CreatedDate, ModifiedDate)
            OUTPUT INSERTED.AlbumKey
            VALUES (@SpotifyAlbumId, @AlbumName, @ArtistKey, @AlbumType, @AlbumTotalTracks, @ReleaseDateKey, @CreatedDate, @ModifiedDate)";
        
        foreach (var album in albums)
        {
            // Map temp artist key to real artist key
            var realArtistKey = artistKeyMap.GetValueOrDefault(album.ArtistKey, album.ArtistKey);
            
            // Map temp date key to real date key (should be same)
            var realDateKey = album.ReleaseDateKey.HasValue 
                ? dateKeyMap.GetValueOrDefault(album.ReleaseDateKey.Value, album.ReleaseDateKey.Value)
                : (int?)null;
            
            var realAlbumKey = connection.QuerySingle<int>(
                sql,
                new
                {
                    album.SpotifyAlbumId,
                    album.AlbumName,
                    ArtistKey = realArtistKey,
                    album.AlbumType,
                    album.AlbumTotalTracks,
                    ReleaseDateKey = realDateKey,
                    album.CreatedDate,
                    album.ModifiedDate
                },
                transaction);
            
            keyMap[album.AlbumKey] = realAlbumKey;
        }
        
        return keyMap;
    }
    
    /// <summary>
    /// Loads DimTrack and returns mapping
    /// </summary>
    private Dictionary<int, int> LoadDimTrack(SqlConnection connection, SqlTransaction transaction, List<DimTrack> tracks)
    {
        var keyMap = new Dictionary<int, int>();
        
        const string sql = @"
            INSERT INTO dbo.DimTrack (SpotifyTrackId, TrackName, TrackNumber, TrackDurationMs, Explicit, CreatedDate, ModifiedDate)
            OUTPUT INSERTED.TrackKey
            VALUES (@SpotifyTrackId, @TrackName, @TrackNumber, @TrackDurationMs, @Explicit, @CreatedDate, @ModifiedDate)";
        
        foreach (var track in tracks)
        {
            var realTrackKey = connection.QuerySingle<int>(
                sql,
                new
                {
                    track.SpotifyTrackId,
                    track.TrackName,
                    track.TrackNumber,
                    track.TrackDurationMs,
                    track.Explicit,
                    track.CreatedDate,
                    track.ModifiedDate
                },
                transaction);
            
            keyMap[track.TrackKey] = realTrackKey;
        }
        
        return keyMap;
    }
    
    /// <summary>
    /// Loads FactTrack with real dimension keys
    /// </summary>
    private int LoadFactTrack(
        SqlConnection connection,
        SqlTransaction transaction,
        List<FactTrack> facts,
        Dictionary<int, int> trackKeyMap,
        Dictionary<int, int> artistKeyMap,
        Dictionary<int, int> albumKeyMap)
    {
        const string sql = @"
            INSERT INTO dbo.FactTrack (
                TrackKey, ArtistKey, AlbumKey, ReleaseDateKey,
                TrackPopularity, Energy, Danceability, Valence, Loudness, Tempo,
                Acousticness, Instrumentalness, Liveness, Speechiness, LoadDate
            )
            VALUES (
                @TrackKey, @ArtistKey, @AlbumKey, @ReleaseDateKey,
                @TrackPopularity, @Energy, @Danceability, @Valence, @Loudness, @Tempo,
                @Acousticness, @Instrumentalness, @Liveness, @Speechiness, @LoadDate
            )";
        
        var factsToLoad = facts.Select(f => new
        {
            TrackKey = trackKeyMap.GetValueOrDefault(f.TrackKey, f.TrackKey),
            ArtistKey = artistKeyMap.GetValueOrDefault(f.ArtistKey, f.ArtistKey),
            AlbumKey = albumKeyMap.GetValueOrDefault(f.AlbumKey, f.AlbumKey),
            f.ReleaseDateKey,
            f.TrackPopularity,
            f.Energy,
            f.Danceability,
            f.Valence,
            f.Loudness,
            f.Tempo,
            f.Acousticness,
            f.Instrumentalness,
            f.Liveness,
            f.Speechiness,
            f.LoadDate
        }).ToList();
        
        var rowsAffected = connection.Execute(sql, factsToLoad, transaction);
        
        return rowsAffected;
    }
}
