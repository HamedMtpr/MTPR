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

EntityPermissions entityPermissions = new(app.Configuration);
FieldWritePermissions fieldWritePermissions = new(app.Configuration);
FieldReadPermissions fieldReadPermissions = new(app.Configuration);

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

    if (!entityPermissions.Allows(context.HttpContext.User,
            entity,
            action))
        return Results.Forbid();

    var method = context.HttpContext.Request.Method;

    if (method is "POST" or "PUT" or "PATCH")
    {
        var body = context.Arguments.OfType<JsonElement>().FirstOrDefault();

        if (!fieldWritePermissions.Allows(context.HttpContext.User,
                entity,
                method,
                body))
            return Results.Forbid();
    }

    if (method == "GET" &&
        !fieldReadPermissions.AllowsQuery(context.HttpContext.User,
            entity,
            context.HttpContext.Request.Query))
        return Results.Forbid();

    var result = await next(context);

    return result is IResult response
        ? fieldReadPermissions.Filter(context.HttpContext.User,
            entity,
            response)
        : result;
});

api.MapPost("/",
    async (string entity,
        JsonElement body,
        IEntityService service,
        CancellationToken ct) =>
{
    var created =
        await service.CreateAsync(entity, BodyReader.ToInput(body), ct);

    return Results.Created($"/api/{entity}/{created["id"]}", created);
});

api.MapGet("/",
    async (string entity,
            int? skip,
            int? take,
            string? sort,
            string? fields,
            HttpRequest request,
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.ListAsync(entity,
        new RecordQuery(skip ?? 0, 
            take ?? 50,
            sort,
            fields,
            request.Query["filter"]
                .Select(v => v ?? string.Empty)
                .ToArray()),
        ct)));

api.MapGet("/{id:guid}",
    async (string entity,
            Guid id,
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.GetAsync(entity, id, ct)));

api.MapPut("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.UpdateAsync(entity,
        id, BodyReader.ToInput(body),
        false, ct)));

api.MapPatch("/{id:guid}",
    async (string entity,
            Guid id,
            JsonElement body,
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.UpdateAsync(entity,
        id,
        BodyReader.ToInput(body),
        true,
        ct)));

api.MapDelete("/{id:guid}",
    async (string entity,
        Guid id,
        IEntityService service,
        CancellationToken ct) =>
{
    await service.DeleteAsync(entity, id, ct);

    return Results.NoContent();
});

app.Run();