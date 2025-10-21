using System;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmartAlarm.Api.Services;

namespace SmartAlarm.Api.Tests.Testing;

/// <summary>
/// WebApplicationFactory customizada para testes de integração da API.
/// Permite substituir serviços, como o IMediator, por mocks.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // O Mock do Mediator é exposto para que os testes possam configurá-lo.
    public Mock<IMediator> MediatorMock { get; } = new();
    public Mock<ICurrentUserService> CurrentUserServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configura o ambiente para "Test"
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            // Substitui a implementação real do IMediator pelo nosso mock.
            services.AddSingleton(MediatorMock.Object);

            // Substitui a implementação real do ICurrentUserService por um mock.
            // Isso nos permite simular um usuário autenticado sem um token JWT real.
            services.AddSingleton(CurrentUserServiceMock.Object);
        });
    }

    /// <summary>
    /// Reseta os mocks para garantir o isolamento entre os testes.
    /// </summary>
    public void ResetMocks()
    {
        MediatorMock.Reset();
        CurrentUserServiceMock.Reset();
    }
}
