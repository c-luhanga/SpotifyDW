using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services.Reports;

public class TopTracksForArtistService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TopTracksForArtistService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<TopTrackResult>> GetTopTracksAsync(string artistPattern, int limit = 20)
    {

        var query = @"
            SELECT TOP (@Limit)
                t.TrackName,
                a.ArtistName,
                d.Year,
                f.TrackPopularity AS Popularity,
                CASE
                    WHEN LOWER(a.ArtistName) = LOWER(@ArtistPattern) THEN 1
                    WHEN LOWER(a.ArtistName) LIKE LOWER(@ArtistPattern) + '%' THEN 2
                    ELSE 3
                END AS MatchRank
            FROM FactTrack f
            JOIN DimTrack t ON f.TrackKey = t.TrackKey
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE LOWER(a.ArtistName) LIKE '%' + LOWER(@ArtistPattern) + '%'
            ORDER BY MatchRank, f.TrackPopularity DESC, a.ArtistName, t.TrackName";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryAsync<TopTrackResult>(query, new { ArtistPattern = artistPattern, Limit = limit });
    }

    public class TopTrackResult
    {
        public string TrackName { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Popularity { get; set; }
    }
}
