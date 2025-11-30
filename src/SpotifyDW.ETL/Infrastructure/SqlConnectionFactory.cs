using System.Data;
using Microsoft.Data.SqlClient;

namespace SpotifyDW.ETL.Infrastructure;

/// <summary>
/// SQL Server implementation of the database connection factory.
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
