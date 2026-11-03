using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class CustomValidatorJwtBearerOptions(TimeProvider timeProvider) : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        options.TokenValidationParameters.LifetimeValidator = (notBefore, expires, token, parameters) =>
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

            if (notBefore.HasValue && notBefore.Value > DateTimeUtil.Add(utcNow, parameters.ClockSkew))
            {
                return false;
            }

            if (expires.HasValue && expires.Value < DateTimeUtil.Add(utcNow, parameters.ClockSkew.Negate()))
            {
                return false;
            }

            return true;
        };
    }
}