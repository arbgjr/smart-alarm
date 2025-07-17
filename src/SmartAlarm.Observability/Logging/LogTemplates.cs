using System;

namespace SmartAlarm.Observability.Logging
{
    /// <summary>
    /// Templates padronizados para logs estruturados no Smart Alarm
    /// </summary>
    public static class LogTemplates
    {
        #region Command Templates

        /// <summary>
        /// Template para início de comando
        /// </summary>
        public const string CommandStarted = "Command {CommandName} started by {UserId} with CorrelationId {CorrelationId}";

        /// <summary>
        /// Template para conclusão de comando
        /// </summary>
        public const string CommandCompleted = "Command {CommandName} completed in {Duration}ms with result {Result}";

        /// <summary>
        /// Template para falha de comando
        /// </summary>
        public const string CommandFailed = "Command {CommandName} failed after {Duration}ms with error {ErrorMessage}";

        #endregion

        #region Query Templates

        /// <summary>
        /// Template para início de query
        /// </summary>
        public const string QueryStarted = "Query {QueryName} started with parameters {@Parameters}";

        /// <summary>
        /// Template para conclusão de query
        /// </summary>
        public const string QueryCompleted = "Query {QueryName} completed in {Duration}ms returning {ResultCount} items";

        /// <summary>
        /// Template para falha de query
        /// </summary>
        public const string QueryFailed = "Query {QueryName} failed after {Duration}ms with error {ErrorMessage}";

        #endregion

        #region Business Event Templates

        /// <summary>
        /// Template para criação de alarme
        /// </summary>
        public const string AlarmCreated = "Alarm {AlarmId} created for user {UserId} with name '{AlarmName}' scheduled for {ScheduledTime}";

        /// <summary>
        /// Template para disparo de alarme
        /// </summary>
        public const string AlarmTriggered = "Alarm {AlarmId} triggered at {TriggerTime} for user {UserId}";

        /// <summary>
        /// Template para desativação de alarme
        /// </summary>
        public const string AlarmDeactivated = "Alarm {AlarmId} deactivated by user {UserId} at {DeactivationTime}";

        /// <summary>
        /// Template para falha no disparo de alarme
        /// </summary>
        public const string AlarmTriggerFailed = "Alarm {AlarmId} failed to trigger at {ScheduledTime} with error {ErrorMessage}";

        /// <summary>
        /// Template para registro de usuário
        /// </summary>
        public const string UserRegistered = "User {UserId} registered with email {Email} at {RegistrationTime}";

        /// <summary>
        /// Template para login de usuário
        /// </summary>
        public const string UserLoggedIn = "User {UserId} logged in from {IPAddress} at {LoginTime}";

        /// <summary>
        /// Template para logout de usuário
        /// </summary>
        public const string UserLoggedOut = "User {UserId} logged out at {LogoutTime} after session duration {SessionDuration}";

        /// <summary>
        /// Template para evento de negócio genérico
        /// </summary>
        public const string BusinessEventOccurred = "Business event {EventName} occurred with data {@EventData} and CorrelationId {CorrelationId}";

        /// <summary>
        /// Template para entidade não encontrada
        /// </summary>
        public const string EntityNotFound = "Entity {EntityType} with ID {EntityId} not found in CorrelationId {CorrelationId}";

        #endregion

        #region Infrastructure Templates

        /// <summary>
        /// Template para conexão com banco de dados
        /// </summary>
        public const string DatabaseConnectionEstablished = "Database connection established to {ConnectionString} in {Duration}ms";

        /// <summary>
        /// Template para falha de conexão com banco
        /// </summary>
        public const string DatabaseConnectionFailed = "Database connection failed to {ConnectionString} after {Duration}ms with error {ErrorMessage}";

        /// <summary>
        /// Template para execução de query SQL
        /// </summary>
        public const string DatabaseQueryExecuted = "Database query executed: {Query} in {Duration}ms affecting {RowsAffected} rows";

        /// <summary>
        /// Template para chamada de serviço externo
        /// </summary>
        public const string ExternalServiceCall = "External service {ServiceName} called with {Method} {Endpoint} returning {StatusCode} in {Duration}ms";

        /// <summary>
        /// Template para falha em serviço externo
        /// </summary>
        public const string ExternalServiceCallFailed = "External service {ServiceName} call failed to {Endpoint} after {Duration}ms with error {ErrorMessage}";

        /// <summary>
        /// Template para operação de storage
        /// </summary>
        public const string StorageOperationCompleted = "Storage operation {Operation} completed for {FileName} in bucket {BucketName} in {Duration}ms";

