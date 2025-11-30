using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services.Reports;

public class CompareTwoArtistsService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CompareTwoArtistsService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<ComparisonResult> CompareAsync(string artist1, string artist2, int? minYear = null, int? maxYear = null)
    {
        var query = @"
            SELECT 
                a.ArtistName,
                AVG(CAST(f.TrackPopularity AS FLOAT)) AS AvgPopularity,
                AVG(CAST(f.Energy AS FLOAT)) AS AvgEnergy,
                AVG(CAST(f.Danceability AS FLOAT)) AS AvgDanceability,
                AVG(CAST(f.Valence AS FLOAT)) AS AvgValence,
                COUNT(*) AS TrackCount
            FROM FactTrack f
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE (a.ArtistName LIKE '%' + @Artist1 + '%' OR a.ArtistName LIKE '%' + @Artist2 + '%')
              AND (@MinYear IS NULL OR d.Year >= @MinYear)
              AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
            GROUP BY a.ArtistName
            ORDER BY a.ArtistName";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        var results = await connection.QueryAsync<ArtistStats>(query, 
            new { Artist1 = artist1, Artist2 = artist2, MinYear = minYear, MaxYear = maxYear });

        var resultList = results.ToList();

        // Find best matches
        var artist1Match = resultList.FirstOrDefault(r => r.ArtistName.Contains(artist1, StringComparison.OrdinalIgnoreCase));
        var artist2Match = resultList.FirstOrDefault(r => r.ArtistName.Contains(artist2, StringComparison.OrdinalIgnoreCase));

        if (artist1Match == null && resultList.Count > 0)
            artist1Match = resultList[0];
        if (artist2Match == null && resultList.Count > 1)
            artist2Match = resultList[1];

        return new ComparisonResult
        {
            Artist1 = artist1Match,
            Artist2 = artist2Match,
            AllMatches = resultList
        };
    }

    public class ComparisonResult
    {
        public ArtistStats? Artist1 { get; set; }
        public ArtistStats? Artist2 { get; set; }
        public List<ArtistStats> AllMatches { get; set; } = new();
    }

    public class ArtistStats
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgPopularity { get; set; }
        public double AvgEnergy { get; set; }
        public double AvgDanceability { get; set; }
        public double AvgValence { get; set; }
        public int TrackCount { get; set; }
    }
}
