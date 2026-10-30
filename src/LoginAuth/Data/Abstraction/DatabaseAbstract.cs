using LoginAuth.Properties;

namespace LoginAuth.Data.Abstraction;

public abstract class DatabaseAbstract<T>
{
    protected string ConnectionString { get; } = string.Empty;
    public DatabaseAbstract(IConfiguration configuration)
    {
        ConnectionString = configuration.ConnectionString();

        Seed.CreateDb(configuration);
    }
    public abstract bool Add(T item);
    public abstract bool Remove(int? id);
    public abstract bool Update(T item);
    public abstract T? FindByID(int? id);
    public abstract IEnumerable<T> FindAll();
}