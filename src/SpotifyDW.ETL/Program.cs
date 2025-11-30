using Microsoft.Extensions.Configuration;

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

// Validate CSV files exist (resolve relative to bin directory)
var spotifyFullPath = Path.GetFullPath(Path.Combine(baseDirectory, spotifyDataPath!));
var trackFullPath = Path.GetFullPath(Path.Combine(baseDirectory, trackDataPath!));

Console.WriteLine("\nValidating data files:");
Console.WriteLine($"  {spotifyFullPath}: {(File.Exists(spotifyFullPath) ? "✓ Found" : "✗ Not Found")}");
Console.WriteLine($"  {trackFullPath}: {(File.Exists(trackFullPath) ? "✓ Found" : "✗ Not Found")}");

Console.WriteLine("\nETL application ready!");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
