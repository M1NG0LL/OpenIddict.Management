using System.ComponentModel.DataAnnotations;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Request payload for dynamically updating the background token cleanup job parameters.
/// </summary>
public sealed class UpdateCleanupJobSettingsRequest
{
    /// <summary>
    /// Gets or sets the maximum amount of tokens to prune per execution cycle.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "BatchSize must be greater than zero.")]
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the execution frequency in minutes.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "IntervalMinutes must be greater than zero.")]
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether revoked tokens should be cleared in addition to expired tokens.
    /// </summary>
    public bool? IncludeRevoked { get; set; }
}
