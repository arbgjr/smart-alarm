using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Provider real para HashiCorp Vault (dev/homologação).
    /// </summary>
    public class HashiCorpVaultProvider : IKeyVaultProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HashiCorpVaultProvider> _logger;
        private readonly string _token;
        private readonly string _mountPath;

        public HashiCorpVaultProvider(HttpClient httpClient, ILogger<HashiCorpVaultProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _token = "dev-token";
            _mountPath = "secret";
        }

        public async Task<string?> GetSecretAsync(string key)
        {
            var url = $"/v1/{_mountPath}/data/{key}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Vault-Token", _token);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Vault] Falha ao ler segredo {Key}: {Status}", key, response.StatusCode);
                return null;
            }
            var json = await response.Content.ReadFromJsonAsync<VaultSecretResponse>();
            return json?.Data?.Data?["value"];
        }

        public async Task<bool> SetSecretAsync(string key, string value)
        {
            var url = $"/v1/{_mountPath}/data/{key}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Vault-Token", _token);
            request.Content = JsonContent.Create(new { data = new { value } });
            var response = await _httpClient.SendAsync(request);
            var ok = response.IsSuccessStatusCode;
            if (!ok)
                _logger.LogWarning("[Vault] Falha ao escrever segredo {Key}: {Status}", key, response.StatusCode);
            return ok;
        }

        private class VaultSecretResponse
        {
            public VaultSecretData? Data { get; set; }
        }
        private class VaultSecretData
        {
            public System.Collections.Generic.Dictionary<string, string>? Data { get; set; }
        }
    }
}
