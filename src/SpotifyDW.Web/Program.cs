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

app.MapRazorPages();

app.Run();
