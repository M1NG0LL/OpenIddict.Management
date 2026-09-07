namespace OpenIddict.Management.Exceptions;

/// <summary>
/// Exception thrown when attempting to create or update an entity that causes a duplicate conflict.
/// </summary>
public class DuplicateEntityException : ManagementException
{
    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Gets the conflicting property name or value.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateEntityException"/>.
    /// </summary>
    public DuplicateEntityException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateEntityException"/> with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DuplicateEntityException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateEntityException"/> specifying entity details.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="propertyName">The property causing conflict.</param>
    /// <param name="value">The conflicting value.</param>
    public DuplicateEntityException(string entityName, string propertyName, object value)
        : base($"An entity of type '{entityName}' with {propertyName} '{value}' already exists.")
    {
        EntityName = entityName;
        PropertyName = propertyName;
    }
}
