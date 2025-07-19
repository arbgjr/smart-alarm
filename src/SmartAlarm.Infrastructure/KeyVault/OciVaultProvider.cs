using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Implementação real do provedor de KeyVault OCI Vault
    /// Implementação completa para produção com autenticação e observabilidade
    /// </summary>
    public class OciVaultProvider : IKeyVaultProvider, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OciVaultProvider> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ActivitySource _activitySource;
        private readonly HttpClient _httpClient;
        private readonly string _vaultId;
        private readonly string _compartmentId;
        private readonly string _region;
        private readonly string _tenancyId;
        private readonly string _userId;
        private readonly string _fingerprint;
        private readonly string _privateKey;
        private bool _disposed = false;

        public OciVaultProvider(
            IConfiguration configuration,
            ILogger<OciVaultProvider> logger,
            SmartAlarmMeter meter,
            ActivitySource activitySource,
            HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Configurações OCI obrigatórias
            _vaultId = _configuration["OCI:Vault:VaultId"] 
                ?? throw new InvalidOperationException("OCI Vault VaultId não configurado");
            _compartmentId = _configuration["OCI:Vault:CompartmentId"] 
                ?? throw new InvalidOperationException("OCI Vault CompartmentId não configurado");
            _region = _configuration["OCI:Region"] 
                ?? throw new InvalidOperationException("OCI Region não configurada");
            _tenancyId = _configuration["OCI:TenancyId"] 
                ?? throw new InvalidOperationException("OCI TenancyId não configurado");
            _userId = _configuration["OCI:UserId"] 
                ?? throw new InvalidOperationException("OCI UserId não configurado");
            _fingerprint = _configuration["OCI:Fingerprint"] 
                ?? throw new InvalidOperationException("OCI Fingerprint não configurado");
            _privateKey = _configuration["OCI:PrivateKey"] 
                ?? throw new InvalidOperationException("OCI PrivateKey não configurada");

            // Configurar cliente HTTP para OCI
            _httpClient.BaseAddress = new Uri($"https://vaults.{_region}.oraclecloud.com");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartAlarm/1.0");

            _logger.LogInformation("OciVaultProvider initialized for vault {VaultId} in region {Region}", 
                _vaultId, _region);
        }

        /// <summary>
        /// Cria assinatura de autenticação para OCI REST API
        /// </summary>
        private string CreateSignature(string method, string uri, Dictionary<string, string> headers, string body = "")
        {
            var signingString = BuildSigningString(method, uri, headers, body);
            
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(_privateKey.ToCharArray());
                var signature = rsa.SignData(Encoding.UTF8.GetBytes(signingString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signature);
            }
        }

        /// <summary>
        /// Constrói string para assinatura conforme especificação OCI
        /// </summary>
        private string BuildSigningString(string method, string uri, Dictionary<string, string> headers, string body)
        {
            var lines = new List<string>
            {
                $"(request-target): {method.ToLower()} {uri}"
            };

            foreach (var header in headers.OrderBy(h => h.Key.ToLower()))
            {
                lines.Add($"{header.Key.ToLower()}: {header.Value}");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Cria cabeçalhos de autenticação OCI
        /// </summary>
        private Dictionary<string, string> CreateAuthHeaders(string method, string uri, string contentLength = "0", string contentType = "application/json")
        {
            var date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss GMT", CultureInfo.InvariantCulture);
            
            var headers = new Dictionary<string, string>
            {
                { "date", date },
                { "host", $"vaults.{_region}.oraclecloud.com" },
                { "content-length", contentLength },
                { "content-type", contentType }
            };

            var signature = CreateSignature(method, uri, headers);
            var keyId = $"{_tenancyId}/{_userId}/{_fingerprint}";
            var signingHeaders = string.Join(" ", headers.Keys.Prepend("(request-target)"));
            
            headers["authorization"] = $"Signature keyId=\"{keyId}\",algorithm=\"rsa-sha256\",headers=\"{signingHeaders}\",signature=\"{signature}\"";
            
            return headers;
        }

        public async Task<string?> GetSecretAsync(string key)
        {
            using var activity = _activitySource.StartActivity("OCI.Vault.GetSecret");
            activity?.SetTag("vault.operation", "get_secret");
            activity?.SetTag("vault.provider", "oci");
            activity?.SetTag("vault.key", key);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "OCIGetSecret",
                new { Key = key });

            try
            {
                // TODO: Uncomment when OCI SDK is properly configured
                // var secretsClient = new VaultsClient(authenticationDetailsProvider);
                // var secretsManagementClient = new VaultsManagementClient(authenticationDetailsProvider);
                //
                // var listSecretsRequest = new ListSecretsRequest
                // {
                //     CompartmentId = _compartmentId,
                //     VaultId = _vaultId,
                //     Name = key
                // };
                //
                // var secrets = await secretsManagementClient.ListSecrets(listSecretsRequest);
                // if (!secrets.Items.Any())
                // {
                //     _logger.LogWarning("Secret not found in OCI Vault: {Key}", key);
                //     return null;
                // }
                //
                // var secret = secrets.Items.First();
                // var getSecretBundleRequest = new GetSecretBundleRequest
                // {
                //     SecretId = secret.Id
                // };
                //
                // var secretBundle = await secretsClient.GetSecretBundle(getSecretBundleRequest);
                // var secretContent = secretBundle.SecretBundleContent as Base64SecretBundleContentDetails;
                // if (secretContent?.Content != null)
                // {
                //     return Encoding.UTF8.GetString(Convert.FromBase64String(secretContent.Content));
                // }

                // Implementação real estruturada para OCI Vault via REST API
                var result = await GetSecretFromOciAsync(key);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "OCI", "GetSecret", result != null);

                _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                    "GetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("KEYVAULT", "OCI", "GetSecretError");

                _logger.LogError(LogTemplates.KeyVaultOperationFailed,
                    "GetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return null;
            }
        }

        private async Task<string?> GetSecretFromOciAsync(string key)
        {
            // Primeiro, listar secrets para encontrar o ID
            var listUri = $"/20180608/secrets?compartmentId={Uri.EscapeDataString(_compartmentId)}&vaultId={Uri.EscapeDataString(_vaultId)}&name={Uri.EscapeDataString(key)}";
            var listHeaders = CreateAuthHeaders("GET", listUri, "0", "application/json");

            using var listRequest = new HttpRequestMessage(HttpMethod.Get, listUri);
            foreach (var header in listHeaders)
            {
                if (header.Key == "host")
                    listRequest.Headers.Host = header.Value;
                else if (header.Key != "content-length" && header.Key != "content-type")
                    listRequest.Headers.Add(header.Key, header.Value);
            }

            _logger.LogDebug("Listing secrets in OCI Vault for key: {Key}", key);

            var listResponse = await _httpClient.SendAsync(listRequest);
            if (!listResponse.IsSuccessStatusCode)
            {
                var errorContent = await listResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"OCI list secrets failed with status {listResponse.StatusCode}: {errorContent}");
            }

            var listJson = await listResponse.Content.ReadAsStringAsync();
            using var listDoc = JsonDocument.Parse(listJson);
            var secrets = listDoc.RootElement.GetProperty("data");

            if (secrets.GetArrayLength() == 0)
            {
                _logger.LogWarning("Secret not found in OCI Vault: {Key}", key);
                return null;
            }

            var secretId = secrets[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(secretId))
            {
                throw new InvalidOperationException($"Secret ID not found for key: {key}");
            }

            // Agora obter o conteúdo do secret
            var getUri = $"/20190301/secretBundles/{Uri.EscapeDataString(secretId)}";
            var getHeaders = CreateAuthHeaders("GET", getUri, "0", "application/json");

            using var getRequest = new HttpRequestMessage(HttpMethod.Get, getUri);
            foreach (var header in getHeaders)
            {
                if (header.Key == "host")
                    getRequest.Headers.Host = header.Value;
                else if (header.Key != "content-length" && header.Key != "content-type")
                    getRequest.Headers.Add(header.Key, header.Value);
            }

            _logger.LogDebug("Getting secret bundle from OCI Vault: {SecretId}", secretId);

            var getResponse = await _httpClient.SendAsync(getRequest);
            if (!getResponse.IsSuccessStatusCode)
            {
                var errorContent = await getResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"OCI get secret failed with status {getResponse.StatusCode}: {errorContent}");
            }

            var getJson = await getResponse.Content.ReadAsStringAsync();
            using var getDoc = JsonDocument.Parse(getJson);
            var secretContent = getDoc.RootElement.GetProperty("secretBundleContent");
            var contentType = secretContent.GetProperty("contentType").GetString();
            
            if (contentType == "BASE64")
            {
                var base64Content = secretContent.GetProperty("content").GetString();
                if (!string.IsNullOrEmpty(base64Content))
                {
                    var decryptedBytes = Convert.FromBase64String(base64Content);
                    var result = Encoding.UTF8.GetString(decryptedBytes);
                    
                    _logger.LogInformation("Successfully retrieved secret {Key} from OCI Vault", key);
                    return result;
                }
            }

            throw new InvalidOperationException($"Unsupported secret content type: {contentType}");
        }

        public async Task<bool> SetSecretAsync(string key, string value)
        {
            using var activity = _activitySource.StartActivity("OCI.Vault.SetSecret");
            activity?.SetTag("vault.operation", "set_secret");
            activity?.SetTag("vault.provider", "oci");
            activity?.SetTag("vault.key", key);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "OCISetSecret",
                new { Key = key });

            try
            {
                // TODO: Uncomment when OCI SDK is properly configured
                // var secretsManagementClient = new VaultsManagementClient(authenticationDetailsProvider);
                //
                // var secretContent = new Base64SecretContentDetails
                // {
                //     Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                // };
                //
                // var createSecretRequest = new CreateSecretRequest
                // {
                //     CreateSecretDetails = new CreateSecretDetails
                //     {
                //         CompartmentId = _compartmentId,
                //         VaultId = _vaultId,
                //         SecretName = key,
                //         SecretContent = secretContent
                //     }
                // };
                //
                // await secretsManagementClient.CreateSecret(createSecretRequest);

                // Implementação real estruturada para OCI Vault via REST API
                var result = await SetSecretInOciAsync(key, value);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "OCI", "SetSecret", result);

                _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                    "SetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("KEYVAULT", "OCI", "SetSecretError");

                _logger.LogError(LogTemplates.KeyVaultOperationFailed,
                    "SetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return false;
            }
        }

        private async Task<bool> SetSecretInOciAsync(string key, string value)
        {
            var createSecretDetails = new
            {
                compartmentId = _compartmentId,
                vaultId = _vaultId,
                secretName = key,
                description = $"Secret {key} managed by SmartAlarm",
                secretContent = new
                {
                    contentType = "BASE64",
                    content = Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                }
            };

            var json = JsonSerializer.Serialize(createSecretDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var uri = "/20180608/secrets";
            var headers = CreateAuthHeaders("POST", uri, jsonBytes.Length.ToString(), "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            foreach (var header in headers)
            {
                if (header.Key == "content-type")
                    request.Content = new ByteArrayContent(jsonBytes);
                else if (header.Key == "host")
                    request.Headers.Host = header.Value;
                else if (header.Key != "content-length")
                    request.Headers.Add(header.Key, header.Value);
            }

            if (request.Content != null)
            {
                request.Content.Headers.ContentLength = jsonBytes.Length;
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            _logger.LogDebug("Creating secret in OCI Vault: {Key}", key);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"OCI create secret failed with status {response.StatusCode}: {errorContent}");
            }

            _logger.LogInformation("Successfully created secret {Key} in OCI Vault", key);
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
