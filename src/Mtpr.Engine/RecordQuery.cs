namespace Mtpr.Engine;

public sealed record RecordQuery(int Skip,
    int Take,
    string? Sort,
    string? Fields,
    IReadOnlyList<string> Filters);
