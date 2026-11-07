using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult TwoFactorConfirmDelegate(
    TwoFactorRequest request,
    ClaimsPrincipal principal,
    DatabaseAbstract<Account> db,
    TwoFactorsAuth tfa);
