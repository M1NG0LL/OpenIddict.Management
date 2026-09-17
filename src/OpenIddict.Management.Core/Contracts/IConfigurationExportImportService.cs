using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for exporting and importing OpenIddict Management configuration (applications and scopes)
/// for environment promotion, backup, or disaster recovery.
/// </summary>
public interface IConfigurationExportImportService
{
    /// <summary>
    /// Exports all registered applications and scopes into a structured <see cref="ManagementExportPackage"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the export package.</returns>
    Task<Result<ManagementExportPackage>> ExportConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all registered applications and scopes as formatted JSON text.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the JSON export text.</returns>
    Task<Result<string>> ExportConfigurationAsJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports applications and scopes from an export package using the specified options.
    /// </summary>
    /// <param name="package">The export package.</param>
    /// <param name="options">Optional import options controlling overwrite and inclusion rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ImportResultDto"/>.</returns>
    Task<Result<ImportResultDto>> ImportConfigurationAsync(
        ManagementExportPackage package,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports applications and scopes from a JSON string using the specified options.
    /// </summary>
    /// <param name="json">The JSON text representation of an export package.</param>
    /// <param name="options">Optional import options controlling overwrite and inclusion rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ImportResultDto"/>.</returns>
    Task<Result<ImportResultDto>> ImportConfigurationFromJsonAsync(
        string json,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default);
}
