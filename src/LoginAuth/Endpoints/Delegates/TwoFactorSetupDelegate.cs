using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult TwoFactorSetupDelegate(
    ClaimsPrincipal principal,
    DatabaseAbstract<Account> db,
    TwoFactorsAuth tfa);
