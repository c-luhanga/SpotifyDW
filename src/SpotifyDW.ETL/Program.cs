using Microsoft.Extensions.Configuration;
using SpotifyDW.ETL.Services;

Console.WriteLine("=== SpotifyDW ETL Application ===\n");

// Build configuration
var baseDirectory = AppContext.BaseDirectory;
var configuration = new ConfigurationBuilder()
    .SetBasePath(baseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Read configuration values
var connectionString = configuration.GetConnectionString("SpotifyDW");
var spotifyDataPath = configuration["DataSources:SpotifyDataCleanPath"];
var trackDataPath = configuration["DataSources:TrackDataFinalPath"];

// Display configuration
Console.WriteLine("Configuration loaded successfully:");
Console.WriteLine($"  Connection String: {connectionString}");
Console.WriteLine($"  Spotify Data CSV: {spotifyDataPath}");
Console.WriteLine($"  Track Data CSV: {trackDataPath}");

// Resolve full paths
var spotifyFullPath = Path.GetFullPath(Path.Combine(baseDirectory, spotifyDataPath!));
var trackFullPath = Path.GetFullPath(Path.Combine(baseDirectory, trackDataPath!));

// Validate CSV files exist
Console.WriteLine("\nValidating data files:");
Console.WriteLine($"  {spotifyFullPath}: {(File.Exists(spotifyFullPath) ? "✓ Found" : "✗ Not Found")}");
Console.WriteLine($"  {trackFullPath}: {(File.Exists(trackFullPath) ? "✓ Found" : "✗ Not Found")}");

if (!File.Exists(spotifyFullPath) || !File.Exists(trackFullPath))
{
    Console.WriteLine("\n❌ ERROR: One or more CSV files not found. Exiting...");
    return;
}

try
{
    // EXTRACT phase
    var extractService = new ExtractService();
    var allTracks = extractService.ExtractTracks(spotifyFullPath, trackFullPath);
    
    Console.WriteLine("\n✓ Extract phase completed successfully!");
    
    // TODO: TRANSFORM phase (next step)
    // TODO: LOAD phase (next step)
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nETL process completed.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
