using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Filters;

/// <summary>
/// Minimal API endpoint filter that validates request DTO objects and arguments before route handlers execute.
/// </summary>
public sealed class ValidationEndpointFilter : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            ValidateArgument(argument, errors);
        }

        if (errors.Count > 0)
        {
            var formattedErrors = errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray()
            );

            return HttpResults.BadRequest(ManagementError.ValidationFailed(
                "One or more validation failures occurred.",
                formattedErrors));
        }

        return await next(context);
    }

    private static void ValidateArgument(object argument, Dictionary<string, List<string>> errors)
    {
        // 1. DataAnnotations validation
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(argument);
        if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
        {
            foreach (var result in validationResults)
            {
                var member = result.MemberNames.FirstOrDefault() ?? "General";
                AddError(errors, member, result.ErrorMessage ?? "Validation error.");
            }
        }

        // 2. Specific domain DTO validations
        switch (argument)
        {
            case ApplicationCreateDto appCreate:
                ValidateApplicationCreate(appCreate, errors);
                break;

            case ApplicationUpdateDto appUpdate:
                ValidateApplicationUpdate(appUpdate, errors);
                break;

            case CreateScopeRequest scopeCreate:
                ValidateCreateScope(scopeCreate, errors);
                break;

            case UpdateScopeRequest scopeUpdate:
                ValidateUpdateScope(scopeUpdate, errors);
                break;

            case RevocationByUserRequest revUser:
                if (string.IsNullOrWhiteSpace(revUser.UserId))
                {
                    AddError(errors, nameof(revUser.UserId), "User ID is required.");
                }
                break;

            case RevocationByClientRequest revClient:
                if (string.IsNullOrWhiteSpace(revClient.ClientId))
                {
                    AddError(errors, nameof(revClient.ClientId), "Client ID is required.");
                }
                break;

            case RevocationBySessionRequest revSession:
                if (string.IsNullOrWhiteSpace(revSession.UserId) && string.IsNullOrWhiteSpace(revSession.AuthorizationId))
                {
                    AddError(errors, "Revocation", "At least one of UserId or AuthorizationId must be specified.");
                }
                break;

            case PagedRequest paged:
                if (paged.PageIndex < 1)
                {
                    AddError(errors, nameof(paged.PageIndex), "PageIndex must be greater than or equal to 1.");
                }
                if (paged.PageSize < 1 || paged.PageSize > 1000)
                {
                    AddError(errors, nameof(paged.PageSize), "PageSize must be between 1 and 1000.");
                }
                break;
        }
    }

    private static void ValidateApplicationCreate(ApplicationCreateDto dto, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(dto.ClientId))
        {
            AddError(errors, nameof(dto.ClientId), "Client ID is required.");
        }
        else if (dto.ClientId.Any(char.IsWhiteSpace))
        {
            AddError(errors, nameof(dto.ClientId), "Client ID cannot contain whitespace.");
        }
        else if (dto.ClientId.Length > 100)
        {
            AddError(errors, nameof(dto.ClientId), "Client ID cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            AddError(errors, nameof(dto.DisplayName), "Display Name is required.");
        }
        else if (dto.DisplayName.Length > 200)
        {
            AddError(errors, nameof(dto.DisplayName), "Display Name cannot exceed 200 characters.");
        }

        ValidateUris(dto.RedirectUris, nameof(dto.RedirectUris), errors, "Redirect URI");
        ValidateUris(dto.PostLogoutRedirectUris, nameof(dto.PostLogoutRedirectUris), errors, "Post-logout redirect URI");

        if (!string.IsNullOrWhiteSpace(dto.LogoUri) && !IsValidAbsoluteUri(dto.LogoUri))
        {
            AddError(errors, nameof(dto.LogoUri), $"Logo URI '{dto.LogoUri}' is not a valid absolute URI.");
        }
    }

    private static void ValidateApplicationUpdate(ApplicationUpdateDto dto, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            AddError(errors, nameof(dto.DisplayName), "Display Name is required.");
        }
        else if (dto.DisplayName.Length > 200)
        {
            AddError(errors, nameof(dto.DisplayName), "Display Name cannot exceed 200 characters.");
        }

        ValidateUris(dto.RedirectUris, nameof(dto.RedirectUris), errors, "Redirect URI");
        ValidateUris(dto.PostLogoutRedirectUris, nameof(dto.PostLogoutRedirectUris), errors, "Post-logout redirect URI");

        if (!string.IsNullOrWhiteSpace(dto.LogoUri) && !IsValidAbsoluteUri(dto.LogoUri))
        {
            AddError(errors, nameof(dto.LogoUri), $"Logo URI '{dto.LogoUri}' is not a valid absolute URI.");
        }
    }

    private static void ValidateCreateScope(CreateScopeRequest req, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            AddError(errors, nameof(req.Name), "Scope Name is required.");
        }
        else if (req.Name.Any(char.IsWhiteSpace))
        {
            AddError(errors, nameof(req.Name), "Scope Name cannot contain whitespace.");
        }
        else if (req.Name.Length > 100)
        {
            AddError(errors, nameof(req.Name), "Scope Name cannot exceed 100 characters.");
        }

        if (req.DisplayName is { Length: > 200 })
        {
            AddError(errors, nameof(req.DisplayName), "Display Name cannot exceed 200 characters.");
        }

        if (req.Resources is { Count: > 0 })
        {
            foreach (var res in req.Resources)
            {
                if (string.IsNullOrWhiteSpace(res) || res.Any(char.IsWhiteSpace))
                {
                    AddError(errors, nameof(req.Resources), $"Resource identifier '{res}' cannot be empty or contain whitespace.");
                }
            }
        }
    }

    private static void ValidateUpdateScope(UpdateScopeRequest req, Dictionary<string, List<string>> errors)
    {
        if (req.DisplayName is { Length: > 200 })
        {
            AddError(errors, nameof(req.DisplayName), "Display Name cannot exceed 200 characters.");
        }

        if (req.Resources is { Count: > 0 })
        {
            foreach (var res in req.Resources)
            {
                if (string.IsNullOrWhiteSpace(res) || res.Any(char.IsWhiteSpace))
                {
                    AddError(errors, nameof(req.Resources), $"Resource identifier '{res}' cannot be empty or contain whitespace.");
                }
            }
        }
    }

    private static void ValidateUris(IEnumerable<string>? uris, string propertyName, Dictionary<string, List<string>> errors, string label)
    {
        if (uris is null) return;

        foreach (var uri in uris)
        {
            if (string.IsNullOrWhiteSpace(uri) || !IsValidAbsoluteUri(uri))
            {
                AddError(errors, propertyName, $"{label} '{uri}' is not a valid absolute URI (e.g. 'https://localhost:5001/callback').");
            }
        }
    }

    private static bool IsValidAbsoluteUri(string uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
            && parsed.IsWellFormedOriginalString()
            && !string.IsNullOrWhiteSpace(parsed.Scheme);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string propertyName, string message)
    {
        if (!errors.TryGetValue(propertyName, out var list))
        {
            list = [];
            errors[propertyName] = list;
        }
        list.Add(message);
    }
}
