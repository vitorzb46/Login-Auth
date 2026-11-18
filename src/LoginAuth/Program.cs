using LoginAuth.DependencyInjection;
using LoginAuth.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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
app.MyEndpoints();
app.Run();