using Dapper;
using LoginAuth.Properties;
using Microsoft.Data.Sqlite;

namespace LoginAuth.Data;

public class Seed
{
    private static SqliteConnection? _dbConnection;

    public static void CreateDb(IConfiguration configuration)
    {
        if (!File.Exists(configuration.DbFilePath()))
        {
            _dbConnection = new SqliteConnection(configuration.ConnectionString());
            _dbConnection.Open();

            _dbConnection.Execute(@"
                    CREATE TABLE IF NOT EXISTS [Account] (
                        [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [Email] VARCHAR(254) NOT NULL UNIQUE,
                        [PasswordHash] VARCHAR(255) NOT NULL
                    )");
        }
    }
}