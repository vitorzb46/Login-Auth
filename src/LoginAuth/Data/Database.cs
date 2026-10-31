using Dapper;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Data.Sqlite;

namespace LoginAuth.Data;

public class Database(IConfiguration configuration) : DatabaseAbstract<Account>(configuration)
{
    public override bool Add(Account item)
    {
        try
        {
            var changedRows = SqlConnected(conn => conn.Execute("INSERT INTO Account (Email, PasswordHash) VALUES (@Email, @PasswordHash)", item));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Remove(int? id)
    {
        try
        {
            var changedRows = SqlConnected(conn => conn.Execute("DELETE FROM Account WHERE Id = @Id", new { Id = id }));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Update(Account item)
    {
        try
        {
            var changedRows = SqlConnected(conn => conn.Execute("UPDATE Account SET Email = @Email, PasswordHash = @PasswordHash WHERE Id = @Id", item));
            return changedRows > 0;
        }
        catch (SqliteException) { return false; }        
    }

    public override Account? FindByID(int? id) =>
        SqlConnected(conn => conn.QueryFirstOrDefault<Account>("SELECT * FROM Account WHERE Id = @Id", new { Id = id })
                ?? null);

    public override IEnumerable<Account> FindAll() =>
        SqlConnected(conn => conn.Query<Account>("SELECT * FROM Account"));

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