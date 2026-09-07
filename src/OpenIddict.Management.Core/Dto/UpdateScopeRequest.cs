namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for updating an existing scope.
/// </summary>
public sealed record UpdateScopeRequest
{
    /// <summary>
    /// Gets the updated display name of the scope.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the updated description of the scope.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets updated resource names associated with the scope.
    /// </summary>
    public IReadOnlyList<string>? Resources { get; init; }
}
