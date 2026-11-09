using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult RefreshDelegate(
    RefreshRequest request,
    DatabaseAbstract db,
    Tokens tokens,
    TimeProvider timeProvider,
    ClaimsPrincipal user);
