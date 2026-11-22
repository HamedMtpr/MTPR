namespace Mtpr.Engine.Persistence.Audit;

/// <summary>One row of the append-only audit log.</summary>
/// <param name="Entity">The entity the change or denial applies to.</param>
/// <param name="RecordId">The record id, or empty when no record is involved.</param>
/// <param name="Operation">create, update, delete, or denied.</param>
/// <param name="Before">Snapshot before the change, when there is one.</param>
/// <param name="After">Snapshot after the change, when there is one.</param>
/// <param name="Detail">Free text, such as the reason of a denial.</param>
/// <param name="CausedBy">The "entity/id" of the root record of a cascade.</param>
public sealed record AuditEntry(string Entity,
    string RecordId,
    string Operation,
    IReadOnlyDictionary<string, object?>? Before = null,
    IReadOnlyDictionary<string, object?>? After = null,
    string? Detail = null,
    string? CausedBy = null);
