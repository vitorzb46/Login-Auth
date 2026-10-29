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
            SqlConnected(conn => conn.Execute("INSERT INTO Account (Email, Password) VALUES (@Email, @Password)", item));
            return true;
        }
        catch (SqliteException) { return false; }
    }

    public override void Remove(int id) =>
        SqlConnected(conn => conn.Execute("DELETE FROM Account WHERE Id = @Id", new { Id = id }));

    public override void Update(Account item) =>
        SqlConnected(conn => conn.Execute("UPDATE Account SET Email = @Email, Password = @Password WHERE Id = @Id", item));

    public override Account? FindByID(int id) =>
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
            //throw;
        }
        return func(connection);
    }
}