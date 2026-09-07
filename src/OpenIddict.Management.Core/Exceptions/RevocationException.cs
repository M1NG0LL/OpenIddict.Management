namespace OpenIddict.Management.Exceptions;

/// <summary>
/// Exception thrown when a revocation operation fails.
/// </summary>
public class RevocationException : ManagementException
{
    /// <summary>
    /// Initializes a new instance of <see cref="RevocationException"/>.
    /// </summary>
    public RevocationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RevocationException"/> with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public RevocationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RevocationException"/> with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RevocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
