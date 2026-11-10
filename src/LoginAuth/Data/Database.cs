using Dapper;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace LoginAuth.Data;

public class Database(IConfiguration configuration) : DatabaseAbstract(configuration)
{
    public override bool Add<T>(T item)
    {
        if (!EntityReflection.TryConfirm(item, out var allowedType)) return false;

        var tableName = EntityReflection.GetTableName(allowedType);
        var props = EntityReflection.GetProperties(allowedType);
        if (props.Length == 0) return false;

        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

        try
        {
            return SqlConnected(conn => conn.Execute(sql, item)) > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Remove<T>(int? id)
    {
        if (!EntityReflection.IsAllowed<T>()) return false;

        var tableName = EntityReflection.GetTableName<T>();
        var primaryKeyName = EntityReflection.GetPrimaryKey<T>().Name;
        var sql = $"DELETE FROM {tableName} WHERE {primaryKeyName} = @Id";

        try
        {
            return SqlConnected(conn => conn.Execute(sql, new { Id = id })) > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override bool Update<T>(T item)
    {
        if (!EntityReflection.TryConfirm(item, out var allowedType)) return false;

        var tableName = EntityReflection.GetTableName(allowedType);
        var props = EntityReflection.GetProperties(allowedType);
        if (props.Length == 0) return false;

        var primaryKeyName = EntityReflection.GetPrimaryKey(allowedType).Name;
        var fieldsToSet = string.Join(", ", props
            .Where(p => p.Name != primaryKeyName)
            .Select(p => $"{p.Name} = @{p.Name}"));

        var sql = $"UPDATE {tableName} SET {fieldsToSet} WHERE {primaryKeyName} = @{primaryKeyName}";

        try
        {
            return SqlConnected(conn => conn.Execute(sql, item)) > 0;
        }
        catch (SqliteException) { return false; }
    }

    public override T? FindByID<T>(int? id) where T : class
    {
        if (!EntityReflection.IsAllowed<T>()) return null;

        var tableName = EntityReflection.GetTableName<T>();
        var primaryKeyName = EntityReflection.GetPrimaryKey<T>().Name;
        var sql = $"SELECT * FROM {tableName} WHERE {primaryKeyName} = @Id";

        return SqlConnected(conn => conn.QueryFirstOrDefault<T>(sql, new { Id = id }));
    }

    public override IEnumerable<T> FindAll<T>()
    {
        if (!EntityReflection.IsAllowed<T>()) return [];

        var tableName = EntityReflection.GetTableName<T>();
        return SqlConnected(conn => conn.Query<T>($"SELECT * FROM {tableName}"));
    }

    public override async Task<bool> ExecuteAsync(string? query, dynamic? param = null)
    {
        if (string.IsNullOrEmpty(query)) return false;
        return await SqlConnected(conn => conn.ExecuteAsync(query, (object?)param)) > 0;
    }

    private T SqlConnected<T>(Func<SqliteConnection, T> func)
    {
        using var connection = new SqliteConnection(ConnectionString);
        try { connection.Open(); }
        catch (SqliteException ex) { Console.WriteLine(ex.Message); }
        return func(connection);
    }

    private static class EntityReflection
    {
        public static readonly HashSet<Type> AllowedTypes =
        [
            typeof(Account),
            typeof(RefreshToken),
            typeof(EmailConfirmation)
        ];

        public static bool IsAllowed<T>() => AllowedTypes.Contains(typeof(T));

        public static string GetTableName<T>() => typeof(T).Name;
        public static string GetTableName(Type type) => type.Name;

        public static PropertyInfo[] GetProperties<T>() => GetProperties(typeof(T));
        public static PropertyInfo[] GetProperties(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        public static PropertyInfo GetPrimaryKey<T>() => GetPrimaryKey(typeof(T));
        public static PropertyInfo GetPrimaryKey(Type type)
        {
            var props = GetProperties(type);
            return props.FirstOrDefault(p =>
                       p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                       p.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase))
                   ?? props.First();
        }

        public static bool TryConfirm<T>(T item, out Type confirmedType)
        {
            confirmedType = default!;
            if (item is null) return false;
            var generic = typeof(T);
            if (!AllowedTypes.Contains(generic)) return false;
            confirmedType = generic;
            return true;
        }
    }
}