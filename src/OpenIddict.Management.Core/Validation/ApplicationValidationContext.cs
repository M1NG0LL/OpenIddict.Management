using System.Security.Claims;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Validation;

/// <summary>
/// Provides contextual information for validating an OpenIddict application.
/// </summary>
public sealed record ApplicationValidationContext
{
    /// <summary>
    /// Gets the client identifier of the application being validated.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the resolved <see cref="ManagedApplication"/> domain entity, if available.
    /// </summary>
    public ManagedApplication? Application { get; init; }

    /// <summary>
    /// Gets the endpoint where validation is being performed (e.g. "Token", "Authorization", "Device", "Validation").
    /// </summary>
    public string? EndpointType { get; init; }

    /// <summary>
    /// Gets the OAuth2 grant type of the request, if applicable (e.g. "authorization_code", "client_credentials").
    /// </summary>
    public string? GrantType { get; init; }

    /// <summary>
    /// Gets the principal associated with the request or token, if available.
    /// </summary>
    public ClaimsPrincipal? Principal { get; init; }

    /// <summary>
    /// Gets the cancellation token for the validation operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}
