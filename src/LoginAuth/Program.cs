using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DependencyInjection;
using LoginAuth.DTO;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;
using LoginAuth.Validation;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/register", (RegisterRequest request, DatabaseAbstract<Account> database, AccountPasswordHasher hasher) =>
{
    if (!request.Validate())
        return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

    if (!database.Add(hasher.HashPassword(request)))
        return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

    return Results.Ok();
}).AllowAnonymous();

app.MapPost("/login", (LoginRequest request,
                       AccountPasswordHasher hasher,
                       DatabaseAbstract<Account> accountTable,
                       DatabaseAbstract<RefreshToken> tokenTable,
                       Tokens tokens) =>
{
    var getAccount = Account.Restore(accountTable, request.Email);
    if (getAccount is null)
        return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

    if (!hasher.VerifyPassword(request, getAccount.PasswordHash))
        return Results.Unauthorized();

    // token
    var token = tokens.Generate(getAccount.Id.ToString(), getAccount.Email);
    // save refresh token on db
    var rt = new RefreshToken
    {
        Id = Guid.NewGuid().ToString(),
        AccountId = getAccount.Id,
        Token = token.RefreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    tokenTable.Add(rt);

    return Results.Ok(token);
}).AllowAnonymous();

app.MapPost("/protected", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                 ?? user.FindFirst("sub")?.Value;
                 
    var email = user.FindFirst(ClaimTypes.Email)?.Value 
                ?? user.FindFirst("email")?.Value;
                
    return Results.Ok(new
    {
        Message = "User authenticated!",
        UserId = userId,
        Email = email,
        ValidatedAt = DateTime.UtcNow
    });
}).RequireAuthorization(); 

app.Run();