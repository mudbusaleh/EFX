using System.Text;

namespace EFX.Core.Schema;

public sealed class SchemaPlan
{
    private readonly List<SchemaChange> _changes = [];

    internal SchemaPlan(DatabaseSchema expectedSchema)
    {
        ExpectedSchema = expectedSchema ?? throw new ArgumentNullException(nameof(expectedSchema));
    }

    internal DatabaseSchema ExpectedSchema { get; }

    public IReadOnlyList<SchemaChange> Changes => _changes.AsReadOnly();
    public IReadOnlyList<SchemaChange> ExecutableChanges => _changes.Where(change => change.IsExecutable).ToArray();
    public bool HasChanges => _changes.Count > 0;
    public bool RequiresManualIntervention => _changes.Any(change => change.Kind == SchemaChangeKind.ManualIntervention);
    public bool HasDestructiveChanges => _changes.Any(change => change.IsDestructive);
    public bool CanApply => HasChanges && !RequiresManualIntervention && _changes.All(change => change.IsExecutable);

    internal void Add(SchemaChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        _changes.Add(change);
    }

    public string ToDisplayString()
    {
        if (!HasChanges)
            return "EFX Schema Plan\nNo schema changes detected. The database is already in sync.";

        var output = new StringBuilder("EFX Schema Plan\n");
        foreach (var change in _changes)
        {
            var marker = change.Kind switch
            {
                SchemaChangeKind.CreateTable => "+ CREATE TABLE",
                SchemaChangeKind.AddColumn => "+ ADD COLUMN",
                _ => "! REVIEW REQUIRED"
            };

            output.AppendLine();
            output.Append(marker).Append(' ').Append(change.Table);
            if (!string.IsNullOrWhiteSpace(change.Column))
                output.Append('.').Append(change.Column);
            if (!string.IsNullOrWhiteSpace(change.Description))
                output.Append("\n  ").Append(change.Description);
            if (!string.IsNullOrWhiteSpace(change.Warning))
                output.Append("\n  WARNING: ").Append(change.Warning);
            if (!string.IsNullOrWhiteSpace(change.Sql))
                output.Append("\n  SQL: ").Append(change.Sql);
        }

        output.Append("\n\n");
        output.Append(RequiresManualIntervention
            ? "Automatic apply is blocked until manual review is completed."
            : "No destructive changes detected. Explicit developer approval is still required before apply.");
        return output.ToString();
    }

    public override string ToString() => ToDisplayString();
}
