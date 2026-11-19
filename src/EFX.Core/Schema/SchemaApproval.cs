using System.Threading;

namespace EFX.Core.Schema;

/// <summary>
/// In-memory, one-time capability issued only by EfxEngine after the host approval callback
/// returns true. The token is bound to one exact plan instance and cannot be created by
/// application code.
/// </summary>
public sealed class SchemaApproval
{
    private readonly SchemaPlan _approvedPlan;
    private int _used;

    internal SchemaApproval(SchemaPlan approvedPlan)
    {
        _approvedPlan = approvedPlan;
    }

    public bool TryAuthorize(SchemaPlan plan) =>
        ReferenceEquals(_approvedPlan, plan) && Interlocked.CompareExchange(ref _used, 1, 0) == 0;
}
