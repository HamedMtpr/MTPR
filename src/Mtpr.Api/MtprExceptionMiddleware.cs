using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Validation;
using Microsoft.Data.Sqlite;

namespace Mtpr.Api;

/// <summary>Translates <see cref="MtprException"/> into RFC 7807-style JSON responses.</summary>
public class MtprExceptionMiddleware(RequestDelegate next, ILogger<MtprExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (EntityValidationException e)
        {
            await writeAsync(context,
                StatusCodes.Status422UnprocessableEntity, new
            {
                code = e.Code,
                message = e.Message,
                errors = e.Violations.Select(v =>
                    new
                    {
                        code = v.Code,
                        message = v.Message
                    })
            });
        }
        catch (MtprException e)
        {
            logger.LogWarning(e, "Request failed with {Code}", e.Code);
            await writeAsync(context, statusOf(e.Code),
                new
                {
                    code = e.Code,
                    message = e.Message
                });
        }
        catch (SqliteException e) when (e.SqliteExtendedErrorCode is 787 or 1811 or 2067)
        {
            logger.LogWarning(e,
                "Reference or uniqueness constraint failed");
            await writeAsync(context, StatusCodes.Status409Conflict, new
            {
                code = MtprErrors.ReferentialIntegrity.Value,
                message = 
                    "The operation violates a reference or uniqueness constraint."
            });
        }
    }

    private static int statusOf(string code)
    {
        if (code == MtprErrors.EntityNotFound.Value ||
            code == MtprErrors.RecordNotFound.Value)
            return StatusCodes.Status404NotFound;

        if (code == MtprErrors.RuleRejected.Value)
            return StatusCodes.Status422UnprocessableEntity;

        if (code == MtprErrors.InvalidDefinition.Value ||
            code == MtprErrors.SchemaChangeNotSupported.Value)
            return StatusCodes.Status500InternalServerError;

        return StatusCodes.Status400BadRequest;
    }

    private static Task writeAsync(HttpContext context, int status, object body)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(body);
    }
}
