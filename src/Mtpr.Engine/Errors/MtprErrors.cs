using Mtpr.Core.Exceptions;

namespace Mtpr.Engine.Errors;

/// <summary>Error codes the engine raises through <see cref="MtprException"/>.</summary>
public static class MtprErrors
{
    public static readonly MtprExceptionCode EntityNotFound =
        new("MTPR-1001",
            "Entity '{0}' is not defined.");

    public static readonly MtprExceptionCode RecordNotFound =
        new("MTPR-1002",
            "Record '{1}' of entity '{0}' was not found.");

    public static readonly MtprExceptionCode ValidationFailed =
        new("MTPR-2000",
            "Validation failed with {0} error(s).");

    public static readonly MtprExceptionCode FieldRequired =
        new("MTPR-2001",
            "Field '{0}' is required.");

    public static readonly MtprExceptionCode FieldTypeMismatch =
        new("MTPR-2002",
            "Field '{0}' must be of type {1}.");

    public static readonly MtprExceptionCode FieldTooLong =
        new("MTPR-2003",
            "Field '{0}' must not exceed {1} characters.");

    public static readonly MtprExceptionCode FieldTooShort =
        new("MTPR-2004", "Field '{0}' must be at least {1} characters.");

    public static readonly MtprExceptionCode FieldBelowMinimum =
        new("MTPR-2005",
            "Field '{0}' must be at least {1}.");

    public static readonly MtprExceptionCode FieldAboveMaximum =
        new("MTPR-2006",
            "Field '{0}' must be at most {1}.");

    public static readonly MtprExceptionCode UnknownField =
        new("MTPR-2007",
            "Field '{0}' is not defined on entity '{1}'.");

    public static readonly MtprExceptionCode InvalidRequestBody =
        new("MTPR-2008",
            "Request body must be a JSON object.");

    public static readonly MtprExceptionCode ReferentialIntegrity =
        new("MTPR-2009", "The operation violates a reference or uniqueness constraint.");

    public static readonly MtprExceptionCode InvalidQuery =
        new("MTPR-2010", "Invalid query: {0}");

    public static readonly MtprExceptionCode RuleRejected =
        new("MTPR-2011", "{0}");

    public static readonly MtprExceptionCode InvalidTransition =
        new("MTPR-2012", "Invalid lifecycle state or transition for field '{0}'.");

    public static readonly MtprExceptionCode ComputedFieldReadOnly =
        new("MTPR-2013", "Computed field '{0}' is read-only.");

    public static readonly MtprExceptionCode ComputationFailed =
        new("MTPR-2014", "Could not compute a valid value for field '{0}'.");

    public static readonly MtprExceptionCode InvalidDefinition =
        new("MTPR-3001",
            "Definition '{0}' is invalid: {1}");

    public static readonly MtprExceptionCode SchemaChangeNotSupported =
        new("MTPR-3002",
            "Entity '{0}': {1}");
}
