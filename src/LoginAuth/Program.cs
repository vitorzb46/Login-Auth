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

app.MapPost("/login", (LoginRequest request, DatabaseAbstract<Account> database, AccountPasswordHasher hasher) =>
{
    var getAccount = Account.Restore(database, request.Email);
    if (getAccount is null)
        return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);
        
    if (!hasher.VerifyPassword(request, getAccount.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok();
}).AllowAnonymous();

app.Run();