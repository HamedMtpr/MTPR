using Mtpr.Engine.Persistence.AuditActorAccessors;

namespace Mtpr.Engine.Persistence.Audit;

/// <summary>Records audit rows that are not part of a record change, such as denials.</summary>
public sealed class AuditTrail(ISqliteConnectionFactory connectionFactory,
    IAuditActorAccessor actor)
{
    private const int maxEntityLength = 200;

    /// <summary>
    /// Records a denied request. The entity name comes from the URL, so it is capped.
    /// Failures propagate: a denial that cannot be recorded must not pass silently.
    /// </summary>
    public async Task RecordDeniedAsync(string entity,
        string detail,
        CancellationToken cancellationToken)
    {
        var capped = entity.Length > maxEntityLength
            ? entity[..maxEntityLength]
            : entity;

        await using var connection =
            await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        await AuditTable.AppendAsync(connection,
            transaction,
            new AuditEntry(capped, 
                string.Empty, "denied", Detail: detail),
            actor,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
