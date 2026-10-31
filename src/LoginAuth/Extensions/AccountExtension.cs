using LoginAuth.Data.Abstraction;

namespace LoginAuth.Model;

static class AccountExtension
{
    extension(Account)
    {
        public static Account? Restore(DatabaseAbstract<Account> db, string email) =>
            db.FindAll().FirstOrDefault(a => a.Email == email);
    }
}