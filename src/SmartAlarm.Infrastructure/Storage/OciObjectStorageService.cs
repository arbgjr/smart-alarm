using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;
using System.Globalization;
using SmartAlarm.Infrastructure.Observability;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Logging;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Requests;
using Oci.ObjectstorageService.Responses;
using Oci.Common.Auth;
using Oci.Common;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Implementação real do serviço de armazenamento OCI Object Storage
    /// Implementação completa para produção com Oracle OCI SDK oficial
    /// </summary>
    public class OciObjectStorageService : IStorageService, IDisposable
    {
        private readonly Lazy<ObjectStorageClient> _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OciObjectStorageService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly HttpClient _httpClient;
        private bool _disposed = false;
        
        private readonly string _namespace;
        private readonly string _bucketName;
        private readonly string _region;
        private readonly string _tenancyId;
        private readonly string _userId;
        private readonly string _fingerprint;
        private readonly string _privateKey;
        private readonly string _endpoint;

        public OciObjectStorageService(
            IConfiguration configuration,
            ILogger<OciObjectStorageService> logger,
            SmartAlarmMeter meter,
            SmartAlarmActivitySource activitySource,
            HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            _namespace = _configuration["OCI:ObjectStorage:Namespace"] 
                ?? throw new InvalidOperationException("OCI ObjectStorage Namespace não configurado");
            _bucketName = _configuration["OCI:ObjectStorage:BucketName"] 
                ?? throw new InvalidOperationException("OCI ObjectStorage BucketName não configurado");
            _region = _configuration["OCI:ObjectStorage:Region"] 
                ?? throw new InvalidOperationException("OCI ObjectStorage Region não configurado");
            _tenancyId = _configuration["OCI:TenancyId"] 
                ?? throw new InvalidOperationException("OCI TenancyId não configurado");
            _userId = _configuration["OCI:UserId"] 
                ?? throw new InvalidOperationException("OCI UserId não configurado");
            _fingerprint = _configuration["OCI:Fingerprint"] 
                ?? throw new InvalidOperationException("OCI Fingerprint não configurado");
            _privateKey = _configuration["OCI:PrivateKey"] 
                ?? throw new InvalidOperationException("OCI PrivateKey não configurado");
                
            _endpoint = $"https://objectstorage.{_region}.oraclecloud.com";
            _httpClient.BaseAddress = new Uri(_endpoint);
                
            // Implementação com Oracle OCI SDK oficial em lazy loading para performance
            _client = new Lazy<ObjectStorageClient>(() => CreateObjectStorageClient());
        }

        /// <summary>
        /// Cria cliente OCI ObjectStorage com autenticação baseada em chave privada
        /// </summary>
        private ObjectStorageClient CreateObjectStorageClient()
        {
            try
            {
                // Estratégia de autenticação com chave privada
                var authProvider = CreateAuthenticationProvider();
                var client = new ObjectStorageClient(authProvider);
                client.SetEndpoint($"https://objectstorage.{_region}.oraclecloud.com");
                
                _logger.LogInformation("Cliente OCI ObjectStorage criado com sucesso para região {Region}", _region);
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente OCI ObjectStorage");
                throw new InvalidOperationException("Falha ao inicializar cliente OCI ObjectStorage", ex);
            }
        }

        /// <summary>
        /// Cria provedor de autenticação para OCI
        /// Utiliza configuração padrão do OCI ou arquivo de configuração
        /// </summary>
        private IAuthenticationDetailsProvider CreateAuthenticationProvider()
        {
            try
            {
                // Usar o provedor de configuração padrão do OCI
                var configFileProvider = new ConfigFileAuthenticationDetailsProvider("DEFAULT");
                _logger.LogDebug("Provedor de autenticação OCI criado usando arquivo de configuração");
                return configFileProvider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar provedor de autenticação OCI");
                throw new InvalidOperationException("Falha ao configurar autenticação OCI. Verifique se o arquivo ~/.oci/config está configurado corretamente.", ex);
            }
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
        private Dictionary<string, string> CreateAuthHeaders(string method, string uri, string contentLength = "0", string contentType = "application/octet-stream")
        {
            var date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss GMT", CultureInfo.InvariantCulture);
            
            var headers = new Dictionary<string, string>
            {
                { "date", date },
                { "host", $"objectstorage.{_region}.oraclecloud.com" },
                { "content-length", contentLength },
                { "content-type", contentType }
            };

            var signature = CreateSignature(method, uri, headers);
            var keyId = $"{_tenancyId}/{_userId}/{_fingerprint}";
            var signingHeaders = string.Join(" ", headers.Keys.Prepend("(request-target)"));
            
            headers["authorization"] = $"Signature keyId=\"{keyId}\",algorithm=\"rsa-sha256\",headers=\"{signingHeaders}\",signature=\"{signature}\"";
            
            return headers;
        }

        public async Task UploadAsync(string path, Stream content)
        {
            using var activity = _activitySource.StartActivity("OCI.ObjectStorage.Upload");
            activity?.SetTag("storage.operation", "upload");
            activity?.SetTag("storage.provider", "oci");
            activity?.SetTag("storage.path", path);
            activity?.SetTag("storage.size", content.Length.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "OCIUpload",
                new { Path = path, Size = content.Length });

            try
            {
                // Implementação real com Oracle OCI SDK oficial
                var putObjectRequest = new PutObjectRequest
                {
                    NamespaceName = _namespace,
                    BucketName = _bucketName,
                    ObjectName = path,
                    PutObjectBody = content
                };
                
                var response = await _client.Value.PutObject(putObjectRequest);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "OCI", "Upload", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Upload",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("oci.etag", response.ETag);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "OCI", "UploadError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Upload",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw new InvalidOperationException($"Erro ao fazer upload do arquivo {path}", ex);
            }
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            using var activity = _activitySource.StartActivity("OCI.ObjectStorage.Download");
            activity?.SetTag("storage.operation", "download");
            activity?.SetTag("storage.provider", "oci");
            activity?.SetTag("storage.path", path);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "OCIDownload",
                new { Path = path });

            try
            {
                // Implementação real com Oracle OCI SDK oficial
                var getObjectRequest = new GetObjectRequest
                {
                    NamespaceName = _namespace,
                    BucketName = _bucketName,
                    ObjectName = path
                };
                
                var response = await _client.Value.GetObject(getObjectRequest);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "OCI", "Download", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Download",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetTag("storage.size", response.ContentLength.ToString());
                activity?.SetTag("oci.etag", response.ETag);
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                return response.InputStream;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "OCI", "DownloadError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Download",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw new InvalidOperationException($"Erro ao fazer download do arquivo {path}", ex);
            }
        }

        public async Task DeleteAsync(string path)
        {
            using var activity = _activitySource.StartActivity("OCI.ObjectStorage.Delete");
            activity?.SetTag("storage.operation", "delete");
            activity?.SetTag("storage.provider", "oci");
            activity?.SetTag("storage.path", path);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "OCIDelete",
                new { Path = path });

            try
            {
                // Implementação real com Oracle OCI SDK oficial
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    NamespaceName = _namespace,
                    BucketName = _bucketName,
                    ObjectName = path
                };
                
                await _client.Value.DeleteObject(deleteObjectRequest);

                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "OCI", "Delete", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Delete",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "OCI", "DeleteError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Delete",
                    path,
                    _bucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw new InvalidOperationException($"Erro ao deletar arquivo {path}", ex);
            }
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
                if (_client.IsValueCreated)
                {
                    _client.Value.Dispose();
                }
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
