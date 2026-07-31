using EquipmentRental.Api.Security;

using Microsoft.Extensions.Options;

namespace EquipmentRental.Api.Tests.Security;

public sealed class ApiKeyCredentialValidatorTests
{
    private const string ConfiguredKey =
        "a-valid-development-key-123456";

    [Fact]
    public void Validate_WithConfiguredKey_ReturnsIdentityAndScopes()
    {
        var validator = CreateValidator();

        var credential = validator.Validate(ConfiguredKey);

        Assert.NotNull(credential);
        Assert.Equal("operator", credential.Name);
        Assert.Contains(ApiSecurity.ReadPolicy, credential.Scopes);
        Assert.Contains(ApiSecurity.WritePolicy, credential.Scopes);
    }

    [Fact]
    public void Validate_WithUnknownKey_ReturnsNull()
    {
        var validator = CreateValidator();

        var credential = validator.Validate(
            "a-different-invalid-key-123");

        Assert.Null(credential);
    }

    private static ApiKeyCredentialValidator CreateValidator() =>
        new(new StaticOptionsMonitor<ApiKeySecurityOptions>(
            new ApiKeySecurityOptions
            {
                ApiKeys =
                [
                    new ApiKeyCredential
                    {
                        Name = "operator",
                        Key = ConfiguredKey,
                        Scopes =
                        [
                            ApiSecurity.ReadPolicy,
                            ApiSecurity.WritePolicy,
                        ],
                    },
                ],
            }));

    private sealed class StaticOptionsMonitor<T>(T value)
        : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(
            Action<T, string?> listener) =>
            null;
    }
}
