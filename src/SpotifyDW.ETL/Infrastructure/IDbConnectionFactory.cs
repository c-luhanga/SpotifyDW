using System.Data;

namespace SpotifyDW.ETL.Infrastructure;

/// <summary>
/// Factory interface for creating database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <returns>A new IDbConnection instance.</returns>
    IDbConnection CreateConnection();
}
