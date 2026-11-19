using LoginAuth.Endpoints.v1;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LoginAuth.Endpoints;

public static class MapEndpoints
{
    public static IEndpointRouteBuilder MyEndpoints(this IEndpointRouteBuilder app)
    {
        var fixedWindow = app.MapGroup("/")
            .AllowAnonymous()
            .RequireRateLimiting("fixed-policy");

        var slidingWindow = app.MapGroup("/")
            .RequireAuthorization()
            .RequireRateLimiting("user-policy");

        fixedWindow.MapPost("/register", Register.Handler);
        fixedWindow.MapPost("/login", Login.Handler);
        fixedWindow.MapPost("/login/mfa", Login.MfaHandler);
        fixedWindow.MapPost("/email/confirmation", Email.Handler);
        fixedWindow.MapPost("/recover-password", Password.RecoverHandler);
        fixedWindow.MapPost("/reset-password", Password.ResetHandler);

        fixedWindow.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        fixedWindow.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        slidingWindow.MapPost("/protected", Protected.Handler);
        slidingWindow.MapPost("/refresh", Refresh.Handler);
        slidingWindow.MapPost("/setup", TwoFactor.SetupHandler);
        slidingWindow.MapPost("/confirm", TwoFactor.ConfirmHandler);

        app.MapPost("/test", () => Results.Ok(new { Message = "Test done!" }))
           .AllowAnonymous()
           .RequireRateLimiting("user-policy");

        return app;
    }
}