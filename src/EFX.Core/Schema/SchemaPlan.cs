namespace EFX.Core.Schema;

public sealed class SchemaPlan
{
    private readonly List<SchemaChange> _changes = [];
    public IReadOnlyList<SchemaChange> Changes => _changes;
    public void Add(SchemaChange change) => _changes.Add(change);
    public bool HasChanges => _changes.Count > 0;
}
