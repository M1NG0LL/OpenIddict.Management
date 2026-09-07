using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace OpenIddict.Management.Validation;

/// <summary>
/// Validates that OpenIddict Server options are configured for Authorization Code Flow with PKCE.
/// </summary>
public sealed class OpenIddictAuthorizationCodePkceValidator : IValidateOptions<OpenIddictServerOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, OpenIddictServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // 1. Validate Authorization Code Flow is enabled
        if (!options.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode))
        {
            failures.Add("OpenIddict must be configured to allow the Authorization Code Flow (e.g., options.AllowAuthorizationCodeFlow()).");
        }

        // 2. Validate PKCE (Proof Key for Code Exchange) is required/enabled
        if (!options.RequireProofKeyForCodeExchange)
        {
            failures.Add("OpenIddict must be configured to require Proof Key for Code Exchange (PKCE) (e.g., options.RequireProofKeyForCodeExchange()).");
        }

        // 3. Validate Authorization Endpoint is configured
        if (options.AuthorizationEndpointUris.Count == 0)
        {
            failures.Add("OpenIddict must configure an authorization endpoint URI (e.g., options.SetAuthorizationEndpointUris(\"/connect/authorize\")).");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}
