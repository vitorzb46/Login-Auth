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

            _dbConnection.Execute("PRAGMA foreign_keys = ON;");

            _dbConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS [Account] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [Email] VARCHAR(254) NOT NULL UNIQUE,
                    [PasswordHash] VARCHAR(255) NOT NULL,
                    [CreatedAt] TEXT NOT NULL DEFAULT (DATETIME('now'))
                )");
            
            _dbConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS [RefreshToken] (
                    [Id] TEXT NOT NULL PRIMARY KEY,
                    [Token] TEXT NOT NULL UNIQUE,
                    [AccountId] INTEGER NOT NULL,
                    [ExpiresAt] TEXT NOT NULL,
                    [IsRevoked] INTEGER NOT NULL DEFAULT 0,
                    [CreatedAt] TEXT NOT NULL DEFAULT (DATETIME('now')),
                    FOREIGN KEY ([AccountId]) REFERENCES [Account] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_RefreshToken_Token ON [RefreshToken] ([Token]);
            ");
        }
    }
}