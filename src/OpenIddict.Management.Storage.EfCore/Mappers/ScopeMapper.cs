using System.Text.Json;
using OpenIddict.Management.Models;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Mappers;

internal static class ScopeMapper
{
    public static ManagedScope ToModel<TKey>(ManagementScope<TKey> entity)
        where TKey : IEquatable<TKey>
    {
        return new ManagedScope
        {
            Id = entity.Id?.ToString() ?? string.Empty,
            Name = entity.Name ?? string.Empty,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Resources = DeserializeJsonList(entity.Resources),
            CreatedAt = entity.CreatedAt,
            LastModifiedAt = entity.LastModifiedAt
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
