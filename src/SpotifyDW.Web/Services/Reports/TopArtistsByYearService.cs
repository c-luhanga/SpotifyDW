using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services.Reports;

public class TopArtistsByYearService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TopArtistsByYearService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<TopArtistResult>> GetTopArtistsAsync(int year, int limit = 10)
    {
        var query = @"
            SELECT TOP (@Limit)
                a.ArtistName,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE d.Year = @Year
            GROUP BY a.ArtistName
            ORDER BY AvgPopularity DESC";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryAsync<TopArtistResult>(query, new { Year = year, Limit = limit });
    }

    public class TopArtistResult
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgPopularity { get; set; }
        public int TrackCount { get; set; }
    }
}
