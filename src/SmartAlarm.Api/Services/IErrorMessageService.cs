namespace SmartAlarm.Api.Services
{
    /// <summary>
    /// Serviço para obter mensagens de erro centralizadas.
    /// </summary>
    public interface IErrorMessageService
    {
        /// <summary>
        /// Obtém uma mensagem de erro pelo caminho da chave.
        /// </summary>
        /// <param name="keyPath">Caminho da chave no formato "categoria.subcategoria.chave"</param>
        /// <param name="parameters">Parâmetros para interpolação na mensagem</param>
        /// <returns>Mensagem de erro formatada</returns>
        string GetMessage(string keyPath, params object[] parameters);

        /// <summary>
        /// Verifica se uma chave de mensagem existe.
        /// </summary>
        /// <param name="keyPath">Caminho da chave</param>
        /// <returns>True se a chave existe</returns>
        bool HasMessage(string keyPath);
    }
}