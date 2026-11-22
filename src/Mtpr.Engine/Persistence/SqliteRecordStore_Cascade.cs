using Microsoft.Data.Sqlite;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Definitions.Fields;

namespace Mtpr.Engine.Persistence;

/// <summary>
/// Works out which rows the database will delete or null out when a record is deleted,
/// so that those changes are audited like any other.
/// </summary>
public partial class SqliteRecordStore
{
    private sealed record CascadedRow(EntityDefinition Definition,
        Guid Id,
        IReadOnlyDictionary<string, object?> Row);

    private sealed record CascadePlan(IReadOnlyList<CascadedRow> Deletes,
        IReadOnlyList<(CascadedRow Row, IReadOnlyDictionary<string, object?> After)> Updates);

    /// <summary>
    /// Walks the references breadth-first. The visited set is keyed by entity and id,
    /// so reference cycles end. Restrict is left to the database, which rejects the
    /// delete and rolls the transaction back, so nothing is audited for it.
    /// </summary>
    private static async Task<CascadePlan> planCascadeAsync(IDefinitionCatalog catalog,
        SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition root,
        Guid rootId,
        CancellationToken cancellationToken)
    {
        var definitions = await catalog.ListAsync(cancellationToken);

        HashSet<(string Entity, Guid Id)> visited =
            [(root.Name.ToLowerInvariant(), rootId)];

        Queue<(EntityDefinition Definition, Guid Id)> pending = new();
        pending.Enqueue((root, rootId));

        List<CascadedRow> deletes = [];
        List<(CascadedRow Row, string Field)> nulls = [];

        while (pending.Count > 0)
        {
            var (current, currentId) = pending.Dequeue();

            foreach (var (referencing, field) in referencesTo(definitions, current))
            {
                var rows = await selectReferencingAsync(connection,
                    transaction,
                    referencing,
                    field.Name,
                    currentId,
                    cancellationToken);

                foreach (var row in rows)
                {
                    var cascaded = new CascadedRow(referencing,
                        (Guid)row[idKey]!,
                        row);

                    if (field.References!.OnDelete == DeleteBehavior.Cascade)
                    {
                        if (!visited.Add((referencing.Name.ToLowerInvariant(), cascaded.Id)))
                            continue;

                        deletes.Add(cascaded);
                        pending.Enqueue((referencing, cascaded.Id));
                    }
                    else
                        nulls.Add((cascaded, field.Name));
                }
            }
        }

        // A row that is deleted anyway needs no set-null; this keeps the result
        // independent of the order in which the references were found.
        var updates = nulls
            .Where(n => !visited.Contains((n.Row.Definition.Name.ToLowerInvariant(), n.Row.Id)))
            .GroupBy(n => (n.Row.Definition.Name.ToLowerInvariant(), n.Row.Id))
            .Select(group =>
            {
                var first = group.First().Row;
                var after = new Dictionary<string, object?>(first.Row);

                foreach (var (_, field) in group)
                    after[field] = null;

                return (first, (IReadOnlyDictionary<string, object?>)after);

            })
            .ToList();

        return new CascadePlan(deletes, updates);
    }

    private async Task auditCascadeAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        string rootEntity,
        Guid rootId,
        CascadePlan plan,
        CancellationToken cancellationToken)
    {
        var causedBy = $"{rootEntity}/{rootId}";

        foreach (var (row, after) in plan.Updates)
            await auditAsync(connection,
                transaction,
                row.Definition.Name,
                row.Id,
                "update",
                row.Row,
                after,
                cancellationToken,
                causedBy);

        // Breadth-first order has parents before children; audit children first.
        foreach (var row in plan.Deletes.Reverse())
            await auditAsync(connection,
                transaction,
                row.Definition.Name,
                row.Id,
                "delete",
                row.Row,
                null,
                cancellationToken,
                causedBy);
    }

    private static IEnumerable<(EntityDefinition Definition, FieldDefinition Field)> referencesTo(
        IReadOnlyList<EntityDefinition> definitions,
        EntityDefinition target) =>
        from definition in definitions
        from field in definition.Fields
        where field.References is not null
              && field.References.OnDelete != DeleteBehavior.Restrict
              && field.References.Entity.Equals(target.Name,
                  StringComparison.OrdinalIgnoreCase)
        select (definition, field);

    private static async Task<List<IReadOnlyDictionary<string, object?>>> selectReferencingAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition definition,
        string field,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT * FROM \"{definition.Name}\" WHERE \"{field}\" = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        List<IReadOnlyDictionary<string, object?>> rows = [];
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            rows.Add(map(definition, reader));

        return rows;
    }
}
