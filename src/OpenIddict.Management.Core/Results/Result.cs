namespace OpenIddict.Management.Results;

/// <summary>
/// Represents the result of an operation indicating success or failure with error details.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of <see cref="Result"/>.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="error">The error details if failed.</param>
    protected Result(bool isSuccess, ManagementError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }
        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error details if the operation failed; otherwise null.
    /// </summary>
    public ManagementError? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(ManagementError error) => new(false, error);

    /// <summary>
    /// Creates a failed result with the specified header and description.
    /// </summary>
    public static Result Failure(string header, string description) => new(false, ManagementError.Custom(header, description));

    /// <summary>
    /// Creates a successful typed result with the specified value.
    /// </summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed typed result with the specified error.
    /// </summary>
    public static Result<T> Failure<T>(ManagementError error) => Result<T>.Failure(error);

    /// <summary>
    /// Creates a failed typed result with the specified header and description.
    /// </summary>
    public static Result<T> Failure<T>(string header, string description) => Result<T>.Failure(header, description);
}

/// <summary>
/// Represents the result of an operation containing a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, ManagementError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value if the operation succeeded; otherwise throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access {nameof(Value)} on a failed result. Error: [{Error?.Header}] {Error?.Description}");

    /// <summary>
    /// Gets the value if successful, or default if failed.
    /// </summary>
    public T? ValueOrDefault => _value;

    /// <summary>
    /// Creates a successful typed result with the specified value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, value, null);
    }

    /// <summary>
    /// Creates a failed typed result with the specified error.
    /// </summary>
    public static new Result<T> Failure(ManagementError error) => new(false, default, error);

    /// <summary>
    /// Creates a failed typed result with the specified header and description.
    /// </summary>
    public static new Result<T> Failure(string header, string description) => new(false, default, ManagementError.Custom(header, description));

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to a successful <see cref="Result{T}"/>.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts a <see cref="ManagementError"/> to a failed <see cref="Result{T}"/>.
    /// </summary>
    public static implicit operator Result<T>(ManagementError error) => Failure(error);
}
