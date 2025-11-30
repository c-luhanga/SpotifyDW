using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using SpotifyDW.Web.Infrastructure;

namespace SpotifyDW.Web.Pages.Warehouse;

public class IndexModel : PageModel
{
    private readonly IDbConnectionFactory _dbFactory;
    public IndexModel(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int? Year { get; set; }
    public bool HasSearched { get; set; }

    public List<WarehouseRow> Results { get; set; } = new();

    public class WarehouseRow
    {
        public string ArtistName { get; set; } = string.Empty;
        public string AlbumName { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public async Task OnGetAsync(string? artist, string? album, int? year)
    {
        Artist = artist;
        Album = album;
        Year = year;
        HasSearched = !string.IsNullOrWhiteSpace(artist) || !string.IsNullOrWhiteSpace(album) || year.HasValue;

        var sql = @"SELECT a.ArtistName, al.AlbumName, d.Year
                    FROM DimAlbum al
                    JOIN DimArtist a ON al.ArtistKey = a.ArtistKey
                    JOIN DimDate d ON al.ReleaseDateKey = d.DateKey
                    WHERE (@artist IS NULL OR a.ArtistName LIKE '%' + @artist + '%')
                      AND (@album IS NULL OR al.AlbumName LIKE '%' + @album + '%')
                      AND (@year IS NULL OR d.Year = @year)
                    ORDER BY a.ArtistName, al.AlbumName, d.Year";
        using var conn = _dbFactory.CreateConnection();
        var results = await conn.QueryAsync<WarehouseRow>(sql, new { artist, album, year });
        Results = results.ToList();
    }
}
