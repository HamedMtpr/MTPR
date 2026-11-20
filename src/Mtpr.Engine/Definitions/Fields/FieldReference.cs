namespace Mtpr.Engine.Definitions.Fields;

public class FieldReference
{
    public string Entity { get; set; } = string.Empty;
    public DeleteBehavior OnDelete { get; set; } = DeleteBehavior.Restrict;
}

public enum DeleteBehavior
{
    Restrict,
    Cascade,
    SetNull
}
