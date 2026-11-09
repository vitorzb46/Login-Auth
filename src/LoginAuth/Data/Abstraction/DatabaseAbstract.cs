using LoginAuth.Properties;

namespace LoginAuth.Data.Abstraction;

public abstract class DatabaseAbstract
{
    protected string ConnectionString { get; } = string.Empty;
    public DatabaseAbstract(IConfiguration configuration)
    {
        ConnectionString = configuration.ConnectionString();

        Seed.CreateDb(configuration);
    }
    public abstract bool Add<T>(T item);
    public abstract bool Remove<T>(int? id);
    public abstract bool Update<T>(T item);
    public abstract T? FindByID<T>(int? id) where T : class;
    public abstract IEnumerable<T> FindAll<T>();
    public abstract Task<bool> ExecuteAsync(string? query);
}