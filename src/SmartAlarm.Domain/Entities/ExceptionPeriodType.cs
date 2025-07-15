namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Tipos de períodos de exceção disponíveis no sistema.
    /// </summary>
    public enum ExceptionPeriodType
    {
        /// <summary>
        /// Férias - período em que alarmes podem ser desabilitados ou ter horários alterados
        /// </summary>
        Vacation = 1,

        /// <summary>
        /// Feriado - dia específico em que alarmes podem ter comportamento diferenciado
        /// </summary>
        Holiday = 2,

        /// <summary>
        /// Viagem - período em que alarmes podem ser ajustados para fuso horário diferente
        /// </summary>
        Travel = 3,

        /// <summary>
        /// Manutenção - período em que o sistema pode ter funcionalidades limitadas
        /// </summary>
        Maintenance = 4,

        /// <summary>
        /// Licença médica - período em que alarmes podem ser suspensos por motivos de saúde
        /// </summary>
        MedicalLeave = 5,

        /// <summary>
        /// Trabalho remoto - período com horários de trabalho diferentes
        /// </summary>
        RemoteWork = 6,

        /// <summary>
        /// Personalizado - período definido pelo usuário com características específicas
        /// </summary>
        Custom = 99
    }
}
