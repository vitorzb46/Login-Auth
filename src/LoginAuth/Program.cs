using LoginAuth.Data.Abstraction;
using LoginAuth.DependencyInjection;
using LoginAuth.DTO;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
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

builder.Services.AddDependencies();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/register", (RegisterRequest request, DatabaseAbstract<Account> database) =>
{
    var newAccount = Account.Create(request.Email, request.Password);
    if (newAccount is null)
        return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

    database.Add(newAccount);

    // TODO: Validate if account was created on database

    return Results.Ok();
}).AllowAnonymous();

app.MapPost("/login", (LoginRequest request, DatabaseAbstract<Account> database) =>
{
    return Account.Restore(database, request.Email, request.Password) is null
        ? Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid)
        : Results.Ok();
}).AllowAnonymous();

app.Run();