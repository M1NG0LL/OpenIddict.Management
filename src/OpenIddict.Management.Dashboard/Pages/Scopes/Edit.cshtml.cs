using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;

namespace OpenIddict.Management.Dashboard.Pages.Scopes;

/// <summary>
/// Page model for scope creation and editing.
/// </summary>
public class EditModel(IScopeManagementService scopeService) : PageModel
{
    /// <summary>Gets or sets scope ID (null for create mode).</summary>
    [BindProperty]
    public string? Id { get; set; }

    /// <summary>Gets or sets scope name.</summary>
    [BindProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets display name.</summary>
    [BindProperty]
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets description.</summary>
    [BindProperty]
    public string? Description { get; set; }

    /// <summary>Gets or sets raw resources text.</summary>
    [BindProperty]
    public string? ResourcesText { get; set; }

    /// <summary>Gets or sets error message if operation fails.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Handles GET request for editing or creating a scope.</summary>
    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return Page();
        }

        var result = await scopeService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null) return NotFound();

        var scope = result.Value;
        Id = scope.Id;
        Name = scope.Name;
        DisplayName = scope.DisplayName;
        Description = scope.Description;
        ResourcesText = scope.Resources != null ? string.Join("\n", scope.Resources) : string.Empty;

        return Page();
    }

    /// <summary>Handles POST request for saving a scope.</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Id))
        {
            if (!string.IsNullOrWhiteSpace(Name) && Name.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError(nameof(Name), "Scope name cannot contain whitespace characters.");
            }
            else if (Name.Length > 100)
            {
                ModelState.AddModelError(nameof(Name), "Scope name cannot exceed 100 characters.");
            }
        }

        if (DisplayName is { Length: > 200 })
        {
            ModelState.AddModelError(nameof(DisplayName), "Display Name cannot exceed 200 characters.");
        }

        var resources = (ResourcesText ?? string.Empty)
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        foreach (var res in resources)
        {
            if (res.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError(nameof(ResourcesText), $"Resource identifier '{res}' cannot contain whitespace.");
            }
        }

        if (!ModelState.IsValid) return Page();

        if (string.IsNullOrEmpty(Id))
        {
            var createResult = await scopeService.CreateAsync(Name, DisplayName, Description, resources, cancellationToken);
            if (!createResult.IsSuccess)
            {
                ErrorMessage = createResult.Error?.Description ?? "Failed to create scope.";
                if (createResult.Error?.Details != null)
                {
                    foreach (var (key, errors) in createResult.Error.Details)
                    {
                        foreach (var err in errors)
                        {
                            ModelState.AddModelError(key, err);
                        }
                    }
                }
                return Page();
            }
        }
        else
        {
            var updateResult = await scopeService.UpdateAsync(Id, DisplayName, Description, resources, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                ErrorMessage = updateResult.Error?.Description ?? "Failed to update scope.";
                if (updateResult.Error?.Details != null)
                {
                    foreach (var (key, errors) in updateResult.Error.Details)
                    {
                        foreach (var err in errors)
                        {
                            ModelState.AddModelError(key, err);
                        }
                    }
                }
                return Page();
            }
        }

        return RedirectToPage("Index");
    }
}
