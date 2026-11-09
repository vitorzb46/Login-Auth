using LoginAuth.Data.Abstraction;

namespace LoginAuth.Model;

static class AccountExtension
{
    extension(Account)
    {
        public static Account? Restore(DatabaseAbstract db, string email) =>
            db.FindAll<Account>().FirstOrDefault(a => a.Email == email);
    }
}