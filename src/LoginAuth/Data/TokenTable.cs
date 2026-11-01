using Dapper;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Data.Sqlite;

namespace LoginAuth.Data;

public class TokenTable(IConfiguration configuration) : DatabaseAbstract<RefreshToken>(configuration)
{
    public override bool Add(RefreshToken item)
    {
        try
        {
            var sqlInsert = @"
                INSERT INTO [RefreshToken] ([Id], [Token], [AccountId], [ExpiresAt])
                VALUES (@Id, @Token, @AccountId, @ExpiresAt)";
            var changedRows = SqlConnected(conn => conn.Execute(sqlInsert, item));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Remove(int? id)
    {
        try
        {
            var changedRows = SqlConnected(conn => conn.Execute("DELETE FROM RefreshToken WHERE AccountId = @AccountId", new { AccountId = id }));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Update(RefreshToken item)
    {
        try
        {
            var sqlUpdate = @"
                UPDATE RefreshToken SET IsRevoked = 1 WHERE Id = @Id";
            var changedRows = SqlConnected(conn => conn.Execute(sqlUpdate, item.Id));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override RefreshToken? FindByID(int? id) =>
        SqlConnected(conn => conn.QueryFirstOrDefault<RefreshToken>("SELECT * FROM RefreshToken WHERE Id = @Id", new { Id = id })
                ?? null);

    public override IEnumerable<RefreshToken> FindAll() =>
        SqlConnected(conn => conn.Query<RefreshToken>("SELECT * FROM RefreshToken"));

    private T SqlConnected<T>(Func<SqliteConnection, T> func)
    {
        using var connection = new SqliteConnection(ConnectionString);
        try { connection.Open(); }
        catch (SqliteException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return func(connection);
    }
}
