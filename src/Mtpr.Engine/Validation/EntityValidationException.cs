using Mtpr.Core.Exceptions;
using Mtpr.Engine.Errors;

namespace Mtpr.Engine.Validation;

/// <summary>Aggregates every violation found in one request.</summary>
public class EntityValidationException(IReadOnlyList<MtprException> violations)
    : MtprException(MtprErrors.ValidationFailed,
        new ExceptionParameter(violations.Count))
{
    public IReadOnlyList<MtprException> Violations { get; } = violations;
}
