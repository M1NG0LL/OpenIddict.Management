namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for creating a new scope.
/// </summary>
public sealed record CreateScopeRequest
{
    /// <summary>
    /// Gets the unique scope name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional display name of the scope.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the optional description of the scope.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets optional resource names associated with the scope.
    /// </summary>
    public IReadOnlyList<string>? Resources { get; init; }
}
