namespace SmartAlarm.Api.Services;

/// <summary>
/// Interface para serviço de rate limiting avançado
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Verifica se uma requisição deve ser limitada
    /// </summary>
    /// <param name="key">Chave de identificação (IP, usuário, etc.)</param>
    /// <param name="endpoint">Endpoint sendo acessado</param>
    /// <param name="requestType">Tipo de requisição (GET, POST, etc.)</param>
    /// <returns>True se deve ser limitada, false caso contrário</returns>
    Task<bool> ShouldRateLimitAsync(string key, string endpoint, string requestType);

    /// <summary>
    /// Obtém informações sobre o limite atual
    /// </summary>
    /// <param name="key">Chave de identificação</param>
    /// <param name="endpoint">Endpoint sendo acessado</param>
    /// <returns>Informações do rate limit</returns>
    Task<RateLimitInfo> GetRateLimitInfoAsync(string key, string endpoint);

    /// <summary>
    /// Registra uma tentativa de acesso suspeita
    /// </summary>
    /// <param name="key">Chave de identificação</param>
    /// <param name="endpoint">Endpoint acessado</param>
    /// <param name="reason">Motivo da suspeita</param>
    Task LogSuspiciousActivityAsync(string key, string endpoint, string reason);
}

/// <summary>
/// Informações sobre o rate limit atual
/// </summary>
public class RateLimitInfo
{
    public int RequestsRemaining { get; set; }
    public int TotalRequests { get; set; }
    public TimeSpan ResetTime { get; set; }
    public bool IsLimited { get; set; }
    public string? RetryAfter { get; set; }
}

/// <summary>
/// Configurações de rate limiting por endpoint
/// </summary>
public class RateLimitConfiguration
{
    public Dictionary<string, EndpointRateLimit> Endpoints { get; set; } = new();
    public GlobalRateLimit Global { get; set; } = new();
    public SecurityRateLimit Security { get; set; } = new();
}

public class EndpointRateLimit
{
    public int RequestsPerMinute { get; set; }
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
    public bool EnableBurstProtection { get; set; }
    public int BurstLimit { get; set; }
}

public class GlobalRateLimit
{
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public bool EnableIpBlocking { get; set; } = true;
    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(15);
}

public class SecurityRateLimit
{
    public int LoginAttemptsPerMinute { get; set; } = 5;
    public int LoginAttemptsPerHour { get; set; } = 20;
    public TimeSpan LoginBlockDuration { get; set; } = TimeSpan.FromMinutes(30);
    public int PasswordResetAttemptsPerHour { get; set; } = 3;
    public int RegistrationAttemptsPerHour { get; set; } = 10;
}
