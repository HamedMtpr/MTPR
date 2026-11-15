using System.Text.Json;
using Mtpr.Api;
using Mtpr.Engine;

var builder = WebApplication.CreateBuilder(args);

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

app.UseMiddleware<MtprExceptionMiddleware>();

var api = app.MapGroup("/api/{entity}");

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
            IEntityService service,
            CancellationToken ct)
    => Results.Ok(await service.ListAsync(entity, 
        skip ?? 0,
        take ?? 50,
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