        /// <summary>
        /// Template para falha em storage
        /// </summary>
        public const string StorageOperationFailed = "Storage operation {Operation} failed for {FileName} in bucket {BucketName} after {Duration}ms with error {ErrorMessage}";

        /// <summary>
        /// Template para operação de KeyVault
        /// </summary>
        public const string KeyVaultOperationCompleted = "KeyVault operation {Operation} completed for secret {SecretName} in {Duration}ms";

        /// <summary>
        /// Template para falha em KeyVault
        /// </summary>
        public const string KeyVaultOperationFailed = "KeyVault operation {Operation} failed for secret {SecretName} after {Duration}ms with error {ErrorMessage}";

        #endregion

        #region API Templates

        /// <summary>
        /// Template para request HTTP
        /// </summary>
        public const string HttpRequestReceived = "HTTP {Method} request received to {Path} from {RemoteIpAddress} with CorrelationId {CorrelationId}";

        /// <summary>
        /// Template para response HTTP
        /// </summary>
        public const string HttpResponseSent = "HTTP {Method} request to {Path} responded with {StatusCode} in {Duration}ms";

        /// <summary>
        /// Template para erro HTTP
        /// </summary>
        public const string HttpRequestFailed = "HTTP {Method} request to {Path} failed with {StatusCode} in {Duration}ms: {ErrorMessage}";

        /// <summary>
        /// Template para validação falhada
        /// </summary>
        public const string ValidationFailed = "Validation failed for {RequestType} with errors: {@ValidationErrors}";

        /// <summary>
        /// Template para rate limiting
        /// </summary>
        public const string RateLimitExceeded = "Rate limit exceeded for {UserId} on endpoint {Endpoint} from {IPAddress}";

        #endregion

        #region Security Templates

        /// <summary>
        /// Template para tentativa de autenticação
        /// </summary>
        public const string AuthenticationAttempt = "Authentication attempt for user {UserId} from {IPAddress} at {AttemptTime}";

        /// <summary>
        /// Template para falha de autenticação
        /// </summary>
        public const string AuthenticationFailed = "Authentication failed for user {UserId} from {IPAddress} with reason {FailureReason}";

        /// <summary>
        /// Template para acesso negado
        /// </summary>
        public const string AccessDenied = "Access denied for user {UserId} to resource {Resource} with action {Action}";

        /// <summary>
        /// Template para token expirado
        /// </summary>
        public const string TokenExpired = "Token expired for user {UserId} at {ExpirationTime}";

        #endregion

        #region Performance Templates

        /// <summary>
        /// Template para operação lenta
        /// </summary>
        public const string SlowOperation = "Slow operation detected: {OperationName} took {Duration}ms (threshold: {ThresholdMs}ms)";

        /// <summary>
        /// Template para alto uso de memória
        /// </summary>
        public const string HighMemoryUsage = "High memory usage detected: {WorkingSet}MB (threshold: {ThresholdMB}MB)";

        /// <summary>
        /// Template para alto uso de CPU
        /// </summary>
        public const string HighCpuUsage = "High CPU usage detected: {CpuUsage}% (threshold: {ThresholdPercent}%)";

        #endregion

        #region Message Queue Templates

        /// <summary>
        /// Template para publicação de mensagem
        /// </summary>
        public const string MessagePublished = "Message {MessageId} of type {MessageType} published to {QueueName} at {PublishTime}";

        /// <summary>
        /// Template para consumo de mensagem
        /// </summary>
        public const string MessageConsumed = "Message {MessageId} of type {MessageType} consumed from {QueueName} in {ProcessingDuration}ms";

        /// <summary>
        /// Template para falha no processamento de mensagem
        /// </summary>
        public const string MessageProcessingFailed = "Message {MessageId} processing failed after {Duration}ms with error {ErrorMessage}";

        #endregion

        #region Integration Templates

        /// <summary>
        /// Template para integração de IA
        /// </summary>
        public const string AiServiceCall = "AI service {ServiceName} called with request {RequestType} returning response in {Duration}ms";

        /// <summary>
        /// Template para processamento de arquivo
        /// </summary>
        public const string FileProcessed = "File {FileName} processed successfully in {Duration}ms with size {FileSize} bytes";

        /// <summary>
        /// Template para importação de dados
        /// </summary>
        public const string DataImported = "Data import completed for {DataType} with {RecordCount} records in {Duration}ms";

        #endregion
    }
}
