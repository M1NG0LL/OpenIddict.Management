using System.Collections;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Services;

/// <summary>
/// Default implementation of <see cref="IOpenIddictTokenService"/> that builds OpenIddict-compliant <see cref="ClaimsPrincipal"/> instances
/// configured for Authorization Code Flow with PKCE and custom claim destinations.
/// </summary>
public class OpenIddictTokenService : IOpenIddictTokenService
{
    private const string DefaultAuthenticationScheme = "OpenIddict.Server.AspNetCore";
    private readonly IApplicationManagementService? _applicationService;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictTokenService"/> without an application management service.
    /// </summary>
    public OpenIddictTokenService() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictTokenService"/> with an optional application management service.
    /// </summary>
    /// <param name="applicationService">The application management service used to resolve client default scopes.</param>
    public OpenIddictTokenService(IApplicationManagementService? applicationService)
    {
        _applicationService = applicationService;
    }

    /// <inheritdoc/>
    public virtual async Task<ClaimsPrincipal> CreatePrincipalAsync(TokenCreationParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.Subject);

        var requestedScopes = MergeScopes(parameters.Scopes);

        var appDefaultScopes = !string.IsNullOrWhiteSpace(parameters.ClientId)
            ? await ResolveDefaultScopesAsync(parameters.ClientId, cancellationToken)
            : [];

        var defaultScopes = MergeScopes(parameters.DefaultScopes, appDefaultScopes);

        if (requestedScopes.Count == 0 && defaultScopes.Count == 0)
        {
            throw new InvalidOperationException(
                "Scopes cannot be empty. When scopes are empty, default scopes or a valid ClientId with configured default scopes must be provided.");
        }

        var mergedScopes = requestedScopes.Count > 0
            ? MergeScopes(requestedScopes, defaultScopes)
            : defaultScopes;

