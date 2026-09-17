namespace OpenIddict.Management.Dto;

/// <summary>
/// Result summary of a batch/bulk creation, update, or deletion operation.
/// </summary>
public sealed record BulkOperationResultDto
{
    /// <summary>
    /// Gets the number of items successfully processed.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of items that failed processing.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the collection of error messages for failed items.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets the identifiers of successfully processed entities.
    /// </summary>
    public IReadOnlyList<string> ProcessedIds { get; init; } = [];

    /// <summary>
    /// Gets the total number of items requested in this batch operation.
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;
}
