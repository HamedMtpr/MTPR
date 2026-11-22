using Mtpr.Engine.Persistence.Audit;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Mtpr.Api;
using Mtpr.Api.Authorization;
using Mtpr.Engine;
using Mtpr.Engine.Persistence.AuditActorAccessors;

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

ReloadablePolicy<EntityPermissions> entityPermissions = new(app.Configuration,
    configuration => new EntityPermissions(configuration),
    policyLogger,
    "entity");

ReloadablePolicy<FieldWritePermissions> fieldWritePermissions = 
    new(app.Configuration,
        configuration => new FieldWritePermissions(configuration),
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

    if (!entityPermissions.Current.Allows(context.HttpContext.User,
            entity,
            action))
        return
            await denyAsync(context,
                entity,
                $"entity:{action}");

    var method = context.HttpContext.Request.Method;

    if (method is "POST" or "PUT" or "PATCH")
    {
        var body = context.Arguments
            .OfType<JsonElement>()
            .FirstOrDefault();

        if (!fieldWritePermissions.Current
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
        !fieldReadPermissions.Current
            .AllowsQuery(context.HttpContext.User,
                entity,
                context.HttpContext.Request.Query))
        return await denyAsync(context,
            entity,
            "field-read:query");

    var result = await next(context);

    return result is IResult response
        ? fieldReadPermissions.Current.Filter(context.HttpContext.User,
            entity,
            response)
        : result;
});

api.MapPost("/",
    async (string entity,
        JsonElement body,
        ClaimsPrincipal user,
        IEntityService service,
        CancellationToken ct) =>
{
    var created =
        await service.CreateAsync(entity, 
            BodyReader.ToInput(body),
            ct,
            rowFilterPermissions.Current.ScopeOf(user, entity));

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
            rowFilterPermissions.Current.ScopeOf(user, entity)),
        ct)));

api.MapGet("/{id:guid}",
    async (string entity,
            Guid id,
            ClaimsPrincipal user,
            IEntityService service,
            CancellationToken ct) => 
        Results.Ok((object?)await service.GetAsync(entity, 
            id,
            ct, 
            rowFilterPermissions.Current.ScopeOf(user, entity))));

api.MapPut("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            ClaimsPrincipal user,
            IEntityService service,
            CancellationToken ct) => 
        Results.Ok((object?)await service.UpdateAsync(entity,
            id,
            BodyReader.ToInput(body),
            false,
            ct,
            rowFilterPermissions.Current.ScopeOf(user, entity))));

api.MapPatch("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            ClaimsPrincipal user,
            IEntityService service,
            CancellationToken ct) =>
        Results.Ok((object?)await service.UpdateAsync(entity,
            id,
            BodyReader.ToInput(body),
            true,
            ct,
            rowFilterPermissions.Current.ScopeOf(user, entity))));

api.MapDelete("/{id:guid}",
    async (string entity,
        Guid id,
        ClaimsPrincipal user,
        IEntityService service,
        CancellationToken ct) =>
{
    await service.DeleteAsync(entity, 
        id, 
        ct,
        rowFilterPermissions.Current.ScopeOf(user, entity));

    return Results.NoContent();
});

app.Run();
return;

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