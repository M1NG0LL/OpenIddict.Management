using System.Security.Claims;
using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Models;

/// <summary>
/// Encapsulates parameters for constructing a <see cref="ClaimsPrincipal"/> configured for OpenIddict token issuance.
/// </summary>
public sealed record TokenCreationParameters
{
    /// <summary>
    /// Gets the subject identifier (unique user ID) for the token.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the optional username or display name for the token.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the optional email address for the token.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the optional list of user roles.
    /// </summary>
    public IEnumerable<string>? Roles { get; init; }

    /// <summary>
    /// Gets the optional client identifier (ClientId) associated with the token request.
    /// Used to resolve default scopes configured for the application.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the optional list of granted scopes (e.g., "openid", "profile", "api_access").
    /// </summary>
    public IEnumerable<string>? Scopes { get; init; }

    /// <summary>
    /// Gets the optional default scopes to include if not already requested or configured.
    /// </summary>
    public IEnumerable<string>? DefaultScopes { get; init; }

    /// <summary>
    /// Gets the optional list of target resources/audiences for the token.
    /// </summary>
    public IEnumerable<string>? Resources { get; init; }

    /// <summary>
    /// Gets any additional individual <see cref="Claim"/> instances to attach to the principal.
    /// </summary>
    public IEnumerable<Claim>? Claims { get; init; }

    /// <summary>
    /// Gets an optional list of audiences to attach to the principal.
    /// </summary>
    public IEnumerable<string>? Audiences { get; init; }

    /// <summary>
    /// Gets an optional custom object or dictionary containing extra metadata/claims to embed in the token.
    /// </summary>
    public object? ExtraData { get; init; }

    /// <summary>
    /// Gets the destination token type(s) for claims. Defaults to <see cref="ClaimDestinationMode.AccessToken"/>.
    /// </summary>
    public ClaimDestinationMode DestinationMode { get; init; } = ClaimDestinationMode.AccessToken;

    /// <summary>
    /// Gets an optional custom delegate to determine the destination(s) for a given claim.
    /// When specified, this delegate takes precedence over <see cref="DestinationMode"/>.
    /// </summary>
    public Func<Claim, IEnumerable<string>?>? DestinationSelector { get; init; }

    /// <summary>
    /// Gets the authentication scheme name. If null, the default OpenIddict server scheme is used.
    /// </summary>
    public string? AuthenticationScheme { get; init; }
}
