using System.Security.Claims;
using System.Text.Encodings.Web;
using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FridayDeploy.Web.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyService apiKeyService)
    : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.Fail("Missing X-Api-Key header.");
        }

        var rawKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return AuthenticateResult.Fail("Missing X-Api-Key header.");
        }

        var application = await apiKeyService.ValidateAndTouchAsync(rawKey, Context.RequestAborted);
        if (application is null)
        {
            return AuthenticateResult.Fail("Invalid or disabled API key.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, application.Id.ToString()),
            new Claim("ApplicationName", application.Name)
        };
        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), ApiKeyAuthenticationDefaults.Scheme);
        return AuthenticateResult.Success(ticket);
    }
}
