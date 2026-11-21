namespace Mtpr.Engine.Persistence.AuditActorAccessors;

/// <summary>Provides the current actor's subject, when known.</summary>
public interface IAuditActorAccessor
{
    string? Subject { get; }

    string? Issuer => null;

    /// <summary>Identifies the current request, so rows of one request can be grouped.</summary>
    string? RequestId => null;
}