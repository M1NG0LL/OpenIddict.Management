namespace OpenIddict.Management.Dashboard.Helpers;

/// <summary>
/// Provides catalog of standard OpenIddict permissions categorized for UI selection.
/// </summary>
public static class OpenIddictPermissionCatalog
{
    /// <summary>
    /// Represents a selectable permission item.
    /// </summary>
    public sealed record PermissionItem(string Value, string DisplayName, string Description);

    /// <summary>
    /// Represents a categorized group of permissions.
    /// </summary>
    public sealed record PermissionGroup(string Category, IReadOnlyList<PermissionItem> Items);

    /// <summary>
    /// Gets all standard categorized OpenIddict permission groups.
    /// </summary>
    public static readonly IReadOnlyList<PermissionGroup> Groups =
    [
        new("Endpoints",
        [
            new("ept:authorization", "Authorization Endpoint", "Allows the client to access the authorization endpoint (/connect/authorize)."),
            new("ept:token", "Token Endpoint", "Allows the client to request tokens from the token endpoint (/connect/token)."),
            new("ept:logout", "End Session / Logout Endpoint", "Allows the client to initiate user sign-out (/connect/logout)."),
            new("ept:revocation", "Revocation Endpoint", "Allows the client to revoke access or refresh tokens (/connect/revocation)."),
            new("ept:introspection", "Introspection Endpoint", "Allows querying token active status (/connect/introspect)."),
            new("ept:userinfo", "UserInfo Endpoint", "Allows the client to retrieve user claims (/connect/userinfo)."),
            new("ept:device", "Device Authorization", "Allows device authorization flow (/connect/device).")
        ]),
        new("Grant Types",
        [
            new("gt:authorization_code", "Authorization Code", "Standard interactive login flow for web apps and mobile/desktop apps."),
            new("gt:client_credentials", "Client Credentials", "Direct machine-to-machine authentication between services."),
            new("gt:refresh_token", "Refresh Token", "Allows refreshing expired access tokens without re-prompting the user."),
            new("gt:implicit", "Implicit Flow", "Legacy flow returning tokens directly from authorization endpoint."),
            new("gt:password", "Resource Owner Password", "Direct username/password exchange (legacy/migration only)."),
            new("gt:urn:ietf:params:oauth:grant-type:device_code", "Device Code", "Allows limited-input devices (TVs, CLI tools) to authorize.")
        ]),
        new("Response Types",
        [
            new("rst:code", "Code (rst:code)", "Returns authorization code (standard for Authorization Code Flow)."),
            new("rst:token", "Token (rst:token)", "Returns access token directly from authorization endpoint."),
            new("rst:id_token", "ID Token (rst:id_token)", "Returns OpenID Connect ID token directly."),
            new("rst:code id_token", "Code + ID Token", "Hybrid flow returning both authorization code and ID token.")
        ]),
        new("Standard Scopes",
        [
            new("scp:openid", "OpenID (scp:openid)", "Required for OpenID Connect identity tokens."),
            new("scp:profile", "Profile (scp:profile)", "Grants access to default user profile information."),
            new("scp:email", "Email (scp:email)", "Grants access to user email address and verification status."),
            new("scp:phone", "Phone (scp:phone)", "Grants access to user phone number."),
            new("scp:address", "Address (scp:address)", "Grants access to user postal address."),
            new("scp:roles", "Roles (scp:roles)", "Grants access to assigned user security roles."),
            new("scp:offline_access", "Offline Access (scp:offline_access)", "Requests refresh token issuance for long-term access.")
        ])
    ];

    /// <summary>
    /// Represents a selectable scope item.
    /// </summary>
    public sealed record ScopeItem(string Name, string DisplayName, string Description);

    /// <summary>
    /// Gets standard OpenID Connect / OAuth 2.0 scopes.
    /// </summary>
    public static readonly IReadOnlyList<ScopeItem> StandardScopes =
    [
        new("openid", "OpenID", "Required for OpenID Connect identity tokens."),
        new("profile", "Profile", "Grants access to default user profile information."),
        new("email", "Email", "Grants access to user email address and verification status."),
        new("phone", "Phone", "Grants access to user phone number."),
        new("address", "Address", "Grants access to user postal address."),
        new("roles", "Roles", "Grants access to assigned user security roles."),
        new("offline_access", "Offline Access", "Requests refresh token issuance for long-term access.")
    ];
}
