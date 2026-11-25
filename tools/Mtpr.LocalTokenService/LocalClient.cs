namespace Mtpr.LocalTokenService;

public sealed class LocalClient
{
    public string Secret { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string? TenantId { get; set; }
}