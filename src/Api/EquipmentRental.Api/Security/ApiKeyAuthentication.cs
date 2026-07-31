using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EquipmentRental.Api.Security;

public static class ApiSecurity
{
    public const string Scheme = "ApiKey";
    public const string ReadPolicy = "equipment-rental.read";
    public const string WritePolicy = "equipment-rental.write";

    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiKeySecurityOptions>()
            .Bind(configuration.GetSection(ApiKeySecurityOptions.SectionName))
            .Validate(
                options => options.ApiKeys.Count > 0,
                "At least one API key must be configured.")
            .Validate(
                options => options.ApiKeys.All(key =>
                    !string.IsNullOrWhiteSpace(key.Name)
                    && key.Key.Length >= 24
                    && key.Scopes.Count > 0),
                "API keys need a name, at least 24 characters, and scopes.")
            .ValidateOnStart();

        services.AddSingleton<ApiKeyCredentialValidator>();
        services.AddAuthentication(Scheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                Scheme,
                _ => { });
        services.AddAuthorizationBuilder()
            .AddPolicy(
                ReadPolicy,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("scope", ReadPolicy))
            .AddPolicy(
                WritePolicy,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("scope", WritePolicy));

        return services;
    }
}

public sealed class ApiKeySecurityOptions
{
    public const string SectionName = "Security";

    public List<ApiKeyCredential> ApiKeys { get; init; } = [];
}

public sealed class ApiKeyCredential
{
    public string Name { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public List<string> Scopes { get; init; } = [];
}

public sealed class ApiKeyCredentialValidator(
    IOptionsMonitor<ApiKeySecurityOptions> options)
{
    public ApiKeyCredential? Validate(string presentedKey)
    {
        var presentedHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(presentedKey));

        foreach (var credential in options.CurrentValue.ApiKeys)
        {
            var configuredHash = SHA256.HashData(
                Encoding.UTF8.GetBytes(credential.Key));

            if (CryptographicOperations.FixedTimeEquals(
                    presentedHash,
                    configuredHash))
            {
                return credential;
            }
        }

        return null;
    }
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyCredentialValidator validator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    private const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var presentedKey = values.ToString();
        var credential = validator.Validate(presentedKey);
        if (credential is null)
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, credential.Name),
            new(ClaimTypes.Name, credential.Name),
        };
        claims.AddRange(credential.Scopes.Select(
            scope => new Claim("scope", scope)));
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, ApiSecurity.Scheme));

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(
                    principal,
                    ApiSecurity.Scheme)));
    }
}
