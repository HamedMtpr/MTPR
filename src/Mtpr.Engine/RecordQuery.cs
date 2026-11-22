namespace Mtpr.Engine;

/// <param name="Scope">
/// Optional row scope set by the host, never by the caller: groups are OR-ed,
/// filters inside a group are AND-ed, and an empty list matches no rows.
/// </param>
public sealed record RecordQuery(int Skip,
    int Take,
    string? Sort,
    string? Fields,
    IReadOnlyList<string> Filters,
    IReadOnlyList<IReadOnlyList<string>>? Scope = null);
