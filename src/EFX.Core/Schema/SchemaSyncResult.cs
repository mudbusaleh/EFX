namespace EFX.Core.Schema;

public enum SchemaSyncStatus
{
    NoChanges,
    ApprovalDeclined,
    Applied,
    BlockedForManualIntervention
}

public sealed record SchemaSyncResult(
    SchemaPlan Plan,
    SchemaSyncStatus Status,
    DatabaseApplyResult? ApplyResult = null)
{
    public bool Applied => Status == SchemaSyncStatus.Applied;
}
