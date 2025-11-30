using SpotifyDW.ETL.Infrastructure;
using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Interactive menu for running reports.
/// </summary>
public class ReportMenu
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IReadOnlyList<IReport> _reports;

    public ReportMenu(IDbConnectionFactory connectionFactory, IReadOnlyList<IReport> reports)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    /// <summary>
    /// Displays the report menu and handles user interaction.
    /// </summary>
    public async Task ShowAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== SpotifyDW Reports ===");
            Console.WriteLine();

            // Display numbered report list
            for (int i = 0; i < _reports.Count; i++)
            {
                Console.WriteLine($"{i + 1}) {_reports[i].Name}");
                Console.WriteLine($"   {_reports[i].Description}");
            }

            Console.WriteLine();
            Console.WriteLine("Q) Quit");
            Console.WriteLine();
            Console.Write("Select a report (1-{0}) or Q to quit: ", _reports.Count);

            var input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Invalid input. Please try again.");
                continue;
            }

            // Check for quit
            if (input == "Q")
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            // Try to parse as a report number
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= _reports.Count)
            {
                var report = _reports[choice - 1];

                Console.WriteLine();
                Console.WriteLine($"Running: {report.Name}");
                Console.WriteLine(new string('-', 60));

                try
                {
                    using var connection = _connectionFactory.CreateConnection();
                    connection.Open();
                    await report.RunAsync(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running report: {ex.Message}");
                }

                Console.WriteLine(new string('-', 60));
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter a number between 1 and {0}, or Q to quit.", _reports.Count);
            }
        }
    }
}
