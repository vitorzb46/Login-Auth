using Dapper;
using LoginAuth.Model;
using System.Reflection;

namespace LoginAuth.Data;

public static class EntityReflection
{
    public static readonly IReadOnlySet<Type> AllowedTypes = new HashSet<Type>
    {
        typeof(Account),
        typeof(RefreshToken),
        typeof(EmailConfirmation),
        typeof(PasswordReset)
    };

    public static bool IsAllowed<T>() => IsAllowed(typeof(T));

    public static bool IsAllowed(Type? type) => type is not null && AllowedTypes.Contains(type);

    public static string GetTableName<T>() => GetTableName(typeof(T));

    public static string GetTableName(Type type) => type.Name;

    public static PropertyInfo[] GetProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    public static PropertyInfo GetPrimaryKey<T>() => GetPrimaryKey(typeof(T));

    public static PropertyInfo GetPrimaryKey(Type type)
    {
        var props = GetProperties(type);
        return props.FirstOrDefault(p =>
                   p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                   p.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase))
               ?? props[0];
    }

    public static bool TryConfirm<T>(T item, out Type confirmedType)
    {
        confirmedType = item?.GetType()!;
        return IsAllowed(confirmedType);
    }

    public static bool NeedsAutoIncrement(Type type, object item)
    {
        var pk = GetPrimaryKey(type);
        if (pk.PropertyType != typeof(int) && pk.PropertyType != typeof(int?))
            return false;

        var current = pk.GetValue(item);
        return current is null || current.Equals(0);
    }

    public static PropertyInfo[] GetInsertProperties(Type type, bool excludeId)
    {
        PropertyInfo[] props = [.. GetProperties(type)
            .Where(p => p.GetSetMethod(nonPublic: true) is not null)
            .Where(p => IsScalar(p.PropertyType))];

        if (!excludeId) return props;

        var pk = GetPrimaryKey(type);
        return [.. props.Where(p => p != pk)];
    }

    public static (string? Sql, DynamicParameters? Params, bool NeedsAutoIncrement) BuildInsert(Type type, object item)
    {
        var needsAutoId = NeedsAutoIncrement(type, item);
        var props = GetInsertProperties(type, needsAutoId);
        if (props.Length == 0) return (null, null, false);

        DynamicParameters parameters = new DynamicParameters();
        foreach (var p in props)
            parameters.Add(p.Name, p.GetValue(item));

        var columns = string.Join(", ", props.Select(p => p.Name));
        var values  = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {GetTableName(type)} ({columns}) VALUES ({values})";
        return (sql, parameters, needsAutoId);
    }

    public static void SetGeneratedId(object item, long rowId)
    {
        var pk = GetPrimaryKey(item.GetType());
        var targetType = Nullable.GetUnderlyingType(pk.PropertyType) ?? pk.PropertyType;
        pk.SetValue(item, Convert.ChangeType(rowId, targetType));
    }

    private static readonly HashSet<Type> _scalarTypes =
    [
        typeof(string), typeof(bool), typeof(int),
        typeof(float), typeof(double), typeof(decimal),
        typeof(Guid), typeof(DateTime),
    ];

    private static bool IsScalar(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        return u.IsEnum || _scalarTypes.Contains(u);
    }
}