using LoginAuth.Data.Abstraction;
using LoginAuth.Model;

namespace LoginAuth.Validation;

static class AccountValidation
{
    extension(Account)
    {
        public static Account? Create(string email, string password) => Validate(email, password);

        public static Account? Restore(DatabaseAbstract<Account> database, string email, string password) =>
            database.FindAll().FirstOrDefault(x => x.Email == email && x.Password == password);

        private static Account? Validate(string email, string password) =>
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            email.Length > 254 ||
            password.Length < 8 ||
            password.Length > 64
                ? null
                : new Account { Email = email, Password = password };
    }
}