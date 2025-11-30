using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services;

public class HomeStatsService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HomeStatsService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<HomeStats> GetStatsAsync()
    {
        var query = @"
            SELECT 
                (SELECT COUNT(*) FROM DimArtist) AS TotalArtists,
                (SELECT COUNT(*) FROM DimAlbum) AS TotalAlbums,
                (SELECT COUNT(*) FROM DimTrack) AS TotalTracks,
                (SELECT AVG(CAST(TrackPopularity AS FLOAT)) FROM FactTrack) AS AvgPopularity,
                (SELECT TOP 1 a.ArtistName 
                 FROM FactTrack f 
                 JOIN DimArtist a ON f.ArtistKey = a.ArtistKey 
                 GROUP BY a.ArtistName 
                 ORDER BY COUNT(*) DESC) AS MostTracksArtist,
                (SELECT TOP 1 d.Year 
                 FROM FactTrack f 
                 JOIN DimDate d ON f.ReleaseDateKey = d.DateKey 
                 GROUP BY d.Year 
                 ORDER BY COUNT(*) DESC) AS MostActiveYear";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QuerySingleAsync<HomeStats>(query);
    }

    public class HomeStats
    {
        public int TotalArtists { get; set; }
        public int TotalAlbums { get; set; }
        public int TotalTracks { get; set; }
        public double AvgPopularity { get; set; }
        public string MostTracksArtist { get; set; } = "";
        public int MostActiveYear { get; set; }
    }
}
