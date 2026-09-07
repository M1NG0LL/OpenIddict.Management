using System.Security.Claims;
using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Models;

/// <summary>
/// Represents the result of a user authentication attempt and provides the metadata required for token issuance.
/// </summary>
public sealed record LoginResult
{
    /// <summary>
    /// Gets the login result type.
    /// </summary>
    public required LoginResultType Type { get; init; }

    /// <summary>
    /// Gets the unique identifier (Subject) of the authenticated user if login succeeded.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the username of the authenticated user if login succeeded.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the optional email address of the authenticated user.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the roles assigned to the authenticated user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Gets the scopes granted to the authenticated session.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Gets the resources / audiences targeted by the token.
    /// </summary>
    public IReadOnlyList<string> Resources { get; init; } = [];

    /// <summary>
    /// Gets custom claims to include in the generated principal.
    /// </summary>
    public IReadOnlyList<Claim> Claims { get; init; } = [];

    /// <summary>
    /// Gets an optional custom object or dictionary containing extra metadata/claims to embed in the token.
    /// </summary>
    public object? ExtraData { get; init; }

    /// <summary>
    /// Gets custom key-value properties associated with the authentication session.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomProperties { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the destination token type(s) for claims. Defaults to <see cref="ClaimDestinationMode.AccessToken"/>.
    /// </summary>
    public ClaimDestinationMode DestinationMode { get; init; } = ClaimDestinationMode.AccessToken;

    /// <summary>
    /// Gets the error header or code if login failed.
    /// </summary>
    public string? ErrorHeader { get; init; }

    /// <summary>
    /// Gets the detailed error message if login failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the lockout expiration date if the user account is locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; init; }

    /// <summary>
    /// Gets a value indicating whether the login succeeded.
    /// </summary>
    public bool Succeeded => Type == LoginResultType.Success;

    /// <summary>
    /// Gets a value indicating whether the account requires two-factor authentication.
    /// </summary>
    public bool RequiresTwoFactor => Type == LoginResultType.RequiresTwoFactor;

    /// <summary>
    /// Gets a value indicating whether the account is currently locked out.
    /// </summary>
    public bool IsLockedOut => Type == LoginResultType.LockedOut;

    /// <summary>
    /// Creates a successful login result with basic user identifiers.
    /// </summary>
    public static LoginResult Success(string userId, string username) => new()
    {
        Type = LoginResultType.Success,
        UserId = userId,
        Username = username
    };

    /// <summary>
    /// Creates a successful login result with complete token parameters, roles, scopes, and custom extra data.
    /// </summary>
    public static LoginResult Success(
        string userId,
        string username,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? scopes = null,
        object? extraData = null,
        ClaimDestinationMode destinationMode = ClaimDestinationMode.AccessToken,
        string? email = null,
        IEnumerable<string>? resources = null,
        IEnumerable<Claim>? claims = null) => new()
    {
        Type = LoginResultType.Success,
        UserId = userId,
        Username = username,
        Email = email,
        Roles = roles?.ToList() ?? [],
        Scopes = scopes?.ToList() ?? [],
        Resources = resources?.ToList() ?? [],
        Claims = claims?.ToList() ?? [],
        ExtraData = extraData,
        DestinationMode = destinationMode
    };

    /// <summary>
    /// Creates an invalid credentials login result.
    /// </summary>
    public static LoginResult InvalidCredentials(string message = "Invalid username or password.") => new()
    {
        Type = LoginResultType.InvalidCredentials,
        ErrorHeader = "InvalidCredentials",
        ErrorMessage = message
    };

    /// <summary>
    /// Creates a locked out login result.
    /// </summary>
    public static LoginResult LockedOut(DateTimeOffset? lockoutEnd = null, string message = "Account is locked out.") => new()
    {
        Type = LoginResultType.LockedOut,
        ErrorHeader = "AccountLockedOut",
        ErrorMessage = message,
        LockoutEnd = lockoutEnd
    };

    /// <summary>
    /// Creates a requires two-factor authentication login result.
    /// </summary>
    public static LoginResult TwoFactorRequired(string? userId = null, string? username = null) => new()
    {
        Type = LoginResultType.RequiresTwoFactor,
        UserId = userId,
        Username = username
    };

    /// <summary>
    /// Creates a generic failed login result.
    /// </summary>
    public static LoginResult Failed(string header, string message) => new()
    {
        Type = LoginResultType.Failed,
        ErrorHeader = header,
        ErrorMessage = message
    };
}
