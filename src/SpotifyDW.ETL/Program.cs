using Microsoft.Extensions.Configuration;
using SpotifyDW.ETL.Infrastructure;
using SpotifyDW.ETL.Reports;
using SpotifyDW.ETL.Services;

// Use async Main to support async operations
await Main(args);

static async Task Main(string[] args)
{
    Console.WriteLine("=== SpotifyDW Application ===\n");

    // Build configuration
    var baseDirectory = AppContext.BaseDirectory;
    var configuration = new ConfigurationBuilder()
        .SetBasePath(baseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    // Read connection string
    var connectionString = configuration.GetConnectionString("SpotifyDW");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("❌ ERROR: Connection string not found in appsettings.json");
        return;
    }

    // Check if running ETL mode
    if (args.Length > 0 && args[0].Equals("--etl", StringComparison.OrdinalIgnoreCase))
    {
        await RunETL(configuration, connectionString);
        return;
    }

    // Default: Run reporting mode
    await RunReporting(connectionString);
}

static async Task RunReporting(string connectionString)
{
    Console.WriteLine("Starting Reporting Mode...\n");

    // Create connection factory
    var connectionFactory = new SqlConnectionFactory(connectionString);

    // Instantiate all reports
    var reports = new List<IReport>
    {
        new TopArtistsByYearReport(),
        new TopTracksForArtistReport(),
        new PopularityTrendByYearReport(),
        new CompareTwoArtistsReport(),
        new AudioProfileReport(),
        new CustomReportBuilderReport()
    };

    // Create and run report menu
    var reportMenu = new ReportMenu(connectionFactory, reports);
    await reportMenu.ShowAsync();
}

static async Task RunETL(IConfiguration configuration, string connectionString)
{
    Console.WriteLine("Starting ETL Mode...\n");

    // Read configuration values
    var spotifyDataPath = configuration["DataSources:SpotifyDataCleanPath"];
    var trackDataPath = configuration["DataSources:TrackDataFinalPath"];

    // Display configuration
    Console.WriteLine("Configuration loaded successfully:");
    Console.WriteLine($"  Connection String: {connectionString}");
    Console.WriteLine($"  Spotify Data CSV: {spotifyDataPath}");
    Console.WriteLine($"  Track Data CSV: {trackDataPath}");

    // Resolve full paths
    var baseDirectory = AppContext.BaseDirectory;
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

        // TRANSFORM phase
        var transformService = new TransformService();
        var transformResult = transformService.TransformData(allTracks);
        Console.WriteLine("\n✓ Transform phase completed successfully!");

        // Display summary
        Console.WriteLine("\n=== TRANSFORMATION SUMMARY ===");
        Console.WriteLine($"  Artists: {transformResult.Artists.Count}");
        Console.WriteLine($"  Albums: {transformResult.Albums.Count}");
        Console.WriteLine($"  Tracks: {transformResult.Tracks.Count}");
        Console.WriteLine($"  Dates: {transformResult.Dates.Count}");
        Console.WriteLine($"  Fact Records: {transformResult.Facts.Count}");

        // LOAD phase
        var loadService = new LoadService(connectionString!);
        loadService.LoadData(transformResult);
        Console.WriteLine("\n✓ Load phase completed successfully!");

        Console.WriteLine("\nETL process completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ ERROR: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();

    await Task.CompletedTask;
}
