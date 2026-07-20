using System.Security.Cryptography;
using Direnix.Core.Storage;

namespace Direnix.Infrastructure.Storage;

public sealed class WindowsDpapiDatabaseKeyStore : IDatabaseKeyStore
{
    private const int KeySizeBytes = 32;
    private readonly ProductStorageOptions options;

    public WindowsDpapiDatabaseKeyStore(ProductStorageOptions options)
    {
        this.options = options;
    }

    public async ValueTask<DatabaseKeyMaterial> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        System.IO.Directory.CreateDirectory(options.DataRoot);

        if (System.IO.File.Exists(options.KeyPath))
        {
            var protectedBytes = await System.IO.File.ReadAllBytesAsync(options.KeyPath, cancellationToken);
            var key = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
            return new DatabaseKeyMaterial(key, "DPAPI LocalMachine");
        }

        var newKey = RandomNumberGenerator.GetBytes(KeySizeBytes);
        try
        {
            var protectedBytes = ProtectedData.Protect(newKey, optionalEntropy: null, DataProtectionScope.LocalMachine);
            await System.IO.File.WriteAllBytesAsync(options.KeyPath, protectedBytes, cancellationToken);
            return new DatabaseKeyMaterial(newKey.ToArray(), "DPAPI LocalMachine");
        }
        finally
        {
            Array.Clear(newKey, 0, newKey.Length);
        }
    }
}
