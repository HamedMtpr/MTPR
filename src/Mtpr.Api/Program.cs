using Mtpr.Engine.Persistence.Audit;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Mtpr.Api;
using Mtpr.Api.Authorization;
using Mtpr.Engine;
using Mtpr.Engine.Definitions;
using Mtpr.Engine.Persistence.AuditActorAccessors;
using Mtpr.Engine.Persistence.Tenancy;
using Mtpr.Engine.Persistence.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.Authority =
            configuration["Mtpr:Authentication:Authority"];
        options.Audience =
            configuration["Mtpr:Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditActorAccessor, HttpAuditActorAccessor>();
builder.Services.AddSingleton<ITenantWriteScopeAccessor, HttpTenantWriteScopeAccessor>();
builder.Services.AddSingleton<IRecordWriteAuthorization, HttpRecordWriteAuthorization>();

builder.Services.AddMtprEngine(
    p =>
    {
        var configuration = p.GetRequiredService<IConfiguration>();

        return Path.GetFullPath(
            configuration["Mtpr:DefinitionsPath"] ?? "definitions",
            p.GetRequiredService<IWebHostEnvironment>()
                .ContentRootPath);
    },
    p =>
        p.GetRequiredService<IConfiguration>()["Mtpr:ConnectionString"]
            ?? "Data Source=mtpr.db");

var app = builder.Build();

var authority =
    app.Configuration["Mtpr:Authentication:Authority"];
var audience =
    app.Configuration["Mtpr:Authentication:Audience"];

if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
    || authorityUri.Scheme != Uri.UriSchemeHttps
    || string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException(
        """
        Configure an HTTPS Mtpr:
        Authentication:Authority and Mtpr:Authentication:Audience.
        """);
}

app.UseMiddleware<MtprExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

var policyLogger = app.Logger;

ReloadablePolicy<EntityPermissions> entityPermissions =
    new(app.Configuration,
    configuration => new EntityPermissions(configuration),
    policyLogger,
    "entity");

ReloadablePolicy<FieldWritePermissions> fieldWritePermissions = 
    new(app.Configuration,
        configuration =>
            new FieldWritePermissions(configuration),
        policyLogger,
        "field write");

ReloadablePolicy<FieldReadPermissions> fieldReadPermissions = 
    new(app.Configuration,
        configuration => new FieldReadPermissions(configuration),
        policyLogger,
        "field read");

ReloadablePolicy<RowFilterPermissions> rowFilterPermissions =
    new(app.Configuration,
        configuration => new RowFilterPermissions(configuration),
        policyLogger,
        "row filter");

ReloadablePolicy<TenantPolicy> tenantPolicy =
    new(app.Configuration,
        configuration => new TenantPolicy(configuration),
        policyLogger,
        "tenant");

var api = app.MapGroup("/api/{entity}")
    .RequireAuthorization();

api.AddEndpointFilter(async (context, next) =>
{
    var entity =
        context.HttpContext
            .Request
            .RouteValues["entity"]?.ToString() ?? string.Empty;

    var action = context.HttpContext.Request.Method switch
    {
        "POST" => "create",
        "GET" => "read",
        "PUT" or "PATCH" => "update",
        "DELETE" => "delete",
        _ => string.Empty
    };

    var entities = entityPermissions.Current;
    var writes = fieldWritePermissions.Current;
    var reads = fieldReadPermissions.Current;

    context.HttpContext.Items["Mtpr.RecordWritePolicy"] =
        new RecordWritePolicy(context.HttpContext.User,
            entities,
            writes);

    if (!entities.Allows(context.HttpContext.User,
            entity,
            action))
        return
            await denyAsync(context,
                entity,
                $"entity:{action}");

    var method = context.HttpContext.Request.Method;

    var tenancy = tenantPolicy.Current;
    context.HttpContext.Items["Mtpr.TenantPolicy"] = tenancy;
    context.HttpContext.Items["Mtpr.RowFilterPermissions"] = rowFilterPermissions.Current;

    if (tenancy.AppliesTo(entity) &&
        tenancy.TenantOf(context.HttpContext.User) is null)
        return await denyAsync(context,
            entity,
            "tenant:missing");

    var tenant = tenancy.TenantOf(context.HttpContext.User);

    if (tenant is not null)
        context.HttpContext.Items["Mtpr.TenantWriteScope"] =
            new TenantWriteScope(tenant, tenancy.Fields);

    var catalog = context.HttpContext.RequestServices.GetRequiredService<IDefinitionCatalog>();
    using var catalogLease = await catalog.AcquireReadLeaseAsync(
        context.HttpContext.RequestAborted);
    using var catalogSnapshot = catalog.PinSnapshot();

    if (tenancy.AppliesTo(entity) ||
        method is "POST" or "PUT" or "PATCH" or "DELETE")
    {
        var definitions = await catalog.ListAsync(context.HttpContext.RequestAborted);
        var definition = definitions.FirstOrDefault(item =>
            item.Name.Equals(entity, StringComparison.OrdinalIgnoreCase));

        if ((definition is not null &&
             !tenancy.IsValidDefinition(definition)) ||
            (method is "POST" or "PUT" or "PATCH" or "DELETE" &&
             (tenancy.HasTenantRelations(entity, definitions) ||
              definitions.Any(item =>
                  !tenancy.IsValidDefinition(item)))))
            return await denyAsync(context,
                entity,
                "tenant:unsafe-definition");
    }

    if (method is "POST" or "PUT" or "PATCH")
    {
        var body = context.Arguments
            .OfType<JsonElement>()
            .FirstOrDefault();

        if (tenancy.Conflicts(context.HttpContext.User,
                entity,
                body))
            return
                await denyAsync(context,
                    entity,
                    $"tenant:{method}");

        if (!writes
                .Allows(context.HttpContext.User,
                    entity,
                    method,
                body))
            return 
                await denyAsync(context,
                    entity, 
                    $"field-write:{method}");
    }

    if (method == "GET" &&
        !reads
            .AllowsQuery(context.HttpContext.User,
                entity,
                context.HttpContext.Request.Query))
        return await denyAsync(context,
            entity,
            "field-read:query");

    object? result;
    try
    {
        result = await next(context);
    }
    catch (RecordWriteForbiddenException)
    {
        return await denyAsync(context,
            entity, 
            "field-write:indirect");
    }

    return result is IResult response
        ? reads.Filter(context.HttpContext.User,
            entity,
            response)
        : result;
});

