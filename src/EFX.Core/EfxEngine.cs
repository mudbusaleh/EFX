using System.Reflection;
using EFX.Core.Discovery;
using EFX.Core.Providers;
using EFX.Core.Schema;

namespace EFX.Core;

public sealed class EfxEngine
{
    private readonly ModelDiscovery _discovery = new();
    public IReadOnlyList<Metadata.ModelInfo> DiscoverModels(Assembly assembly) => _discovery.Discover(assembly);
    public SchemaPlan Plan(Assembly assembly, IDatabaseProvider provider, string connectionString)
    {
        var plan = new SchemaPlan();
        foreach (var model in DiscoverModels(assembly))
        {
            if (!provider.GetTables(connectionString).Contains(model.Name, StringComparer.OrdinalIgnoreCase))
                plan.Add(new SchemaChange(SchemaChangeKind.CreateTable, model.Name, null, $"CREATE TABLE {model.Name} (Id INTEGER PRIMARY KEY);"));
        }
        return plan;
    }
}
