using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Domain.Abstractions;
using DomainUser = SmartAlarm.Domain.Entities.User;

namespace SmartAlarm.Application.Handlers.Auth;

/// <summary>
/// Handler para login tradicional
/// </summary>
public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<LoginHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing login request for email: {Email}", request.Email);

            // Buscar usuário por email
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt failed - user not found or inactive: {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Credenciais inválidas",
                    Errors = new[] { "Email ou senha incorretos" }
                };
            }

            // Verificar senha
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt failed - invalid password for user: {UserId}", user.Id);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Credenciais inválidas",
                    Errors = new[] { "Email ou senha incorretos" }
                };
            }

            // Obter roles do usuário
            var roles = user.UserRoles?
                .Where(ur => ur.IsValid())
                .Select(ur => ur.Role.Name)
                .ToArray() ?? Array.Empty<string>();

            // Gerar tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user);

            // Atualizar último login
            user.RecordLogin();
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Login successful for user: {UserId}", user.Id);

            return new AuthResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name.ToString(),
                    Email = user.Email.ToString(),
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    Roles = roles
                },
                Message = "Login realizado com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login for email: {Email}", request.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new[] { "Tente novamente mais tarde" }
            };
        }
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            // Priorizar BCrypt para verificação segura (senhas novas)
            if (hashedPassword.StartsWith("$2"))  // BCrypt hash sempre começa com $2a, $2b, $2x ou $2y
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
        }
        catch (Exception ex)
        {
            // Log do erro, mas continue para fallback
            System.Diagnostics.Debug.WriteLine($"BCrypt verification failed: {ex.Message}");
        }
        
        try
        {
            // Fallback para SHA256 para compatibilidade com senhas existentes
            using var sha256 = SHA256.Create();
            var hashedInput = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return hashedInput == hashedPassword;
        }
        catch
        {
            return false; // Se ambos falharem, senha inválida
        }
    }
}

/// <summary>
/// Handler para registro de usuário
/// </summary>
public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<RegisterHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing registration request for email: {Email}", request.Email);

            // Verificar se usuário já existe
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt failed - email already exists: {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email já está em uso",
                    Errors = new[] { "Este email já está cadastrado" }
                };
            }

            // Criar usuário
            var name = new Name(request.Name);
            var email = new Email(request.Email);
            var user = new DomainUser(Guid.NewGuid(), name, email);

            // Hash da senha
            user.SetPasswordHash(HashPassword(request.Password));

            // Salvar usuário
            await _userRepository.AddAsync(user);

            // Gerar tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user);

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            return new AuthResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name.ToString(),
                    Email = user.Email.ToString(),
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    Roles = new[] { "User" }
                },
                Message = "Usuário registrado com sucesso"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration for email: {Email}", request.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new[] { "Tente novamente mais tarde" }
            };
        }
    }

    private static string HashPassword(string password)
    {
        // Usar BCrypt para hashing seguro de senhas em produção
        // BCrypt inclui salt automático e é resistente a ataques de força bruta
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
}

/// <summary>
/// Handler para refresh token
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IJwtTokenService jwtTokenService,
        IUserRepository userRepository,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing refresh token request");

            // Validar o refresh token
            var isValid = await _jwtTokenService.ValidateTokenAsync(request.RefreshToken);
            if (!isValid)
            {
                _logger.LogWarning("Invalid refresh token provided");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Obter o usuário do token
            var userId = await _jwtTokenService.GetUserIdFromTokenAsync(request.RefreshToken);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unable to extract user ID from refresh token");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive: {UserId}", userId.Value);
                throw new UnauthorizedAccessException("User not found or inactive");
            }

            // Gerar novos tokens
            var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user);

            var expiresAt = DateTime.UtcNow.AddHours(1); // 1 hora para o access token

            _logger.LogInformation("Refresh token successful for user: {UserId}", user.Id);

            return new AuthResponseDto
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                Message = "Token refreshed successfully"
            };
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "Error processing refresh token request");
            throw;
        }
    }
}
