using System.Text.Json;
using Mtpr.Core.Exceptions;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Persistence;
using Mtpr.Engine.Validation;
using Mtpr.Engine.Rules;

namespace Mtpr.Engine;

public class EntityService(IDefinitionCatalog catalog,
    IRecordValidator validator,
    IRecordStore store) : IEntityService
{
    public async Task<IReadOnlyDictionary<string, object?>> CreateAsync(string entity,
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken)
    {
        var definition = await catalog.GetAsync(entity, cancellationToken);
        var withDefaults = new Dictionary<string, JsonElement>(input);
        
        foreach (var field in definition.Fields)
            if (field.Default.ValueKind != JsonValueKind.Undefined &&
                !withDefaults.Keys.Any(key => 
                    string.Equals(key, 
                        field.Name, 
                        StringComparison.OrdinalIgnoreCase)))
                withDefaults.Add(field.Name, field.Default);

        var values = 
            validator.Validate(definition, 
                withDefaults, 
                partial: false);

        CreateRuleInterpreter.Execute(definition, values);
        return await store.InsertAsync(definition, values, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, object?>> GetAsync(string entity,
        Guid id,
        CancellationToken cancellationToken)
    {
        var definition = await catalog.GetAsync(entity, cancellationToken);

        return await store.FindAsync(definition, id, cancellationToken)
               ?? throw notFound(definition, id);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListAsync(string entity,
        RecordQuery query,
        CancellationToken cancellationToken)
    {
        var definition = await catalog.GetAsync(entity, cancellationToken);

        return await store.ListAsync(definition,
            query with
            {
                Skip = Math.Max(query.Skip, 0),
                Take = Math.Clamp(query.Take, 1, 200)
            },
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, object?>> UpdateAsync(string entity,
        Guid id,
        IReadOnlyDictionary<string, JsonElement> input,
        bool partial,
        CancellationToken cancellationToken)
    {
        var definition = await catalog.GetAsync(entity, cancellationToken);
        var values = validator.Validate(definition, input, partial);

        if (!await store.UpdateAsync(definition, id, values, cancellationToken))
            throw notFound(definition, id);

        return await GetAsync(entity, id, cancellationToken);
    }

    public async Task DeleteAsync(string entity, Guid id, CancellationToken cancellationToken)
    {
        var definition = await catalog.GetAsync(entity, cancellationToken);

        if (!await store.DeleteAsync(definition, id, cancellationToken))
            throw notFound(definition, id);
    }

    private static MtprException notFound(EntityDefinition definition, Guid id)
        => new(MtprErrors.RecordNotFound,
            new ExceptionParameter(definition.Name),
            new ExceptionParameter(id));
}