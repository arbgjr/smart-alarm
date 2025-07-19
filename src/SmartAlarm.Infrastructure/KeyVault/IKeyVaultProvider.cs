using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Abstração para provider de segredos (KeyVault).
    /// </summary>
    public interface IKeyVaultProvider
    {
        Task<string?> GetSecretAsync(string key);
        Task<bool> SetSecretAsync(string key, string value);
    }
}
