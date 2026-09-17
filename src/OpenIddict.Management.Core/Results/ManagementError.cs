namespace OpenIddict.Management.Results;

/// <summary>
/// Represents structured error details for a failed operation containing an error code and description.
/// </summary>
public sealed record ManagementError
{
    private readonly string? _code;

    /// <summary>
    /// Gets the error code (e.g., "EntityNotFound", "DuplicateEntity", "ValidationFailed").
    /// </summary>
    public string Code
    {
        get => _code ?? Header ?? string.Empty;
        init => _code = value;
    }

    /// <summary>
    /// Gets the error header or code (alias for <see cref="Code"/>).
    /// </summary>
    public string Header
    {
        get => _code ?? string.Empty;
        init => _code = value;
    }

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
        Code = "EntityNotFound",
        Description = $"Entity of type '{entityName}' with ID '{id}' was not found."
    };

    /// <summary>
    /// Creates a pre-defined DuplicateEntity error.
    /// </summary>
    public static ManagementError DuplicateEntity(string entityName, string propertyName, object value) => new()
    {
        Code = "DuplicateEntity",
        Description = $"An entity of type '{entityName}' with {propertyName} '{value}' already exists."
    };

    /// <summary>
    /// Creates a pre-defined ValidationFailed error.
    /// </summary>
    public static ManagementError ValidationFailed(string description, IReadOnlyDictionary<string, string[]>? details = null) => new()
    {
        Code = "ValidationFailed",
        Description = description,
        Details = details
    };

    /// <summary>
    /// Creates a custom error with the specified code and description.
    /// </summary>
    public static ManagementError Custom(string code, string description) => new()
    {
        Code = code,
        Description = description
    };
}
