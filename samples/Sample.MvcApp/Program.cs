using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dashboard.Extensions;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Server.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.;Database=OpenIddictManagementMvc;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False";

// Register HttpClient with development-friendly certificate validation for local loopback calls
builder.Services.AddHttpClient("LocalAuthClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddAntiforgery();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<SampleDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Configure OpenIddict Server & Validation
builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetIntrospectionEndpointUris("/connect/introspect")
               .SetRevocationEndpointUris("/connect/revocation");

        options.AllowAuthorizationCodeFlow()
               .AllowClientCredentialsFlow()
               .AllowPasswordFlow
               .AllowRefreshTokenFlow()
               .RequireProofKeyForCodeExchange();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess,
            "api_access"
        );

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();

        options.SetAccessTokenLifetime(TimeSpan.FromDays(365));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(365));
        options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(30));

        // Use reference access and refresh tokens so tokens are stored in the database
        // and visible in the OpenIddict Management dashboard with their 365-day lifetimes.
        options.UseReferenceAccessTokens();
        options.UseReferenceRefreshTokens();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Configure OpenIddict Management Suite, Custom Provider, and Embedded Admin Dashboard
builder.Services.AddOpenIddictManagement<SampleDbContext>()
    .AddAuthenticationProvider<SampleUserAuthProvider, SampleLoginRequest>()
    .AddDashboard(options =>
    {
        options.PathPrefix = "/admin/identity";
        options.ExitUrl = "/";
        options.ExitButtonText = "Home";
        options.DashboardTitle = "Enterprise Identity Admin";
        options.RequireAuthorization = false; // Set to true in production with custom policy
        options.AvailableTags = ["Social login", "Dark mode"];
    });

var app = builder.Build();

// Ensure database created and seed initial demonstration data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await context.Database.EnsureCreatedAsync();

    var appService = scope.ServiceProvider.GetRequiredService<IApplicationManagementService>();
    var existingApp = await appService.GetByClientIdAsync("sample-mvc-client");
    var redirectUris = new[]
    {
        "https://localhost:7001/signin-oidc",
        "https://oauth.pstmn.io/v1/callback",
        "http://localhost:5000/callback",
        "https://localhost:5001/callback",
        "http://localhost:5084/callback",
        "https://localhost:51378/callback",
        "http://localhost:51379/callback",
        "https://localhost:7198/callback"
    };

    var permissions = new[]
    {
        OpenIddictConstants.Permissions.Endpoints.Authorization,
        OpenIddictConstants.Permissions.Endpoints.Token,
        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
        OpenIddictConstants.Permissions.ResponseTypes.Code,
        OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
        OpenIddictConstants.Permissions.Scopes.Email,
        OpenIddictConstants.Permissions.Scopes.Profile,
        OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
        OpenIddictConstants.Permissions.Prefixes.Scope + "api_access",
    };

    if (!existingApp.IsSuccess)
    {
        await appService.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "sample-mvc-client",
            ClientSecret = "sample-secret-key-12345",
            DisplayName = "Sample MVC Client Application",
            Environment = ApplicationEnvironment.Development,
            RedirectUris = redirectUris,
            Permissions = permissions,
            DefaultScopes = ["openid", "profile", "email", "api_access"]
        });
    }
    else
    {
        await appService.UpdateAsync(existingApp.Value.Id, new ApplicationUpdateDto
        {
            DisplayName = "Sample MVC Client Application",
            Environment = ApplicationEnvironment.Development,
            RedirectUris = redirectUris,
            Permissions = permissions,
            DefaultScopes = ["openid", "profile", "email", "api_access"]
        });
        await appService.UpdateClientSecretAsync(existingApp.Value.Id, "sample-secret-key-12345");
    }

    var scopeService = scope.ServiceProvider.GetRequiredService<IScopeManagementService>();
    var existingScope = await scopeService.GetByNameAsync("api_access");
    if (!existingScope.IsSuccess)
    {
        await scopeService.CreateAsync("api_access", "API Access Scope", "Grants full API access resources", ["resource_server_1"]);
    }
}

