using System.Reflection;
using EFX.Core.Metadata;

namespace EFX.Core.Discovery;

public sealed class ModelDiscovery
{
    public IReadOnlyList<ModelInfo> Discover(Assembly assembly) => assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.Namespace?.Contains("Models") == true)
        .Select(t => new ModelInfo(t.Name, t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => new ModelProperty(p.Name, p.PropertyType.Name,
                Nullable.GetUnderlyingType(p.PropertyType) is not null || !p.PropertyType.IsValueType))
            .ToList()))
        .ToList();
}