api.MapPost("/",
    async (string entity,
        JsonElement body,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IEntityService service,
        CancellationToken ct) =>
{
    var created =
        await service.CreateAsync(entity, 
            stamp(httpContext, user, entity, body),
            ct,
            scopeOf(httpContext, user, entity));

    return Results.Created($"/api/{entity}/{created["id"]}", created);
});

api.MapGet("/",
    async (string entity,
            int? skip,
            int? take,
            string? sort,
            string? fields,
            HttpRequest request,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.ListAsync(entity,
        new RecordQuery(skip ?? 0,
            take ?? 50,
            sort,
            fields,
            request.Query["filter"]
                .Select(v => v ?? string.Empty)
                .ToArray(),
            scopeOf(httpContext, user, entity)),
        ct)));

api.MapGet("/{id:guid}",
    async (string entity,
            Guid id,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IEntityService service,
            CancellationToken ct) => 
        Results.Ok(await service.GetAsync(entity,
            id,
            ct, 
            scopeOf(httpContext, user, entity))));

api.MapPut("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IEntityService service,
            CancellationToken ct) => 
        Results.Ok(await service.UpdateAsync(entity,
            id,
            stamp(httpContext, user, entity, body),
            false,
            ct,
            scopeOf(httpContext, user, entity))));

api.MapPatch("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IEntityService service,
            CancellationToken ct) =>
        Results.Ok(await service.UpdateAsync(entity,
            id,
            stamp(httpContext, user, entity, body),
            true,
            ct,
            scopeOf(httpContext, user, entity))));

api.MapDelete("/{id:guid}",
    async (string entity,
        Guid id,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IEntityService service,
        CancellationToken ct) =>
{
    await service.DeleteAsync(entity, 
        id, 
        ct,
        scopeOf(httpContext, user, entity));

    return Results.NoContent();
});

app.Run();
return;

IReadOnlyList<IReadOnlyList<string>>? scopeOf(HttpContext context,
    ClaimsPrincipal user,
    string entity) =>
    ((TenantPolicy)context.Items["Mtpr.TenantPolicy"]!).Scope(user,
        entity,
        ((RowFilterPermissions)context.Items["Mtpr.RowFilterPermissions"]!).ScopeOf(user, entity));

IReadOnlyDictionary<string, JsonElement> stamp(HttpContext context,
    ClaimsPrincipal user,
    string entity,
    JsonElement body) =>
    ((TenantPolicy)context.Items["Mtpr.TenantPolicy"]!).Stamp(user,
        entity,
        BodyReader.ToInput(body));

static async Task<object?> denyAsync(EndpointFilterInvocationContext context,
    string entity,
    string detail)
{
    await context.HttpContext
        .RequestServices
        .GetRequiredService<AuditTrail>()
        .RecordDeniedAsync(entity,
            detail,
            context.HttpContext.RequestAborted);

    return Results.Forbid();
}