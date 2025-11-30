using System.Data;

namespace SpotifyDW.ETL.Reports;

/// <summary>
/// Interface for all reporting classes.
/// </summary>
public interface IReport
{
    /// <summary>
    /// Gets the name of the report.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the report.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the report using the provided database connection.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunAsync(IDbConnection connection);
}
