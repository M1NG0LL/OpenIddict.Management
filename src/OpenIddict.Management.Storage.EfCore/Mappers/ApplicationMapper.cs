using System.Text.Json;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Mappers;

internal static class ApplicationMapper
{
    public static ManagedApplication ToModel<TKey>(ManagementApplication<TKey> entity)
        where TKey : IEquatable<TKey>
    {
        return new ManagedApplication
        {
            Id = entity.Id?.ToString() ?? string.Empty,
            ClientId = entity.ClientId ?? string.Empty,
            DisplayName = entity.DisplayName,
            Status = entity.Status,
            Environment = entity.Environment,
            AllowedRoles = entity.GetAllowedRoles(),
            Description = entity.Description,
            LogoUri = entity.LogoUri,
            OwnerUserId = entity.OwnerUserId,
            CreatedAt = entity.CreatedAt,
            LastModifiedAt = entity.LastModifiedAt,
            ExtraData = entity.ExtraData,
            Tags = entity.Tags ?? [],
            RedirectUris = DeserializeJsonList(entity.RedirectUris),
            PostLogoutRedirectUris = DeserializeJsonList(entity.PostLogoutRedirectUris),
            Permissions = DeserializeJsonList(entity.Permissions),
            DefaultScopes = DeserializeJsonList(entity.DefaultScopes),
            Requirements = DeserializeJsonList(entity.Requirements)
        };
    }

    public static ApplicationListDto ToListDto<TKey>(ManagementApplication<TKey> entity)
        where TKey : IEquatable<TKey>
    {
        return new ApplicationListDto
        {
            Id = entity.Id?.ToString() ?? string.Empty,
            ClientId = entity.ClientId ?? string.Empty,
            DisplayName = entity.DisplayName,
            Status = entity.Status,
            Environment = entity.Environment,
            OwnerUserId = entity.OwnerUserId,
            Tags = entity.Tags ?? [],
            CreatedAt = entity.CreatedAt
        };
    }

    private static IReadOnlyList<string> DeserializeJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
