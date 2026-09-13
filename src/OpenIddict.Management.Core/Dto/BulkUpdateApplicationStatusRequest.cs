using System.ComponentModel.DataAnnotations;
using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Request payload for bulk updating application operational statuses by environment.
/// </summary>
public sealed class BulkUpdateApplicationStatusRequest
{
    /// <summary>
    /// Gets or sets the target environment (e.g. Development, Staging, Production). If null, applies to all environments.
    /// </summary>
    public ApplicationEnvironment? Environment { get; set; }

    /// <summary>
    /// Gets or sets the operational status to set across matching applications.
    /// </summary>
    [Required]
    public ApplicationStatus Status { get; set; }
}
