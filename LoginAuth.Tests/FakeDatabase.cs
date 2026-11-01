using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.Configuration;

namespace LoginAuth.Tests;

public class FakeDatabase(IConfiguration configuration) : DatabaseAbstract<Account>(configuration)
{
    private List<Account> _accounts = [];

    public override bool Add(Account item)
    {
        _accounts.Add(item);
        return true;
    }

    public override IEnumerable<Account> FindAll() => _accounts;

    public override Account? FindByID(int? id) => _accounts.FirstOrDefault(a => a.Id == id) ?? null;

    public override bool Remove(int? id)
    {
        var item = _accounts.FirstOrDefault(a => a.Id == id);
        if (item == null) return false;
        _accounts.Remove(item);
        return true;
    }

    public override bool Update(Account item)
    {
        var index = _accounts.FindIndex(a => a.Id == item.Id);
        if (index == -1) return false;
        _accounts[index] = item;
        return true;
    }
}