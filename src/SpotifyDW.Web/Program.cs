using Dapper;
using SpotifyDW.Web.Infrastructure;
using SpotifyDW.Web.Services.Reports;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register database connection factory
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' not found in configuration.");
}

builder.Services.AddScoped<IDbConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

// Register report services
builder.Services.AddScoped<TopArtistsByYearService>();
builder.Services.AddScoped<TopTracksForArtistService>();
builder.Services.AddScoped<PopularityTrendByYearService>();
builder.Services.AddScoped<CompareTwoArtistsService>();
builder.Services.AddScoped<AudioProfileService>();
builder.Services.AddScoped<CustomReportBuilderService>();

// Register home stats service
builder.Services.AddScoped<SpotifyDW.Web.Services.HomeStatsService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Minimal API endpoint for artist autocomplete

app.MapGet("/api/artists/suggest", async (string term, IDbConnectionFactory dbFactory) =>
{
    if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        return Results.Ok(Array.Empty<string>());

    using var conn = dbFactory.CreateConnection();
    var sql = @"
        SELECT TOP 10 ArtistName
        FROM DimArtist a
        LEFT JOIN FactTrack f ON a.ArtistKey = f.ArtistKey
        WHERE LOWER(ArtistName) LIKE '%' + LOWER(@pattern) + '%'
        GROUP BY a.ArtistName
        ORDER BY
            CASE
                WHEN LOWER(a.ArtistName) = LOWER(@pattern) THEN 1
                WHEN LOWER(a.ArtistName) LIKE LOWER(@pattern) + '%' THEN 2
                ELSE 3
            END,
            ISNULL(MAX(f.TrackPopularity), 0) DESC,
            a.ArtistName
    ";
    var results = await conn.QueryAsync<string>(sql, new { pattern = term });
    return Results.Ok(results);
});

app.MapRazorPages();

app.Run();
