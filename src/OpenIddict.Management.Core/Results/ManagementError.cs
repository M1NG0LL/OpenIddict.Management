namespace OpenIddict.Management.Results;

/// <summary>
/// Represents structured error details for a failed operation containing an error header and description.
/// </summary>
public sealed record ManagementError
{
    /// <summary>
    /// Gets the error header or code (e.g., "EntityNotFound", "DuplicateEntity", "ValidationFailed").
    /// </summary>
    public required string Header { get; init; }

    /// <summary>
    /// Gets the detailed description of the error.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets optional field-level validation errors.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    /// <summary>
    /// Creates a pre-defined EntityNotFound error.
    /// </summary>
    public static ManagementError EntityNotFound(string entityName, object id) => new()
    {
        Header = "EntityNotFound",
        Description = $"Entity of type '{entityName}' with ID '{id}' was not found."
    };

    /// <summary>
    /// Creates a pre-defined DuplicateEntity error.
    /// </summary>
    public static ManagementError DuplicateEntity(string entityName, string propertyName, object value) => new()
    {
        Header = "DuplicateEntity",
        Description = $"An entity of type '{entityName}' with {propertyName} '{value}' already exists."
    };

    /// <summary>
    /// Creates a pre-defined ValidationFailed error.
    /// </summary>
    public static ManagementError ValidationFailed(string description, IReadOnlyDictionary<string, string[]>? details = null) => new()
    {
        Header = "ValidationFailed",
        Description = description,
        Details = details
    };

    /// <summary>
    /// Creates a custom error with the specified header and description.
    /// </summary>
    public static ManagementError Custom(string header, string description) => new()
    {
        Header = header,
        Description = description
    };
}
