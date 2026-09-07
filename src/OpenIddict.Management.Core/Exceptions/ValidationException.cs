namespace OpenIddict.Management.Exceptions;

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>
public class ValidationException : ManagementException
{
    /// <summary>
    /// Gets the validation errors dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/>.
    /// </summary>
    public ValidationException()
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> specifying validation errors.
    /// </summary>
    /// <param name="errors">The validation errors dictionary.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}
