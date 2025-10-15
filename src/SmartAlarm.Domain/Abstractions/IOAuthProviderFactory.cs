namespace SmartAlarm.Domain.Abstractions;

/// <summary>
/// Factory para criar provedores OAuth2 específicos
/// </summary>
public interface IOAuthProviderFactory
{
    /// <summary>
    /// Cria um provedor OAuth2 específico
    /// </summary>
    /// <param name="providerName">Nome do provedor (Google, GitHub, Facebook, Microsoft)</param>
    /// <returns>Provedor OAuth2 correspondente</returns>
    /// <exception cref="ArgumentException">Quando o provedor não é suportado</exception>
    IOAuthProvider CreateProvider(string providerName);

    /// <summary>
    /// Lista todos os provedores suportados
    /// </summary>
    /// <returns>Lista de nomes de provedores suportados</returns>
    IEnumerable<string> GetSupportedProviders();

    /// <summary>
    /// Verifica se um provedor é suportado
    /// </summary>
    /// <param name="providerName">Nome do provedor</param>
    /// <returns>True se o provedor é suportado</returns>
    bool IsProviderSupported(string providerName);
}