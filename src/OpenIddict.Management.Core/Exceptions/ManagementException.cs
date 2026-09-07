namespace OpenIddict.Management.Exceptions;

/// <summary>
/// Base exception for all domain and operational errors within OpenIddict.Management.
/// </summary>
public class ManagementException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ManagementException"/>.
    /// </summary>
    public ManagementException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ManagementException"/> with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ManagementException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ManagementException"/> with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManagementException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
