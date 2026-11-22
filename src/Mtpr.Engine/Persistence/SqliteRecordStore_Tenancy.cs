using Microsoft.Data.Sqlite;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Persistence.Tenancy;

namespace Mtpr.Engine.Persistence;

public partial class SqliteRecordStore
{
    private static async Task verifyTenantReferencesAsync(SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition definition,
        IReadOnlyDictionary<string, object?> record,
        TenantWriteScope? scope,
        CancellationToken cancellationToken)
    {
        if (scope is null || !scope.Fields.ContainsKey(definition.Name))
            return;

        foreach (var field in definition.Fields.Where(field => field.References is not null))
        {
            if (record[field.Name] is not Guid id)
                continue;

            var target = field.References!.Entity;

            if (!scope.Fields.TryGetValue(target, out var tenantField))
                throw deniedTenantRelation();

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT 1 FROM \"{target}\" " +
                $"WHERE \"id\" = $id AND \"{tenantField}\" = $tenant";
            command.Parameters.AddWithValue("$id", id.ToString());
            command.Parameters.AddWithValue("$tenant", scope.Tenant);

            if (await command.ExecuteScalarAsync(cancellationToken) is null)
                throw deniedTenantRelation();
        }
    }

    private static void verifyCascadeTenant(CascadePlan plan, TenantWriteScope? scope)
    {
        if (scope is null)
            return;

        foreach (var row in plan.Deletes.Concat(plan.Updates.Select(item => item.Row)))
        {
            if (!scope.Fields.TryGetValue(row.Definition.Name, out var field) ||
                !row.Row.TryGetValue(field, out var value) ||
                !string.Equals(value as string, scope.Tenant, StringComparison.Ordinal))
                throw deniedTenantRelation();
        }
    }

    private static async Task verifyDeleteReferencesAsync(IDefinitionCatalog catalog,
        SqliteConnection connection,
        SqliteTransaction transaction,
        EntityDefinition definition,
        Guid id,
        CascadePlan plan,
        TenantWriteScope? scope,
        CancellationToken cancellationToken)
    {
        if (scope is null || !scope.Fields.ContainsKey(definition.Name))
            return;

        var definitions = await catalog.ListAsync(cancellationToken);
        var targets = plan.Deletes.Select(row => (row.Definition, row.Id))
            .Prepend((definition, id));

        foreach (var (target, targetId) in targets)
        foreach (var source in definitions)
        foreach (var field in source.Fields.Where(field => field.References is not null &&
                     field.References.Entity.Equals(target.Name,
                         StringComparison.OrdinalIgnoreCase)))
        {
            var rows = await selectReferencingAsync(connection, transaction,
                source, field.Name, targetId, cancellationToken);

            foreach (var row in rows)
                if (!scope.Fields.TryGetValue(source.Name, out var tenantField) ||
                    !row.TryGetValue(tenantField, out var tenant) ||
                    !string.Equals(tenant as string, scope.Tenant, StringComparison.Ordinal))
                    throw deniedTenantRelation();
        }
    }

    private static MtprException deniedTenantRelation() =>
        new(MtprErrors.RecordNotFound,
            new ExceptionParameter("record"),
            new ExceptionParameter("unavailable"));
}
