namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for updating the client secret of an application.
/// </summary>
public sealed record UpdateClientSecretRequest
{
    /// <summary>
    /// Gets the new client secret (or null to clear secret for public clients).
    /// </summary>
    public string? ClientSecret { get; init; }
}
