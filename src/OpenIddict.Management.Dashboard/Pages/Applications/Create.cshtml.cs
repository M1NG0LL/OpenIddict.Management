using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Dashboard.Pages.Applications;

/// <summary>
/// Page model for application registration.
/// </summary>
public class CreateModel(
    IApplicationManagementService applicationService,
    IScopeManagementService scopeService,
    IOptions<DashboardOptions> dashboardOptions) : PageModel
{
    /// <summary>Gets or sets client ID.</summary>
    [BindProperty]
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets client secret.</summary>
    [BindProperty]
    public string? ClientSecret { get; set; }

    /// <summary>Gets or sets display name.</summary>
    [BindProperty]
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets target environment.</summary>
    [BindProperty]
    public ApplicationEnvironment Environment { get; set; } = ApplicationEnvironment.Development;

    /// <summary>Gets or sets description.</summary>
    [BindProperty]
    public string? Description { get; set; }

    /// <summary>Gets or sets raw redirect URIs text.</summary>
    [BindProperty]
    public string? RedirectUrisText { get; set; }

    /// <summary>Gets or sets selected tags from predefined configuration.</summary>
    [BindProperty]
    public List<string> SelectedTags { get; set; } = [];

    /// <summary>Gets or sets optional custom tags (comma or line separated).</summary>
    [BindProperty]
    public string? CustomTagsText { get; set; }

    /// <summary>Gets predefined available tags from dashboard options.</summary>
    public IReadOnlyList<string> AvailableTags => dashboardOptions.Value.AvailableTags;

    /// <summary>Gets custom scopes defined in the system.</summary>
    public IReadOnlyList<ManagedScope> CustomScopes { get; set; } = [];

    /// <summary>Gets or sets selected permissions from checkboxes.</summary>
    [BindProperty]
    public List<string> SelectedPermissions { get; set; } = [];

    /// <summary>Gets or sets optional custom permissions (comma or line separated).</summary>
    [BindProperty]
    public string? CustomPermissionsText { get; set; }

    /// <summary>Gets or sets error message if creation fails.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Handles GET request.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Default standard selections for standard authorization code app
        SelectedPermissions =
        [
            "ept:authorization",
            "ept:token",
            "ept:logout",
            "gt:authorization_code",
            "gt:refresh_token",
            "rst:code",
            "scp:openid",
            "scp:profile",
            "scp:email"
        ];

        await LoadCustomScopesAsync(cancellationToken);
    }

    /// <summary>Handles POST request for creating an application.</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(ClientId) && ClientId.Any(char.IsWhiteSpace))
        {
            ModelState.AddModelError(nameof(ClientId), "Client ID cannot contain whitespace characters.");
        }

        var redirectUris = (RedirectUrisText ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        foreach (var uri in redirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.IsWellFormedOriginalString() || string.IsNullOrWhiteSpace(parsed.Scheme))
            {
                ModelState.AddModelError(nameof(RedirectUrisText), $"Redirect URI '{uri}' is not a valid absolute URI (e.g. 'https://localhost:5001/signin-oidc').");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadCustomScopesAsync(cancellationToken);
            return Page();
        }

        var permissionsSet = new HashSet<string>(SelectedPermissions, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(CustomPermissionsText))
        {
            var customPerms = CustomPermissionsText
                .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var perm in customPerms)
            {
                permissionsSet.Add(perm);
            }
        }

        var tagsSet = new HashSet<string>(SelectedTags, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(CustomTagsText))
        {
            var customTags = CustomTagsText
                .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tag in customTags)
            {
                tagsSet.Add(tag);
            }
        }

        var dto = new ApplicationCreateDto
        {
            ClientId = ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(ClientSecret) ? null : ClientSecret,
            DisplayName = DisplayName,
            Environment = Environment,
            Description = Description,
            RedirectUris = redirectUris,
            Permissions = permissionsSet.ToList(),
            Tags = tagsSet.ToList()
        };

        var result = await applicationService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error?.Description ?? "Failed to create application.";
            if (result.Error?.Details != null)
            {
                foreach (var (key, errors) in result.Error.Details)
                {
                    foreach (var err in errors)
                    {
                        ModelState.AddModelError(key, err);
                    }
                }
            }
            await LoadCustomScopesAsync(cancellationToken);
            return Page();
        }

        var prefix = dashboardOptions.Value.PathPrefix.TrimEnd('/');
        return Redirect($"{prefix}/Applications");
    }

    private async Task LoadCustomScopesAsync(CancellationToken cancellationToken)
    {
        var scopeResult = await scopeService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken);
        if (scopeResult.IsSuccess && scopeResult.Value is not null)
        {
            CustomScopes = scopeResult.Value.Items;
        }
    }
}
