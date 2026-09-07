namespace OpenIddict.Management.Exceptions;

/// <summary>
/// Exception thrown when a requested entity cannot be found.
/// </summary>
public class EntityNotFoundException : ManagementException
{
    /// <summary>
    /// Gets the type name of the entity that was not found.
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public object? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException"/>.
    /// </summary>
    public EntityNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException"/> with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException"/> specifying entity details.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    public EntityNotFoundException(string entityName, object entityId)
        : base($"Entity of type '{entityName}' with ID '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
