using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult RefreshDelegate(
    RefreshRequest request,
    DatabaseAbstract<Account> accountTable,
    DatabaseAbstract<RefreshToken> tokenTable,
    Tokens tokens,
    TimeProvider timeProvider,
    ClaimsPrincipal user);
