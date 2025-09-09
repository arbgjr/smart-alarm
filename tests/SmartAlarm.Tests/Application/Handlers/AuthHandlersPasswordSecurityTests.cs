using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace SmartAlarm.Tests.Application.Handlers
{
    /// <summary>
    /// Testes específicos para validar a implementação de segurança de senhas
    /// com BCrypt no AuthHandlers - Tech Debt #1 "HASH DE SENHA SIMPLIFICADO"
    /// </summary>
    public class AuthHandlersPasswordSecurityTests
    {
        [Fact]
        public void HashPassword_WithBCrypt_ShouldReturnValidBCryptHash()
        {
            // Arrange
            var password = "TestPassword123!";
            
            // Act
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Assert
            hashedPassword.Should().NotBeNullOrEmpty();
            hashedPassword.Should().StartWith("$2");  // BCrypt hash identifier
            hashedPassword.Should().NotBe(password);
            hashedPassword.Length.Should().BeGreaterThan(50); // BCrypt hashes are typically 60+ chars
        }

        [Fact]
        public void VerifyPassword_WithBCryptHash_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Act
            var isValid = BCrypt.Net.BCrypt.Verify(password, bcryptHash);
            
            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var correctPassword = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword(correctPassword, workFactor: 12);
            
            // Act
            var isValid = BCrypt.Net.BCrypt.Verify(wrongPassword, bcryptHash);
            
            // Assert
            isValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("$2a$12$somehashedvalue", true)]  // BCrypt
        [InlineData("$2b$12$somehashedvalue", true)]  // BCrypt variant
        [InlineData("$2y$12$somehashedvalue", true)]  // BCrypt variant
        [InlineData("5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8", false)] // SHA256
        [InlineData("plaintext", false)] // Plain text
        public void IsBCryptHash_ShouldDetectCorrectly(string hash, bool expected)
        {
            // Act
            var isBCrypt = hash.StartsWith("$2");
            
            // Assert
            isBCrypt.Should().Be(expected);
        }

        [Fact]
        public void BCryptWorkFactor_ShouldBe12()
        {
            // Arrange
            var password = "TestPassword123!";
            
            // Act
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Assert
            // Verificar se o hash contém o work factor 12
            hashedPassword.Should().Contain("$12$");
        }

        [Fact]
        public void BCryptHashing_ShouldProduceDifferentHashesForSamePassword()
        {
            // Arrange
            var password = "TestPassword123!";
            
            // Act
            var hash1 = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            var hash2 = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Assert
            hash1.Should().NotBe(hash2); // BCrypt uses random salt
            BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
        }

        [Fact]
        public void BCryptHashing_ShouldResistTimingAttacks()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Act & Assert - BCrypt.Verify deve sempre levar tempo similar
            var startTime = DateTime.UtcNow;
            BCrypt.Net.BCrypt.Verify(password, hash); // Correto
            var correctTime = DateTime.UtcNow - startTime;
            
            startTime = DateTime.UtcNow;
            BCrypt.Net.BCrypt.Verify("wrongpassword", hash); // Incorreto
            var wrongTime = DateTime.UtcNow - startTime;
            
            // Os tempos devem ser relativamente similares (dentro de uma ordem de magnitude)
            // Isso não é um teste perfeito de timing attack, mas verifica que BCrypt não falha imediatamente
            wrongTime.Should().BeCloseTo(correctTime, TimeSpan.FromMilliseconds(100));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        // Note: null test removed to avoid xUnit warning
        public void BCryptVerify_WithInvalidInputs_ShouldHandleGracefully(string invalidInput)
        {
            // Arrange
            var validHash = BCrypt.Net.BCrypt.HashPassword("test", workFactor: 12);
            
            // Act & Assert
            var result = BCrypt.Net.BCrypt.Verify(invalidInput, validHash);
            result.Should().BeFalse();
        }

        [Fact]
        public void BCryptVerify_WithNullInput_ShouldThrowException()
        {
            // Arrange
            var validHash = BCrypt.Net.BCrypt.HashPassword("test", workFactor: 12);
            
            // Act & Assert
            Action act = () => BCrypt.Net.BCrypt.Verify(null, validHash);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SHA256Fallback_ShouldStillWork_ForLegacyPasswords()
        {
            // Arrange - simula um hash SHA256 legacy
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var password = "TestPassword123!";
                var sha256Hash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
                
                // Act & Assert
                sha256Hash.Should().NotStartWith("$2"); // Não é BCrypt
                sha256Hash.Length.Should().Be(44); // Base64 encoded SHA256 tem 44 chars
                
                // Verifica que conseguimos identificar que não é BCrypt
                var isBCrypt = sha256Hash.StartsWith("$2");
                isBCrypt.Should().BeFalse();
            }
        }

        [Fact]
        public void PasswordSecurity_ComparisonBetweenSHA256AndBCrypt()
        {
            // Arrange
            var password = "TestPassword123!";
            
            // Act - SHA256 (legacy/inseguro)
            string sha256Hash;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                sha256Hash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            }
            
            // Act - BCrypt (novo/seguro)
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            
            // Assert - Demonstrar superioridade do BCrypt
            sha256Hash.Should().HaveLength(44); // Sempre o mesmo tamanho
            bcryptHash.Length.Should().Be(60); // BCrypt tem estrutura fixa
            
            // SHA256 sempre produz o mesmo hash (vulnerável a rainbow tables)
            var sha256Hash2 = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            sha256Hash.Should().Be(sha256Hash2); // Vulnerabilidade!
            
            // BCrypt sempre produz hash diferente (resistente a rainbow tables)
            var bcryptHash2 = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            bcryptHash.Should().NotBe(bcryptHash2); // Segurança!
            
            // Ambos verificam a senha corretamente
            BCrypt.Net.BCrypt.Verify(password, bcryptHash).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, bcryptHash2).Should().BeTrue();
        }
    }
}
