using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;
using Mtpr.Engine.Validation;

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
    }

    private static int statusOf(string code)
    {
        if (code == MtprErrors.EntityNotFound.Value ||
            code == MtprErrors.RecordNotFound.Value)
            return StatusCodes.Status404NotFound;

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
