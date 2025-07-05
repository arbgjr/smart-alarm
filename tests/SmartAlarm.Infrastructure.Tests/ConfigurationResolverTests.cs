using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.KeyVault.Abstractions;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests
{
    public class ConfigurationResolverTests
    {
        [Fact]
        public async Task GetConfigAsync_Returns_Environment_Value_First()
        {
            Environment.SetEnvironmentVariable("TEST_KEY", "env_value");
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var keyVaultMock = new Mock<IKeyVaultService>();
            keyVaultMock.Setup(x => x.GetSecretAsync("TEST_KEY", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
            var loggerMock = new Mock<ILogger<ConfigurationResolver>>();
            var resolver = new ConfigurationResolver(config, keyVaultMock.Object, loggerMock.Object);
            var value = await resolver.GetConfigAsync("TEST_KEY");
            Assert.Equal("env_value", value);
            Environment.SetEnvironmentVariable("TEST_KEY", null);
        }

        [Fact]
        public async Task GetConfigAsync_Returns_Vault_Value_If_No_Env()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var keyVaultMock = new Mock<IKeyVaultService>();
            keyVaultMock.Setup(x => x.GetSecretAsync("TEST_KEY", It.IsAny<CancellationToken>())).ReturnsAsync("vault_value");
            var loggerMock = new Mock<ILogger<ConfigurationResolver>>();
            var resolver = new ConfigurationResolver(config, keyVaultMock.Object, loggerMock.Object);
            var value = await resolver.GetConfigAsync("TEST_KEY");
            Assert.Equal("vault_value", value);
        }

        [Fact]
        public async Task GetConfigAsync_Returns_AppSettings_Value_If_No_Env_Or_Vault()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string>("TEST_KEY", "appsettings_value") }).Build();
            var keyVaultMock = new Mock<IKeyVaultService>();
            keyVaultMock.Setup(x => x.GetSecretAsync("TEST_KEY", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
            var loggerMock = new Mock<ILogger<ConfigurationResolver>>();
            var resolver = new ConfigurationResolver(config, keyVaultMock.Object, loggerMock.Object);
            var value = await resolver.GetConfigAsync("TEST_KEY");
            Assert.Equal("appsettings_value", value);
        }

        [Fact]
        public async Task GetConfigAsync_Throws_If_Not_Found()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var keyVaultMock = new Mock<IKeyVaultService>();
            keyVaultMock.Setup(x => x.GetSecretAsync("TEST_KEY", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
            var loggerMock = new Mock<ILogger<ConfigurationResolver>>();
            var resolver = new ConfigurationResolver(config, keyVaultMock.Object, loggerMock.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.GetConfigAsync("TEST_KEY"));
        }
    }
}
