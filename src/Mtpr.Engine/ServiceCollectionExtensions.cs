using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mtpr.Engine.Persistence.Tenancy;
using Mtpr.Engine.Persistence.Authorization;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Persistence;
using Mtpr.Engine.Persistence.Audit;
using Mtpr.Engine.Persistence.AuditActorAccessors;
using Mtpr.Engine.Validation;

namespace Mtpr.Engine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMtprEngine(this IServiceCollection services,
        Func<IServiceProvider, string> definitionsDirectory,
        Func<IServiceProvider, string> sqliteConnectionString)
    {
        services.AddSingleton<IEntityDefinitionVerifier, EntityDefinitionVerifier>();

        services.AddSingleton<IEntityDefinitionSource>(p =>
            new JsonDirectoryDefinitionSource(definitionsDirectory(p),
                p.GetRequiredService<IEntityDefinitionVerifier>()));

        services.AddSingleton<ISqliteConnectionFactory>(p => 
            new SqliteConnectionFactory(sqliteConnectionString(p)));

        services.AddSingleton<ISchemaMigrator, SqliteSchemaMigrator>();
        services.AddSingleton<IDefinitionCatalog, DefinitionCatalog>();
        services.AddSingleton<IRecordValidator, RecordValidator>();
        services.TryAddSingleton<IAuditActorAccessor, EmptyAuditActorAccessor>();
        services.TryAddSingleton<ITenantWriteScopeAccessor, EmptyTenantWriteScopeAccessor>();
        services.TryAddSingleton<IRecordWriteAuthorization, UnrestrictedRecordWriteAuthorization>();
        services.AddSingleton<AuditTrail>();
        services.AddSingleton<IRecordStore, SqliteRecordStore>();
        services.AddSingleton<IEntityService, EntityService>();

        return services;
    }
}
