using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Services.Reports;

public class AudioProfileService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AudioProfileService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<ArtistProfileResult>> GetProfileAsync(string artistPattern, int? minYear = null, int? maxYear = null)
    {

        var query = @"
            SELECT 
                a.ArtistName,
                AVG(CAST(f.Energy AS FLOAT)) AS AvgEnergy,
                AVG(CAST(f.Danceability AS FLOAT)) AS AvgDanceability,
                AVG(CAST(f.Valence AS FLOAT)) AS AvgValence,
                AVG(CAST(f.Tempo AS FLOAT)) AS AvgTempo,
                AVG(CAST(f.Acousticness AS FLOAT)) AS AvgAcousticness,
                AVG(CAST(f.Instrumentalness AS FLOAT)) AS AvgInstrumentalness,
                AVG(CAST(f.Liveness AS FLOAT)) AS AvgLiveness,
                AVG(CAST(f.Speechiness AS FLOAT)) AS AvgSpeechiness,
                AVG(CAST(f.Loudness AS FLOAT)) AS AvgLoudness,
                COUNT(*) AS TrackCount,
                CASE
                    WHEN LOWER(a.ArtistName) = LOWER(@ArtistPattern) THEN 1
                    WHEN LOWER(a.ArtistName) LIKE LOWER(@ArtistPattern) + '%' THEN 2
                    ELSE 3
                END AS MatchRank
            FROM FactTrack f
            JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
            JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
            WHERE LOWER(a.ArtistName) LIKE '%' + LOWER(@ArtistPattern) + '%'
              AND (@MinYear IS NULL OR d.Year >= @MinYear)
              AND (@MaxYear IS NULL OR d.Year <= @MaxYear)
            GROUP BY a.ArtistName
            ORDER BY MatchRank, TrackCount DESC, a.ArtistName";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryAsync<ArtistProfileResult>(query, 
            new { ArtistPattern = artistPattern, MinYear = minYear, MaxYear = maxYear });
    }

    public class ArtistProfileResult
    {
        public string ArtistName { get; set; } = string.Empty;
        public double AvgEnergy { get; set; }
        public double AvgDanceability { get; set; }
        public double AvgValence { get; set; }
        public double AvgTempo { get; set; }
        public double AvgAcousticness { get; set; }
        public double AvgInstrumentalness { get; set; }
        public double AvgLiveness { get; set; }
        public double AvgSpeechiness { get; set; }
        public double AvgLoudness { get; set; }
        public int TrackCount { get; set; }
    }
}
