namespace OpenIddict.Management.Options;

/// <summary>
/// Configuration options for the automatic token cleanup background job.
/// </summary>
public sealed class TokenCleanupOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the token cleanup background job is active. Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum amount of expired and revoked tokens to prune per execution cycle. Defaults to 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the execution frequency for the cleanup background job. Defaults to 1 hour.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets a value indicating whether revoked tokens should be cleared in addition to expired tokens. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeRevoked { get; set; } = true;
}
