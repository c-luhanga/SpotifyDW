using Dapper;
using SpotifyDW.Web.Infrastructure;
using System.Text;

namespace SpotifyDW.Web.Services.Reports;

public class CustomReportBuilderService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CustomReportBuilderService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<CustomReportRow>> ExecuteCustomReportAsync(
        MeasureType measure,
        GroupingType grouping,
        int? minYear = null,
        int? maxYear = null,
        int? minPopularity = null,
        string? artistPattern = null)
    {
        var query = BuildQuery(measure, grouping);

        var parameters = new
        {
            MinYear = minYear,
            MaxYear = maxYear,
            MinPopularity = minPopularity,
            ArtistPattern = artistPattern
        };

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        return await connection.QueryAsync<CustomReportRow>(query, parameters);
    }

    private static string BuildQuery(MeasureType measure, GroupingType grouping)
    {
        var sql = new StringBuilder();

        // Determine measure column
        var measureColumn = measure switch
        {
            MeasureType.Energy => "Energy",
            MeasureType.Danceability => "Danceability",
            MeasureType.Valence => "Valence",
            MeasureType.Tempo => "Tempo",
            _ => "TrackPopularity"
        };

        // SELECT clause
        sql.Append("SELECT ");

        switch (grouping)
        {
            case GroupingType.Artist:
                sql.Append("a.ArtistName AS GroupValue1, NULL AS GroupValue2");
                break;
            case GroupingType.Year:
                sql.Append("CAST(d.Year AS NVARCHAR(50)) AS GroupValue1, NULL AS GroupValue2");
                break;
            case GroupingType.Album:
                sql.Append("al.AlbumName AS GroupValue1, NULL AS GroupValue2");
                break;
            case GroupingType.ArtistYear:
                sql.Append("a.ArtistName AS GroupValue1, CAST(d.Year AS NVARCHAR(50)) AS GroupValue2");
                break;
        }

        sql.AppendLine($", AVG(CAST(f.{measureColumn} AS FLOAT)) AS AvgValue");
        sql.AppendLine("     , COUNT(*) AS TrackCount");

        // FROM clause
        sql.AppendLine("FROM FactTrack f");

        // JOIN clauses based on grouping
        bool needsArtist = grouping == GroupingType.Artist || grouping == GroupingType.ArtistYear;
        bool needsAlbum = grouping == GroupingType.Album;
        bool needsDate = grouping == GroupingType.Year || grouping == GroupingType.ArtistYear;

        if (needsArtist)
            sql.AppendLine("JOIN DimArtist a ON f.ArtistKey = a.ArtistKey");
        if (needsAlbum)
            sql.AppendLine("JOIN DimAlbum al ON f.AlbumKey = al.AlbumKey");
        if (needsDate)
            sql.AppendLine("JOIN DimDate d ON f.ReleaseDateKey = d.DateKey");

        // WHERE clause with filters
        sql.AppendLine("WHERE 1=1");

        if (needsDate)
        {
            sql.AppendLine("  AND (@MinYear IS NULL OR d.Year >= @MinYear)");
            sql.AppendLine("  AND (@MaxYear IS NULL OR d.Year <= @MaxYear)");
        }

        sql.AppendLine("  AND (@MinPopularity IS NULL OR f.TrackPopularity >= @MinPopularity)");

        if (needsArtist)
        {
            sql.AppendLine("  AND (@ArtistPattern IS NULL OR a.ArtistName LIKE '%' + @ArtistPattern + '%')");
        }

        // GROUP BY clause
        sql.Append("GROUP BY ");
        switch (grouping)
        {
            case GroupingType.Artist:
                sql.AppendLine("a.ArtistName");
                break;
            case GroupingType.Year:
                sql.AppendLine("d.Year");
                break;
            case GroupingType.Album:
                sql.AppendLine("al.AlbumName");
                break;
            case GroupingType.ArtistYear:
                sql.AppendLine("a.ArtistName, d.Year");
                break;
        }

        // ORDER BY clause
        sql.AppendLine("ORDER BY AvgValue DESC");

        return sql.ToString();
    }

    public enum MeasureType
    {
        Popularity,
        Energy,
        Danceability,
        Valence,
        Tempo
    }

    public enum GroupingType
    {
        Artist,
        Year,
        Album,
        ArtistYear
    }

    public class CustomReportRow
    {
        public string? GroupValue1 { get; set; }
        public string? GroupValue2 { get; set; }
        public double AvgValue { get; set; }
        public int TrackCount { get; set; }
    }
}
