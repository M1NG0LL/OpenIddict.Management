using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace OpenIddict.Management.Validation;

/// <summary>
/// OpenIddict event handler that intercepts authentication and token requests across any flow,
/// as well as token validation, to verify that the client application satisfies status,
/// environment, and custom validation requirements.
/// </summary>
public class OpenIddictApplicationValidationHandler :
    IOpenIddictValidationHandler<OpenIddictValidationEvents.ProcessAuthenticationContext>,
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>,
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>,
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateDeviceRequestContext>
{
    /// <summary>
    /// The default order in which this handler executes within the OpenIddict event pipeline.
    /// </summary>
    public const int DefaultOrder = 50_000;

    private readonly IOpenIddictApplicationValidator _validator;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictApplicationValidationHandler"/>.
    /// </summary>
    /// <param name="validator">The application validator.</param>
    public OpenIddictApplicationValidationHandler(IOpenIddictApplicationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    /// <inheritdoc/>
    public virtual async ValueTask HandleAsync(OpenIddictValidationEvents.ProcessAuthenticationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.IsRejected)
        {
            return;
        }

        var clientId = ResolveClientId(context);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var validationContext = new ApplicationValidationContext
        {
            ClientId = clientId,
            EndpointType = "Validation",
            Principal = context.AccessTokenPrincipal,
            CancellationToken = context.CancellationToken
        };

        var result = await _validator.ValidateClientIdAsync(clientId, validationContext);
        if (!result.Succeeded)
        {
            context.Reject(result.Error, result.ErrorDescription, result.ErrorUri);
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.IsRejected)
        {
            return;
        }

        var clientId = context.ClientId ?? context.Request?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var validationContext = new ApplicationValidationContext
        {
            ClientId = clientId,
            EndpointType = "Token",
            GrantType = context.Request?.GrantType,
            Principal = context.Principal,
            CancellationToken = context.CancellationToken
        };

        var result = await _validator.ValidateClientIdAsync(clientId, validationContext);
        if (!result.Succeeded)
        {
            context.Reject(result.Error, result.ErrorDescription, result.ErrorUri);
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.ValidateAuthorizationRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.IsRejected)
        {
            return;
        }

        var clientId = context.ClientId ?? context.Request?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var validationContext = new ApplicationValidationContext
        {
            ClientId = clientId,
            EndpointType = "Authorization",
            Principal = context.IdentityTokenHintPrincipal,
            CancellationToken = context.CancellationToken
        };

        var result = await _validator.ValidateClientIdAsync(clientId, validationContext);
        if (!result.Succeeded)
        {
            context.Reject(result.Error, result.ErrorDescription, result.ErrorUri);
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.ValidateDeviceRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.IsRejected)
        {
            return;
        }

        var clientId = context.ClientId ?? context.Request?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var validationContext = new ApplicationValidationContext
        {
            ClientId = clientId,
            EndpointType = "Device",
            CancellationToken = context.CancellationToken
        };

        var result = await _validator.ValidateClientIdAsync(clientId, validationContext);
        if (!result.Succeeded)
        {
            context.Reject(result.Error, result.ErrorDescription, result.ErrorUri);
        }
    }

    private static string? ResolveClientId(OpenIddictValidationEvents.ProcessAuthenticationContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Request?.ClientId))
        {
            return context.Request.ClientId;
        }

        var principal = context.AccessTokenPrincipal;
        if (principal is null)
        {
            return null;
        }

        return principal.FindFirst(OpenIddictConstants.Claims.ClientId)?.Value
            ?? principal.FindFirst(OpenIddictConstants.Claims.AuthorizedParty)?.Value
            ?? principal.FindFirst("client_id")?.Value
            ?? principal.FindFirst("azp")?.Value;
    }
}
