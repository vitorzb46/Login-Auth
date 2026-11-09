using System.Reflection;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.Configuration;

namespace LoginAuth.Data;

public class FakeDatabase(IConfiguration configuration) : DatabaseAbstract(configuration)
{
    private readonly Dictionary<Type, List<object>> _tables = [];
    private readonly Dictionary<Type, int> _autoIncrements = [];

    public override bool Add<T>(T item)
    {
        if (!EntityReflection.TryConfirm(item, out var allowedType)) return false;

        var table = GetTable(allowedType);
        if (HasUniqueViolation(table, item!)) return false;
        
        var nextId = _autoIncrements.TryGetValue(allowedType, out var current) ? current : 1;
        EntityReflection.GetPrimaryKey(allowedType).SetValue(item, nextId);
        _autoIncrements[allowedType] = nextId + 1;

        table.Add(item!);
        return true;
    }

    public override bool Remove<T>(int? id)
    {
        if (!EntityReflection.IsAllowed<T>()) return false;

        var table = GetTable(typeof(T));
        var pk = EntityReflection.GetPrimaryKey<T>();
        var item = table.FirstOrDefault(x => Equals(pk.GetValue(x), id));
        if (item is null) return false;

        table.Remove(item);
        return true;
    }

    public override bool Update<T>(T item)
    {
        if (!EntityReflection.TryConfirm(item, out var allowedType)) return false;

        var table = GetTable(allowedType);
        var pk = EntityReflection.GetPrimaryKey(allowedType);
        var id = pk.GetValue(item);
        var index = table.FindIndex(x => Equals(pk.GetValue(x), id));
        if (index == -1) return false;

        table[index] = item!;
        return true;
    }

    public override T? FindByID<T>(int? id) where T : class
    {
        if (!EntityReflection.IsAllowed<T>()) return null;

        var table = GetTable(typeof(T));
        var pk = EntityReflection.GetPrimaryKey<T>();
        return table.OfType<T>().FirstOrDefault(x => Equals(pk.GetValue(x), id));
    }

    public override IEnumerable<T> FindAll<T>()
    {
        if (!EntityReflection.IsAllowed<T>()) return [];
        return GetTable(typeof(T)).OfType<T>().ToList();
    }

    private List<object> GetTable(Type type)
    {
        if (!_tables.TryGetValue(type, out var table))
        {
            table = [];
            _tables[type] = table;
        }
        return table;
    }

    private static bool HasUniqueViolation(List<object> table, object item) => item switch
    {
        Account account => table.OfType<Account>().Any(a => a.Email == account.Email),
        // Others constraints
        _ => false
    };

    public override Task<bool> ExecuteAsync(string? query)
    {
        throw new NotImplementedException();
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