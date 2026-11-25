namespace Mtpr.LocalTokenService;

public interface ITokenIssuer
{
    string Issue(LocalClient client, LocalTokenOptions options);
}