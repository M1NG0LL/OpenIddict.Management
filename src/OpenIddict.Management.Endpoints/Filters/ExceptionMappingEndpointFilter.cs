using Microsoft.AspNetCore.Http;
using OpenIddict.Management.Exceptions;
using OpenIddict.Management.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Filters;

/// <summary>
/// Minimal API endpoint filter that catches unhandled domain exceptions and maps them to HTTP responses.
/// </summary>
public sealed class ExceptionMappingEndpointFilter : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (EntityNotFoundException ex)
        {
            return HttpResults.NotFound(ManagementError.EntityNotFound(ex.EntityName ?? "Entity", ex.EntityId ?? "Unknown"));
        }
        catch (DuplicateEntityException ex)
        {
            return HttpResults.Conflict(ManagementError.DuplicateEntity(ex.EntityName ?? "Entity", ex.PropertyName ?? "Property", "Duplicate"));
        }
        catch (ValidationException ex)
        {
            return HttpResults.BadRequest(ManagementError.ValidationFailed(ex.Message, ex.Errors));
        }
        catch (OpenIddict.Abstractions.OpenIddictExceptions.ValidationException ex)
        {
            return HttpResults.BadRequest(ManagementError.ValidationFailed(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return HttpResults.BadRequest(ManagementError.ValidationFailed(ex.Message));
        }
        catch (ManagementException ex)
        {
            return HttpResults.BadRequest(ManagementError.Custom("ManagementError", ex.Message));
        }
    }
}
