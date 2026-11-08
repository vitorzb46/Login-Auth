using LoginAuth.DependencyInjection;
using LoginAuth.Endpoints.v1;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddDependencies(builder);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

RouteGroupBuilder @fixed = app.MapGroup("/")
    .AllowAnonymous()
    .RequireRateLimiting("fixed-policy");

RouteGroupBuilder sliding = app.MapGroup("/")
    .RequireAuthorization()
    .RequireRateLimiting("user-policy");

@fixed.MapPost("/register", Register.Handler);
@fixed.MapPost("/login", Login.Handler);
@fixed.MapPost("/login/mfa", Login.MfaHandler);
sliding.MapPost("/protected", Protected.Handler);
sliding.MapPost("/refresh", Refresh.Handler);
sliding.MapPost("/setup", TwoFactor.SetupHandler);
sliding.MapPost("/confirm", TwoFactor.ConfirmHandler);

app.MapPost("/test", () =>
{
    return Results.Ok(new
    {
        Message = "Test done!"
    });
}).AllowAnonymous()
  .RequireRateLimiting("user-policy");
  
app.Run();