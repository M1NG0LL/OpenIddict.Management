using Microsoft.AspNetCore.Http;
using OpenIddict.Management.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Extensions;

/// <summary>
/// Extension methods for converting domain <see cref="Result"/> instances into ASP.NET Core <see cref="IResult"/> HTTP responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result"/> into an appropriate <see cref="IResult"/> HTTP response.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return HttpResults.NoContent();
        }

        return MapErrorToHttpResult(result.Error);
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into an appropriate <see cref="IResult"/> HTTP response.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return onSuccess is not null ? onSuccess(result.Value) : HttpResults.Ok(result.Value);
        }

        return MapErrorToHttpResult(result.Error);
    }

    private static IResult MapErrorToHttpResult(ManagementError? error)
    {
        if (error is null)
        {
            return HttpResults.BadRequest();
        }

        return error.Header switch
        {
            "EntityNotFound" => HttpResults.NotFound(error),
            "DuplicateEntity" => HttpResults.Conflict(error),
            "ValidationError" or "ValidationFailed" => HttpResults.BadRequest(error),
            _ => HttpResults.BadRequest(error)
        };
    }
}
