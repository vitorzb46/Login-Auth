using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.v1;

public static class Refresh
{
    public static RefreshDelegate Handler => GetResult;

    private static IResult GetResult(RefreshRequest request,
                                     DatabaseAbstract<Account> accountTable,
                                     DatabaseAbstract<RefreshToken> tokenTable,
                                     Tokens tokens,
                                     TimeProvider timeProvider,
                                     ClaimsPrincipal user)
    {
        var oldToken = tokenTable.FindAll().FirstOrDefault(t => t.Token == request.RefreshToken);
        var currentUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (oldToken is null || oldToken.ExpiresAt <= currentUtc)
        {
            RevokeAndUpdateDb(tokenTable, oldToken, 1);
            return Results.Unauthorized();
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;

        if (userId is null || email is null)
            return Results.BadRequest("User credentials not found!");

        var newTokens = tokens.Generate(userId, email);

        oldToken.Token = newTokens.RefreshToken;
        oldToken.ExpiresAt = currentUtc.AddDays(7);
        RevokeAndUpdateDb(tokenTable, oldToken);

        return Results.Ok(oldToken);
    }

    private static void RevokeAndUpdateDb(DatabaseAbstract<RefreshToken> tokenTable, RefreshToken? oldToken, int isRevoked = 0)
    {
        if (oldToken is null) return;
        oldToken.IsRevoked = isRevoked;
        tokenTable.Update(oldToken);
    }
}