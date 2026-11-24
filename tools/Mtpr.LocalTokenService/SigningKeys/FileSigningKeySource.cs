using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Mtpr.LocalTokenService.SigningKeys;

/// <summary>
/// Reads the key from SigningKeyPath; generates and stores a new one
/// (owner-only permissions on Unix) when the file does not exist.
/// </summary>
public sealed class FileSigningKeySource(
    IOptions<LocalTokenOptions> options,
    IHostEnvironment environment) : ISigningKeySource
{
    public RSA? TryGetKey()
    {
        var path = Path.GetFullPath(
            options.Value.SigningKeyPath,
            environment.ContentRootPath);

        var rsa = RSA.Create(2048);

        if (File.Exists(path))
        {
            rsa.ImportFromPem(File.ReadAllText(path));
            return rsa;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        WriteOwnerOnly(path, rsa.ExportRSAPrivateKeyPem());
        return rsa;
    }

    private static void WriteOwnerOnly(string path, string content)
    {
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write
        };

        if (!OperatingSystem.IsWindows())
            fileOptions.UnixCreateMode =
                UnixFileMode.UserRead | UnixFileMode.UserWrite;

        using var writer = new StreamWriter(path, fileOptions);
        writer.Write(content);
    }
}