app.UseStaticFiles();
app.UseRouting();

// Antiforgery & Authentication / Authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Mount OpenIddict Admin Dashboard (single unified call)
app.MapOpenIddictManagementDashboard();

// -----------------------------------------------------------------------------------------
// Custom Login View & Endpoints (Authorization Code Flow with PKCE)
// -----------------------------------------------------------------------------------------

// GET /login: Renders the custom login form and preserves the OpenID returnUrl
app.MapGet("/login", (HttpContext context) =>
{
    var returnUrl = context.Request.Query["returnUrl"].ToString();
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        returnUrl = context.Request.Query["ReturnUrl"].ToString();
    }
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        returnUrl = "/";
    }

    var escapedUrl = Uri.EscapeDataString(returnUrl);
    var htmlEncodedUrl = System.Net.WebUtility.HtmlEncode(returnUrl);

    return Results.Content($@"<!DOCTYPE html>
<html>
<head>
    <title>Sign In — Enterprise Identity</title>
    <style>
        body {{ font-family: system-ui, -apple-system, BlinkMacSystemFont, sans-serif; background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
        .login-card {{ background: #1e293b; padding: 2.5rem; border-radius: 14px; width: 100%; max-width: 420px; border: 1px solid #334155; box-shadow: 0 12px 30px rgba(0,0,0,0.5); }}
        h2 {{ margin-top: 0; margin-bottom: 0.5rem; font-size: 1.6rem; }}
        p.subtitle {{ color: #94a3b8; font-size: 0.9rem; margin-bottom: 1.5rem; }}
        .form-group {{ margin-bottom: 1.25rem; }}
        label {{ display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.4rem; color: #cbd5e1; }}
        input {{ width: 100%; box-sizing: border-box; background: #0f172a; border: 1px solid #475569; border-radius: 6px; padding: 0.65rem 0.75rem; color: #f8fafc; font-size: 0.95rem; }}
        input:focus {{ outline: none; border-color: #6366f1; ring: 2px solid #6366f1; }}
        button {{ width: 100%; background: #6366f1; color: white; border: none; padding: 0.75rem; border-radius: 6px; font-weight: 600; cursor: pointer; font-size: 1rem; transition: background 0.15s; }}
        button:hover {{ background: #4f46e5; }}
        .badge {{ background: #334155; color: #38bdf8; font-size: 0.75rem; padding: 0.25rem 0.6rem; border-radius: 4px; display: inline-block; margin-bottom: 1rem; font-weight: 600; }}
        .info-box {{ background: #0f172a; border: 1px solid #334155; padding: 0.75rem; border-radius: 6px; font-size: 0.8rem; color: #94a3b8; margin-top: 1.25rem; line-height: 1.4; }}
    </style>
</head>
<body>
    <div class='login-card'>
        <span class='badge'>Auth Code + PKCE</span>
        <h2>Sign In</h2>
        <p class='subtitle'>Demo credentials: <strong>admin</strong> / <strong>password123</strong></p>
        <form method='post' action='/login?returnUrl={escapedUrl}'>
            <input type='hidden' name='returnUrl' value='{htmlEncodedUrl}' />
            <div class='form-group'>
                <label for='username'>Username or Email</label>
                <input type='text' id='username' name='Username' value='admin' required />
            </div>
            <div class='form-group'>
                <label for='password'>Password</label>
                <input type='password' id='password' name='Password' value='password123' required />
            </div>
            <div class='form-group'>
                <label for='tenantCode'>Tenant Code (Custom context property)</label>
                <input type='text' id='tenantCode' name='TenantCode' value='TENANT-CORP-42' />
            </div>
            <button type='submit'>Sign In &rarr;</button>
        </form>
        <div class='info-box'>
            Authenticates with <code>IUserAuthenticationProvider</code> and returns to OpenIddict authorization endpoint.
        </div>
    </div>
</body>
</html>", "text/html");
});

// POST /login: Processes credentials using the custom validation engine & signs into the Cookie session
app.MapPost("/login", async (
    [FromForm] SampleLoginRequest request,
    HttpContext httpContext,
    IOpenIddictLoginEngine<SampleLoginRequest> loginEngine) =>
{
    // 1. Run developer's custom validation engine
    var loginResult = await loginEngine.AuthenticateAsync(request);

    if (!loginResult.Succeeded)
    {
        return Results.BadRequest(new { error = loginResult.ErrorHeader, message = loginResult.ErrorMessage });
    }

    // 2. Sign into Cookie session for the user
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, loginResult.UserId!),
        new(ClaimTypes.Name, loginResult.Username!)
    };
    if (loginResult.Email is not null) claims.Add(new(ClaimTypes.Email, loginResult.Email));
    foreach (var role in loginResult.Roles) claims.Add(new(ClaimTypes.Role, role));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    // 3. Redirect back to authorization endpoint (returnUrl) or root, preserving all PKCE query params
    var targetUrl = httpContext.Request.Query["returnUrl"].ToString();
    if (string.IsNullOrWhiteSpace(targetUrl))
    {
        targetUrl = httpContext.Request.Query["ReturnUrl"].ToString();
    }
    if (string.IsNullOrWhiteSpace(targetUrl) && httpContext.Request.HasFormContentType)
    {
        targetUrl = httpContext.Request.Form["returnUrl"].ToString();
    }
    if (string.IsNullOrWhiteSpace(targetUrl))
    {
        targetUrl = "/";
    }

    return Results.Redirect(targetUrl);
})
.DisableAntiforgery();

// GET/POST /connect/authorize: OpenIddict Authorization Endpoint
app.MapMethods("/connect/authorize", ["GET", "POST"], async (
    HttpContext httpContext,
    IOpenIddictTokenService tokenService) =>
{
    var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!authResult.Succeeded || authResult.Principal is null)
    {
        // Challenge unauthenticated user to the /login view with returnUrl preserved
        return Results.Challenge(
            properties: new AuthenticationProperties
            {
                RedirectUri = httpContext.Request.Path + httpContext.Request.QueryString
            },
            authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }

    var user = authResult.Principal;
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "user-001";
    var username = user.FindFirst(ClaimTypes.Name)?.Value ?? "admin";
    var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "admin@corp.example";
    var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    var openIddictRequest = httpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

    var requestedScopes = openIddictRequest.GetScopes();

    // Build OpenIddict Principal using IOpenIddictTokenService with ExtraData and AccessToken destination
    var principal = await tokenService.CreatePrincipalAsync(new TokenCreationParameters
    {
        Subject = userId,
        Username = username,
        Email = email,
        Roles = roles,
        Scopes = requestedScopes,
        Audiences = ["sample-mvc-client"],
        ClientId = "sample-mvc-client",
        ExtraData = new
        {
            TenantId = "TENANT-CORP-42",
            SubscriptionTier = "Enterprise",
            Department = "Engineering",
            IsAdmin = true
        },
        DestinationMode = ClaimDestinationMode.AccessTokenAndIdentityToken  
    });

    // Sign into OpenIddict server to issue the authorization code (PKCE)
    return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

// POST /connect/token: OpenIddict Token Endpoint (exchanges authorization code + PKCE for tokens)
app.MapPost("/connect/token", async (HttpContext httpContext) =>
{
    var request = httpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

    if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
    {
        var authResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is invalid or has already been redeemed."
                }));
        }

        return Results.SignIn(authResult.Principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    else if (request.IsClientCredentialsGrantType())
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, (string?)request.ClientId ?? "client"));
        identity.SetScopes(request.GetScopes());

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    throw new InvalidOperationException("The specified grant type is not supported.");
});

// -----------------------------------------------------------------------------------------
// GET /callback: MVC Callback Page displaying Token and Serialized Claims
// -----------------------------------------------------------------------------------------
app.MapGet("/callback", async (
    [FromQuery] string? code,
    [FromQuery] string? error,
    [FromQuery] string? error_description,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory) =>
{
    if (!string.IsNullOrEmpty(error))
    {
        return Results.Content($@"<!DOCTYPE html>
<html>
<head>
    <title>OAuth Error</title>
    <style>
        body {{ font-family: system-ui, sans-serif; background: #0f172a; color: #f8fafc; padding: 3rem; display: flex; justify-content: center; }}
        .card {{ background: #1e293b; padding: 2rem; border-radius: 12px; max-width: 600px; border: 1px solid #ef4444; }}
        h2 {{ color: #ef4444; margin-top: 0; }}
        pre {{ background: #0f172a; padding: 1rem; border-radius: 6px; overflow-x: auto; }}
        a {{ color: #6366f1; }}
    </style>
</head>
<body>
    <div class='card'>
        <h2>Authorization Error: {error}</h2>
        <p>{error_description}</p>
        <a href='/'>&larr; Back to Home</a>
    </div>
</body>
</html>", "text/html");
    }

    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest("Missing authorization code.");
    }

    // Retrieve active PKCE code_verifier (from cookie or RFC 7636 fallback)
    var codeVerifier = httpContext.Request.Cookies["pkce_verifier"];
    if (string.IsNullOrWhiteSpace(codeVerifier))
    {
        codeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    }

    // Clean up one-time verifier cookie
    httpContext.Response.Cookies.Delete("pkce_verifier");

    // Perform token exchange with /connect/token using PKCE code_verifier
    var client = httpClientFactory.CreateClient("LocalAuthClient");
    var currentUri = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
    client.BaseAddress = currentUri;

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "sample-mvc-client",
            ["client_secret"] = "sample-secret-key-12345",
            ["code"] = code,
            ["redirect_uri"] = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/callback",
            ["code_verifier"] = codeVerifier
        })
    };

    var response = await client.SendAsync(tokenRequest);
    var responseContent = await response.Content.ReadAsStringAsync();

    string accessToken = "N/A";
    string tokenType = "Bearer";
    string scope = "openid profile email offline_access api_access";
    string expiresIn = "3600";
    string decodedClaimsJson = "{}";
    bool isSuccess = response.IsSuccessStatusCode;

    if (isSuccess)
    {
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;
        if (root.TryGetProperty("access_token", out var at)) accessToken = at.GetString() ?? "N/A";
        if (root.TryGetProperty("token_type", out var tt)) tokenType = tt.GetString() ?? "Bearer";
        if (root.TryGetProperty("scope", out var sc)) scope = sc.GetString() ?? "N/A";
        if (root.TryGetProperty("expires_in", out var ex)) expiresIn = ex.GetInt64().ToString();

        // Decode JWT payload claims
        decodedClaimsJson = DecodeJwtPayload(accessToken);
    }
    else
    {
        decodedClaimsJson = responseContent;
    }

    var statusClass = isSuccess ? "status-success" : "status-error";
    var statusText = isSuccess ? "HTTP 200 OK" : $"HTTP {(int)response.StatusCode} {response.StatusCode}";
    var headerTitle = isSuccess ? "OAuth 2.0 PKCE Authorization Succeeded" : "OAuth 2.0 PKCE Token Exchange Failed";

    return Results.Content($@"<!DOCTYPE html>
<html>
<head>
    <title>Token Result — OpenIddict Management</title>
    <style>
        body {{ font-family: system-ui, -apple-system, BlinkMacSystemFont, sans-serif; background: #0f172a; color: #f8fafc; margin: 0; padding: 2.5rem 1rem; display: flex; justify-content: center; }}
        .container {{ width: 100%; max-width: 860px; }}
        .header {{ background: #1e293b; padding: 1.5rem 2rem; border-radius: 12px 12px 0 0; border: 1px solid #334155; border-bottom: none; display: flex; align-items: center; justify-content: space-between; }}
        .header h2 {{ margin: 0; font-size: 1.4rem; color: {(isSuccess ? "#38bdf8" : "#ef4444")}; display: flex; align-items: center; gap: 0.5rem; }}
        .status-pill {{ font-size: 0.8rem; font-weight: 700; padding: 0.3rem 0.75rem; border-radius: 9999px; text-transform: uppercase; }}
        .status-success {{ background: #10b981; color: #0f172a; }}
        .status-error {{ background: #ef4444; color: #ffffff; }}
        .content {{ background: #1e293b; padding: 2rem; border-radius: 0 0 12px 12px; border: 1px solid #334155; }}
        .section-title {{ font-size: 1rem; font-weight: 600; color: #cbd5e1; margin-top: 1.5rem; margin-bottom: 0.5rem; }}
        .section-title:first-child {{ margin-top: 0; }}
        .code-block {{ background: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 1rem; overflow-x: auto; font-family: 'Consolas', 'Fira Code', monospace; font-size: 0.88rem; line-height: 1.5; color: #e2e8f0; word-break: break-all; white-space: pre-wrap; }}
        .json-block {{ color: #a5b4fc; }}
        .meta-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }}
        .meta-card {{ background: #0f172a; border: 1px solid #334155; padding: 1rem; border-radius: 8px; }}
        .meta-card label {{ font-size: 0.75rem; text-transform: uppercase; color: #64748b; font-weight: 600; display: block; margin-bottom: 0.25rem; }}
        .meta-card value {{ font-size: 1rem; font-weight: 600; color: #f1f5f9; display: block; }}
        .actions {{ margin-top: 2rem; display: flex; gap: 1rem; }}
        a.btn {{ background: #6366f1; color: white; padding: 0.65rem 1.25rem; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block; font-size: 0.95rem; transition: background 0.15s; }}
        a.btn:hover {{ background: #4f46e5; }}
        a.btn-secondary {{ background: #334155; color: #f8fafc; }}
        a.btn-secondary:hover {{ background: #475569; }}
        .notice {{ background: #1e1b4b; border: 1px solid #6366f1; padding: 1rem; border-radius: 8px; margin-bottom: 1.5rem; font-size: 0.9rem; color: #c7d2fe; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{headerTitle}</h2>
            <span class='status-pill {statusClass}'>{statusText}</span>
        </div>
        <div class='content'>
            {(!isSuccess ? @"<div class='notice'><strong>Note:</strong> In OAuth 2.0, authorization codes are single-use. If you refreshed this page, the code was already redeemed. Click <strong>Return Home</strong> to initiate a fresh flow.</div>" : "")}

            <div class='meta-grid'>
                <div class='meta-card'>
                    <label>Token Type</label>
                    <value>{tokenType}</value>
                </div>
                <div class='meta-card'>
                    <label>Expires In</label>
                    <value>{expiresIn} seconds</value>
                </div>
                <div class='meta-card'>
                    <label>Granted Scopes</label>
                    <value>{scope}</value>
                </div>
            </div>

            <div class='section-title'>Decoded Token Payload & Claims (with Custom ExtraData)</div>
            <div class='code-block json-block'>{decodedClaimsJson}</div>

            <div class='section-title'>Raw Access Token (JWT)</div>
            <div class='code-block'>{accessToken}</div>

            <div class='actions'>
                <a class='btn' href='/'>&larr; Return Home</a>
                <a class='btn btn-secondary' href='/admin/identity'>Go to Admin Dashboard</a>
            </div>
        </div>
    </div>
</body>
</html>", "text/html");
});

// Home page
app.MapGet("/", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    
    // Generate fresh cryptographic PKCE parameters for each session
    var (verifier, challenge) = GeneratePkce();
    
    // Persist verifier in a secure, short-lived HttpOnly cookie for the callback endpoint
    context.Response.Cookies.Append("pkce_verifier", verifier, new CookieOptions
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromMinutes(10)
    });

    var authUrl = $"/connect/authorize?client_id=sample-mvc-client&response_type=code&redirect_uri={baseUrl}/callback&code_challenge={challenge}&code_challenge_method=S256";

    return Results.Content($@"<!DOCTYPE html>
<html>
<head>
    <title>Sample MVC App — OpenIddict Management</title>
    <style>
        body {{ font-family: system-ui, -apple-system, BlinkMacSystemFont, sans-serif; background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
        .card {{ background: #1e293b; padding: 2.5rem; border-radius: 14px; max-width: 580px; text-align: center; border: 1px solid #334155; box-shadow: 0 12px 30px rgba(0,0,0,0.5); }}
        h1 {{ margin-bottom: 0.5rem; font-size: 1.8rem; }}
        p {{ color: #94a3b8; margin-bottom: 2rem; line-height: 1.5; font-size: 0.95rem; }}
        .buttons {{ display: flex; gap: 1rem; justify-content: center; flex-wrap: wrap; }}
        a.btn {{ background: #6366f1; color: white; padding: 0.75rem 1.5rem; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block; transition: background 0.15s; }}
        a.btn:hover {{ background: #4f46e5; }}
        a.btn-secondary {{ background: #334155; color: #f8fafc; }}
        a.btn-secondary:hover {{ background: #475569; }}
    </style>
</head>
<body>
    <div class='card'>
        <h1>OpenIddict Management</h1>
        <p>Demonstrating custom Login Engine & View integration with OpenIddict PKCE Authorization Code flow, ExtraData token serialization, and Admin Dashboard.</p>
        <div class='buttons'>
            <a class='btn' href='{authUrl}'>Initiate PKCE Login Flow &rarr;</a>
            <a class='btn btn-secondary' href='/admin/identity'>Admin Dashboard</a>
        </div>
    </div>
</body>
</html>", "text/html");
});

app.Run();

// -----------------------------------------------------------------------------------------
// Helper Functions
// -----------------------------------------------------------------------------------------

static (string Verifier, string Challenge) GeneratePkce()
{
    var bytes = RandomNumberGenerator.GetBytes(32);
    var verifier = Base64UrlEncode(bytes);
    var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
    var challenge = Base64UrlEncode(challengeBytes);
    return (verifier, challenge);
}

static string Base64UrlEncode(byte[] bytes)
{
    return Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

static string DecodeJwtPayload(string jwt)
{
    try
    {
        var parts = jwt.Split('.');
        if (parts.Length >= 2)
        {
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                             .Replace('_', '/').Replace('-', '+');
            var jsonBytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(jsonBytes);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
    }
    catch
    {
        // Fallback to raw text if token format is encrypted or non-JWT
    }
    return jwt;
}

// -----------------------------------------------------------------------------------------
// Custom Login Models & Provider Implementation
// -----------------------------------------------------------------------------------------

/// <summary>
/// Custom developer login context carrying arbitrary form inputs.
/// </summary>
public sealed record SampleLoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string? TenantCode { get; init; }
}

/// <summary>
/// Developer-provided validation engine validating credentials and attaching ExtraData to the token.
/// </summary>
public sealed class SampleUserAuthProvider : IUserAuthenticationProvider<SampleLoginRequest>
{
    public Task<LoginResult> AuthenticateAsync(SampleLoginRequest context, CancellationToken cancellationToken = default)
    {
        // Demonstration credential verification
        if (context.Username == "admin" && context.Password == "password123")
        {
            // Custom extra data payload to be embedded as token claims
            var extraData = new
            {
                TenantId = context.TenantCode ?? "DEFAULT-TENANT",
                SubscriptionTier = "Enterprise",
                Department = "Engineering",
                IsAdmin = true
            };

            return Task.FromResult(LoginResult.Success(
                userId: "user-001",
                username: "admin",
                roles: ["Administrator", "Developer"],
                clientId: "sample-mvc-client",
                extraData: extraData,
                destinationMode: ClaimDestinationMode.AccessToken,
                email: "admin@corp.example"
            ));
        }

        return Task.FromResult(LoginResult.InvalidCredentials("Invalid username or password."));
    }
}

public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<ManagementApplication<Guid>> Applications => Set<ManagementApplication<Guid>>();
    public DbSet<ManagementAuthorization<Guid>> Authorizations => Set<ManagementAuthorization<Guid>>();
    public DbSet<ManagementScope<Guid>> Scopes => Set<ManagementScope<Guid>>();
    public DbSet<ManagementToken<Guid>> Tokens => Set<ManagementToken<Guid>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddictManagement();
    }
}
