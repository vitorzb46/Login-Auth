using System.Security.Claims;
using LoginAuth.Endpoints.Delegates;

namespace LoginAuth.Endpoints.v1;

public class Protected
{
    public static ProtectedDelegate Handler => GetResult;

    private static IResult GetResult(ClaimsPrincipal user, TimeProvider timeProvider)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value;

        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;

        var currentUtc = timeProvider.GetUtcNow().UtcDateTime;

        return Results.Ok(new
        {
            Message = "User authenticated!",
            UserId = userId,
            Email = email,
            ValidatedAt = currentUtc
        });
    }
}