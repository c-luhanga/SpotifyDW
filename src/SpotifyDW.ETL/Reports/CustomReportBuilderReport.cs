using Dapper;
using System.Data;
using System.Text;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Interactive report builder allowing users to customize measure, grouping, and filters.
/// </summary>
public class CustomReportBuilderReport : IReport
{
    public string Name => "Custom report builder";

    public string Description => "Lets you choose a measure, grouping, and filters to build a custom summary report.";

    public async Task RunAsync(IDbConnection connection)
    {
        // Step 1: Choose measure
        Console.WriteLine("Choose a measure to analyze:");
        Console.WriteLine("1) Popularity");
        Console.WriteLine("2) Energy");
        Console.WriteLine("3) Danceability");
        Console.WriteLine("4) Valence");
        Console.WriteLine("5) Tempo");
        Console.Write("Select measure (1-5, default 1): ");
        var measureInput = Console.ReadLine();
        
        var (measureColumn, measureName) = measureInput switch
        {
            "2" => ("Energy", "Energy"),
            "3" => ("Danceability", "Danceability"),
            "4" => ("Valence", "Valence"),
            "5" => ("Tempo", "Tempo"),
            _ => ("TrackPopularity", "Popularity")
        };

        // Step 2: Choose grouping
        Console.WriteLine();
        Console.WriteLine("Choose grouping:");
        Console.WriteLine("1) By Artist");
        Console.WriteLine("2) By Year");
        Console.WriteLine("3) By Album");
        Console.WriteLine("4) By Artist+Year");
        Console.Write("Select grouping (1-4, default 1): ");
        var groupingInput = Console.ReadLine();
        
        var groupingChoice = groupingInput switch
        {
            "2" => GroupingType.Year,
            "3" => GroupingType.Album,
            "4" => GroupingType.ArtistYear,
            _ => GroupingType.Artist
        };

        // Step 3: Collect filters
        Console.WriteLine();
        Console.Write("Enter minimum year (leave blank for all): ");
        var minYearInput = Console.ReadLine();
        int? minYear = null;
        if (!string.IsNullOrWhiteSpace(minYearInput) && int.TryParse(minYearInput, out int parsedMinYear))
        {
            minYear = parsedMinYear;
        }

        Console.Write("Enter maximum year (leave blank for all): ");
        var maxYearInput = Console.ReadLine();
        int? maxYear = null;
        if (!string.IsNullOrWhiteSpace(maxYearInput) && int.TryParse(maxYearInput, out int parsedMaxYear))
        {
            maxYear = parsedMaxYear;
        }

        Console.Write("Enter minimum popularity (leave blank for all): ");
        var minPopInput = Console.ReadLine();
        int? minPopularity = null;
        if (!string.IsNullOrWhiteSpace(minPopInput) && int.TryParse(minPopInput, out int parsedMinPop))
        {
            minPopularity = parsedMinPop;
        }

        string? artistFragment = null;
        if (groupingChoice == GroupingType.Artist || groupingChoice == GroupingType.ArtistYear)
        {
            Console.Write("Enter artist name fragment to filter (leave blank for all): ");
            artistFragment = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(artistFragment))
                artistFragment = null;
        }

        // Step 4: Build SQL query
        var query = BuildQuery(measureColumn, groupingChoice);

        // Step 5: Execute query
        var parameters = new
        {
            MinYear = minYear,
            MaxYear = maxYear,
            MinPopularity = minPopularity,
            ArtistFragment = artistFragment
        };

        IEnumerable<dynamic> results;
        try
        {
            results = await connection.QueryAsync(query, parameters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing query: {ex.Message}");
            return;
        }

        var resultList = results.ToList();

        // Step 6: Display results
        if (resultList.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("No data found matching your criteria.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Custom Report: Average {measureName} {GetGroupingDescription(groupingChoice)}");
        Console.WriteLine(new string('=', 90));
        Console.WriteLine();

        // Display header based on grouping type
        if (groupingChoice == GroupingType.ArtistYear)
        {
            Console.WriteLine($"{"Artist",-40} {"Year",-6} {"Avg " + measureName,-15} {"Track Count",-12}");
            Console.WriteLine(new string('-', 90));
            foreach (var row in resultList)
            {
                string artist = row.GroupValue1 ?? "Unknown";
                int year = row.GroupValue2;
                double avgValue = row.AvgValue;
                int trackCount = row.TrackCount;
                Console.WriteLine($"{artist,-40} {year,-6} {avgValue,15:F2} {trackCount,12}");
            }
        }
        else
        {
            Console.WriteLine($"{"Group",-50} {"Avg " + measureName,-15} {"Track Count",-12}");
            Console.WriteLine(new string('-', 90));
            foreach (var row in resultList)
            {
                string groupValue = row.GroupValue?.ToString() ?? "Unknown";
                double avgValue = row.AvgValue;
                int trackCount = row.TrackCount;
                Console.WriteLine($"{groupValue,-50} {avgValue,15:F2} {trackCount,12}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total groups: {resultList.Count}");
    }

    private static string BuildQuery(string measureColumn, GroupingType grouping)
    {
        var sql = new StringBuilder();

        // SELECT clause
        sql.Append("SELECT ");
        
        switch (grouping)
        {
            case GroupingType.Artist:
                sql.Append("a.ArtistName AS GroupValue");
                break;
            case GroupingType.Year:
                sql.Append("d.Year AS GroupValue");
                break;
            case GroupingType.Album:
                sql.Append("al.AlbumName AS GroupValue");
                break;
            case GroupingType.ArtistYear:
                sql.Append("a.ArtistName AS GroupValue1, d.Year AS GroupValue2");
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
        {
            sql.AppendLine("JOIN DimAlbum al ON f.AlbumKey = al.AlbumKey");
        }
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
            sql.AppendLine("  AND (@ArtistFragment IS NULL OR a.ArtistName LIKE '%' + @ArtistFragment + '%')");
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

    private static string GetGroupingDescription(GroupingType grouping)
    {
        return grouping switch
        {
            GroupingType.Artist => "by Artist",
            GroupingType.Year => "by Year",
            GroupingType.Album => "by Album",
            GroupingType.ArtistYear => "by Artist and Year",
            _ => ""
        };
    }

    private enum GroupingType
    {
        Artist,
        Year,
        Album,
        ArtistYear
    }
}