        return CreatePrincipalCore(parameters, mergedScopes);
    }

    /// <inheritdoc/>
    public virtual async Task<ClaimsPrincipal> CreatePrincipalAsync(LoginResult loginResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loginResult);

        if (!loginResult.Succeeded || string.IsNullOrWhiteSpace(loginResult.UserId))
        {
            throw new InvalidOperationException("Cannot construct a token principal from an unsuccessful login result or empty UserId.");
        }

        var parameters = new TokenCreationParameters
        {
            Subject = loginResult.UserId,
            Username = loginResult.Username,
            Email = loginResult.Email,
            Roles = loginResult.Roles,
            ClientId = loginResult.ClientId,
            Scopes = loginResult.Scopes,
            DefaultScopes = loginResult.DefaultScopes,
            Resources = loginResult.Resources,
            Claims = loginResult.Claims,
            Audiences = loginResult.Audiences,
            ExtraData = loginResult.ExtraData,
            DestinationMode = loginResult.DestinationMode,
            DestinationSelector = loginResult.DestinationSelector
        };

        return await CreatePrincipalAsync(parameters, cancellationToken);
    }

    /// <summary>
    /// Core method that constructs a <see cref="ClaimsPrincipal"/> from parameters and merged scopes.
    /// </summary>
    /// <param name="parameters">The token creation parameters.</param>
    /// <param name="mergedScopes">The normalized, trimmed, and deduplicated scopes.</param>
    /// <returns>A configured <see cref="ClaimsPrincipal"/>.</returns>
    protected virtual ClaimsPrincipal CreatePrincipalCore(TokenCreationParameters parameters, List<string> mergedScopes)
    {
        var scheme = parameters.AuthenticationScheme ?? DefaultAuthenticationScheme;
        var identity = new ClaimsIdentity(
            authenticationType: scheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // 1. Set Subject (sub)
        var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, parameters.Subject);
        subjectClaim.SetDestinations(ResolveDestinations(subjectClaim, parameters));
        identity.AddClaim(subjectClaim);

        // 2. Set Username / Name (name)
        if (!string.IsNullOrWhiteSpace(parameters.Username))
        {
            var nameClaim = new Claim(OpenIddictConstants.Claims.Name, parameters.Username);
            nameClaim.SetDestinations(ResolveDestinations(nameClaim, parameters));
            identity.AddClaim(nameClaim);
        }

        // 3. Set Email (email)
        if (!string.IsNullOrWhiteSpace(parameters.Email))
        {
            var emailClaim = new Claim(OpenIddictConstants.Claims.Email, parameters.Email);
            emailClaim.SetDestinations(ResolveDestinations(emailClaim, parameters));
            identity.AddClaim(emailClaim);
        }

        // 4. Set Roles (role)
        if (parameters.Roles is not null)
        {
            foreach (var role in parameters.Roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                var roleClaim = new Claim(OpenIddictConstants.Claims.Role, role);
                roleClaim.SetDestinations(ResolveDestinations(roleClaim, parameters));
                identity.AddClaim(roleClaim);
            }
        }

        // 5. Add custom individual claims
        if (parameters.Claims is not null)
        {
            foreach (var claim in parameters.Claims)
            {
                if (!claim.GetDestinations().Any())
                {
                    claim.SetDestinations(ResolveDestinations(claim, parameters));
                }
                identity.AddClaim(claim);
            }
        }

        // 6. Embed ExtraData object/dictionary
        if (parameters.ExtraData is not null)
        {
            AppendExtraDataClaims(identity, parameters.ExtraData, c => ResolveDestinations(c, parameters));
        }

        // 7. Set Scopes (trimmed and deduplicated, including default scopes)
        identity.SetScopes(mergedScopes);

        // 8. Set Resources
        if (parameters.Resources is not null)
        {
            identity.SetResources(parameters.Resources);
        }

        var principal = new ClaimsPrincipal(identity);

        if (parameters.Audiences is not null)
        {
            principal.SetAudiences(parameters.Audiences);
        }

        return principal;
    }


    /// <summary>
    /// Resolves default scopes for the specified client ID asynchronously.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes with the collection of default scopes configured for the client application.</returns>
    protected virtual async Task<IEnumerable<string>> ResolveDefaultScopesAsync(string? clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || _applicationService is null)
        {
            return [];
        }

        try
        {
            var result = await _applicationService.GetByClientIdAsync(clientId, cancellationToken);
            if (result.IsSuccess)
            {
                return result.Value.DefaultScopes;
            }
        }
        catch
        {
            // Graceful fallback if application not found or operation cancelled
        }

        return [];
    }

    /// <summary>
    /// Combines, normalizes, trims, and deduplicates scopes from multiple sources.
    /// Splits space-delimited entries, trims leading/trailing whitespace, and removes duplicate scopes (case-insensitive).
    /// </summary>
    /// <param name="scopeSources">One or more collections of scopes to merge.</param>
    /// <returns>A list of unique, trimmed scopes.</returns>
    protected virtual List<string> MergeScopes(params IEnumerable<string>?[] scopeSources)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in scopeSources)
        {
            if (source is null) continue;

            foreach (var entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;

                var parts = entry.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (seen.Add(part))
                    {
                        result.Add(part);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves the destination(s) for a specific claim using <see cref="TokenCreationParameters.DestinationSelector"/> if provided,
    /// or falling back to <see cref="ResolveDestinations(ClaimDestinationMode)"/>.
    /// </summary>
    /// <param name="claim">The claim for which to resolve destinations.</param>
    /// <param name="parameters">The token creation parameters.</param>
    /// <returns>A collection of destination strings (e.g. access_token, id_token).</returns>
    protected virtual IEnumerable<string> ResolveDestinations(Claim claim, TokenCreationParameters parameters)
    {
        var selector = parameters.DestinationSelector;
        if (selector is not null)
        {
            return selector(claim) ?? ResolveDestinations(parameters.DestinationMode);
        }

        return ResolveDestinations(parameters.DestinationMode);
    }

    /// <summary>
    /// Maps the <see cref="ClaimDestinationMode"/> enum into OpenIddict destination strings.
    /// </summary>
    /// <param name="mode">The destination mode.</param>
    /// <returns>A collection of destination strings (e.g. access_token, id_token).</returns>
    protected virtual List<string> ResolveDestinations(ClaimDestinationMode mode)
    {
        var destinations = new List<string>(2);

        if (mode.HasFlag(ClaimDestinationMode.AccessToken))
        {
            destinations.Add(OpenIddictConstants.Destinations.AccessToken);
        }

        if (mode.HasFlag(ClaimDestinationMode.IdentityToken))
        {
            destinations.Add(OpenIddictConstants.Destinations.IdentityToken);
        }

        // Fallback default to AccessToken if none matched
        if (destinations.Count == 0)
        {
            destinations.Add(OpenIddictConstants.Destinations.AccessToken);
        }

        return destinations;
    }

    /// <summary>
    /// Appends extra data properties from an object or dictionary as claims with the specified destinations.
    /// </summary>
    /// <param name="identity">The claims identity.</param>
    /// <param name="extraData">The extra data payload.</param>
    /// <param name="destinations">The claim destinations.</param>
    protected virtual void AppendExtraDataClaims(ClaimsIdentity identity, object extraData, List<string> destinations)
    {
        AppendExtraDataClaims(identity, extraData, _ => destinations);
    }

    /// <summary>
    /// Appends extra data properties from an object or dictionary as claims with destinations resolved for each claim.
    /// </summary>
    /// <param name="identity">The claims identity.</param>
    /// <param name="extraData">The extra data payload.</param>
    /// <param name="destinationResolver">The function that resolves destinations for each claim.</param>
    protected virtual void AppendExtraDataClaims(ClaimsIdentity identity, object extraData, Func<Claim, IEnumerable<string>?> destinationResolver)
    {
        switch (extraData)
        {
            case IDictionary<string, object?> dictObj:
                foreach (var (key, value) in dictObj)
                {
                    if (value is not null)
                    {
                        var claimValue = ValueToString(value);
                        var claim = new Claim(key, claimValue);
                        var dests = destinationResolver(claim);
                        if (dests is not null)
                        {
                            claim.SetDestinations(dests);
                        }
                        identity.AddClaim(claim);
                    }
                }
                break;

            case IDictionary<string, string> dictStr:
                foreach (var (key, value) in dictStr)
                {
                    if (value is not null)
                    {
                        var claim = new Claim(key, value);
                        var dests = destinationResolver(claim);
                        if (dests is not null)
                        {
                            claim.SetDestinations(dests);
                        }
                        identity.AddClaim(claim);
                    }
                }
                break;

            case IDictionary legacyDict:
                foreach (DictionaryEntry entry in legacyDict)
                {
                    if (entry.Key is not null && entry.Value is not null)
                    {
                        var claim = new Claim(entry.Key.ToString()!, ValueToString(entry.Value));
                        var dests = destinationResolver(claim);
                        if (dests is not null)
                        {
                            claim.SetDestinations(dests);
                        }
                        identity.AddClaim(claim);
                    }
                }
                break;

            default:
                var properties = extraData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                if (properties.Length > 0)
                {
                    foreach (var prop in properties)
                    {
                        if (prop.CanRead)
                        {
                            var val = prop.GetValue(extraData);
                            if (val is not null)
                            {
                                var claim = new Claim(prop.Name, ValueToString(val));
                                var dests = destinationResolver(claim);
                                if (dests is not null)
                                {
                                    claim.SetDestinations(dests);
                                }
                                identity.AddClaim(claim);
                            }
                        }
                    }
                }
                else
                {
                    // Primitive or non-property object
                    var rawClaim = new Claim("extra_data", ValueToString(extraData));
                    var dests = destinationResolver(rawClaim);
                    if (dests is not null)
                    {
                        rawClaim.SetDestinations(dests);
                    }
                    identity.AddClaim(rawClaim);
                }
                break;
        }
    }

    private static string ValueToString(object value)
    {
        return value switch
        {
            string str => str,
            bool b => b ? "true" : "false",
            int or long or double or decimal or float => value.ToString()!,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            Guid guid => guid.ToString(),
            _ => JsonSerializer.Serialize(value)
        };
    }
}
