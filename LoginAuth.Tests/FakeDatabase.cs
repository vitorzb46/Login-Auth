using System.Reflection;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.Configuration;

namespace LoginAuth.Tests;

public class FakeDatabase(IConfiguration configuration) : DatabaseAbstract(configuration)
{
    private readonly Dictionary<Type, List<object>> _tables = [];
    private readonly Dictionary<Type, int> _autoIncrements = [];

    public override T? Add<T>(T item) where T : class
    {
        if (!EntityReflection.TryConfirm(item, out var allowedType)) return null;

        var table = GetTable(allowedType);
        if (HasUniqueViolation(table, item!)) return null;

        AssignPrimaryKey(EntityReflection.GetPrimaryKey(allowedType), item, allowedType);
        table.Add(item);
        return item;
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
        if (id is null) return null;
        if (!EntityReflection.IsAllowed<T>()) return null;

        var table = GetTable(typeof(T));
        var pk = EntityReflection.GetPrimaryKey<T>();

        return table.OfType<T>().FirstOrDefault(x =>
        {
            var pkValue = pk.GetValue(x);
            if (pkValue is null) return false;
            return Equals(pkValue, id) || string.Equals(pkValue.ToString(), id.Value.ToString(), StringComparison.Ordinal);
        });
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

    public override Task<bool> ExecuteAsync(string? query, dynamic? param = null)
    {
        if (string.IsNullOrEmpty(query) || param is null) return Task.FromResult(false);
        object p = param;

        if (query.Contains("INSERT INTO EmailConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            var entityType = typeof(EmailConfirmation);
            if (!_tables.TryGetValue(entityType, out var list))
            {
                list = [];
                _tables[entityType] = list;
            }
            
            var entity = new EmailConfirmation
            {
                AccountId = (int)EntityReflection.GetPropertyValue(p, "Id")!,
                Code      = (string)EntityReflection.GetPropertyValue(p, "Code")!,
                ExpiresAt = (DateTime)EntityReflection.GetPropertyValue(p, "ExpiresAt")!
            };
            
            var existing = list.Cast<EmailConfirmation>()
                               .FirstOrDefault(e => e.AccountId == entity.AccountId);
            
            if (existing != null)
            {
                existing.Code = entity.Code;
                existing.ExpiresAt = entity.ExpiresAt;
            }
            else
            {
                list.Add(entity);
            }

            return Task.FromResult(true);
        }
        if (query.Contains("DELETE FROM EmailConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            if (_tables.TryGetValue(typeof(EmailConfirmation), out var list))
            {
                int accountId = (int)EntityReflection.GetPropertyValue(p, "AccountId")!;
                int removed = list.RemoveAll(x => ((EmailConfirmation)x).AccountId == accountId);
                return Task.FromResult(removed > 0);
            }
            return Task.FromResult(false);
        }
        throw new NotImplementedException($"Query não implementada no FakeDatabase: {query}");
    }

    private void AssignPrimaryKey(PropertyInfo pk, object item, Type type)
    {
        if (pk.PropertyType == typeof(int) || pk.PropertyType == typeof(int?))
        {
            var nextId = _autoIncrements.TryGetValue(type, out var current) ? current : 1;
            pk.SetValue(item, nextId);
            _autoIncrements[type] = nextId + 1;
        }
        else if (pk.PropertyType == typeof(string))
        {
            if (string.IsNullOrEmpty(pk.GetValue(item) as string))
                pk.SetValue(item, Guid.NewGuid().ToString());
        }
        // Others types
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

            var runtimeType = item.GetType();
            if (!AllowedTypes.Contains(runtimeType)) return false;

            confirmedType = runtimeType;
            return true;
        }

        public static object? GetPropertyValue(object source, string name)
        {
            ArgumentNullException.ThrowIfNull(source);
            var prop = GetProperties(source.GetType())
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return prop?.GetValue(source);
        }
    }
}