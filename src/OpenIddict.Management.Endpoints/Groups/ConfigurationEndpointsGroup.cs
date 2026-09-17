using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class ConfigurationEndpointsGroup
{
    public static RouteGroupBuilder MapConfigurationEndpoints(this RouteGroupBuilder group)
    {
        var configGroup = group.MapGroup("/configuration")
            .WithTags("Configuration");

        configGroup.MapGet("/export", async (
            [FromServices] IConfigurationExportImportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ExportConfigurationAsync(cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ExportConfiguration")
        .WithSummary("Export configuration")
        .WithDescription("Exports registered applications and scopes as a structured configuration package for backup, migration, or promotion across environments.")
        .Produces<ManagementExportPackage>(StatusCodes.Status200OK);

        configGroup.MapPost("/import", async (
            [FromBody] ManagementExportPackage package,
            [FromQuery] bool overwrite = false,
            [FromQuery] bool importApplications = true,
            [FromQuery] bool importScopes = true,
            [FromQuery] bool importConfigurations = true,
            [FromServices] IConfigurationExportImportService service = null!,
            CancellationToken cancellationToken = default) =>
        {
            var options = new ImportOptions
            {
                OverwriteExisting = overwrite,
                ImportApplications = importApplications,
                ImportScopes = importScopes,
                ImportConfigurations = importConfigurations
            };

            var result = await service.ImportConfigurationAsync(package, options, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ImportConfiguration")
        .WithSummary("Import configuration")
        .WithDescription("Imports registered applications, scopes, and runtime configurations from an exported configuration package with options for overwriting existing items.")
        .Produces<ImportResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return configGroup;
    }
}
