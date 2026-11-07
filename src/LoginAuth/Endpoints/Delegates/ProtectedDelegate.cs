using System.Security.Claims;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult ProtectedDelegate(ClaimsPrincipal user, TimeProvider timeProvider);
