using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services.Reports;

public class PopularityTrendByYearService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PopularityTrendByYearService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<YearTrendResult>> GetTrendAsync(int? minYear = null, int? maxYear = null)
    {
        var query = @"
            SELECT 
                d.Year,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE (@MinYear IS NULL OR d.Year >= @MinYear)
              AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
            GROUP BY d.Year
            ORDER BY d.Year";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryAsync<YearTrendResult>(query, new { MinYear = minYear, MaxYear = maxYear });
    }

    public class YearTrendResult
    {
        public int Year { get; set; }
        public double AvgPopularity { get; set; }
        public int TrackCount { get; set; }
    }
}
