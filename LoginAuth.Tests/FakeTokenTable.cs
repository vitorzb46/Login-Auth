using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.Configuration;

namespace LoginAuth.Tests;

public class FakeTokenTable(IConfiguration configuration) : DatabaseAbstract<RefreshToken>(configuration)
{
    private List<RefreshToken> _accounts = [];

    public override bool Add(RefreshToken item)
    {
        _accounts.Add(item);
        return true;
    }

    public override IEnumerable<RefreshToken> FindAll() => _accounts;

    public override RefreshToken? FindByID(int? id) =>
        _accounts.FirstOrDefault(a => ToString(a, id)) ?? null;
        
    public override bool Remove(int? id)
    {
        var item = _accounts.FirstOrDefault(a => ToString(a, id));
        if (item == null) return false;
        _accounts.Remove(item);
        return true;
    }

    public override bool Update(RefreshToken item)
    {
        var index = _accounts.FindIndex(a => a.Id == item.Id);
        if (index == -1) return false;
        _accounts[index] = item;
        return true;
    }

    private static bool ToString(RefreshToken a, int? id) => a.Id.ToString() == id.ToString();
}
