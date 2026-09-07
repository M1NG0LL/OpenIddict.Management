using System.Collections;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
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

    /// <inheritdoc/>
    public virtual ClaimsPrincipal CreatePrincipal(TokenCreationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.Subject);

        var scheme = parameters.AuthenticationScheme ?? DefaultAuthenticationScheme;
        var identity = new ClaimsIdentity(
            authenticationType: scheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        var destinations = ResolveDestinations(parameters.DestinationMode);

        // 1. Set Subject (sub)
        var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, parameters.Subject);
        subjectClaim.SetDestinations(destinations);
        identity.AddClaim(subjectClaim);

        // 2. Set Username / Name (name)
        if (!string.IsNullOrWhiteSpace(parameters.Username))
        {
            var nameClaim = new Claim(OpenIddictConstants.Claims.Name, parameters.Username);
            nameClaim.SetDestinations(destinations);
            identity.AddClaim(nameClaim);
        }

        // 3. Set Email (email)
        if (!string.IsNullOrWhiteSpace(parameters.Email))
        {
            var emailClaim = new Claim(OpenIddictConstants.Claims.Email, parameters.Email);
            emailClaim.SetDestinations(destinations);
            identity.AddClaim(emailClaim);
        }

        // 4. Set Roles (role)
        if (parameters.Roles is not null)
        {
            foreach (var role in parameters.Roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                var roleClaim = new Claim(OpenIddictConstants.Claims.Role, role);
                roleClaim.SetDestinations(destinations);
                identity.AddClaim(roleClaim);
            }
        }

        // 5. Add custom individual claims
        if (parameters.Claims is not null)
        {
            foreach (var claim in parameters.Claims)
            {
                // If the claim doesn't already have destinations assigned, assign resolved destinations
                if (!claim.GetDestinations().Any())
                {
                    claim.SetDestinations(destinations);
                }
                identity.AddClaim(claim);
            }
        }

        // 6. Embed ExtraData object/dictionary
        if (parameters.ExtraData is not null)
        {
            AppendExtraDataClaims(identity, parameters.ExtraData, destinations);
        }

        // 7. Set Scopes
        if (parameters.Scopes is not null)
        {
            identity.SetScopes(parameters.Scopes);
        }

        // 8. Set Resources
        if (parameters.Resources is not null)
        {
            identity.SetResources(parameters.Resources);
        }

        return new ClaimsPrincipal(identity);
    }

    /// <inheritdoc/>
    public virtual ClaimsPrincipal CreatePrincipal(LoginResult loginResult)
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
            Scopes = loginResult.Scopes,
            Resources = loginResult.Resources,
            Claims = loginResult.Claims,
            ExtraData = loginResult.ExtraData,
            DestinationMode = loginResult.DestinationMode
        };

        return CreatePrincipal(parameters);
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
        switch (extraData)
        {
            case IDictionary<string, object?> dictObj:
                foreach (var (key, value) in dictObj)
                {
                    if (value is not null)
                    {
                        var claimValue = ValueToString(value);
                        var claim = new Claim(key, claimValue);
                        claim.SetDestinations(destinations);
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
                        claim.SetDestinations(destinations);
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
                        claim.SetDestinations(destinations);
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
                                claim.SetDestinations(destinations);
                                identity.AddClaim(claim);
                            }
                        }
                    }
                }
                else
                {
                    // Primitive or non-property object
                    var rawClaim = new Claim("extra_data", ValueToString(extraData));
                    rawClaim.SetDestinations(destinations);
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
