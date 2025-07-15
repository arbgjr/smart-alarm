namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Ações disponíveis para preferências de feriado.
    /// Define como os alarmes devem se comportar durante feriados específicos.
    /// </summary>
    public enum HolidayPreferenceAction
    {
        /// <summary>
        /// Desabilita os alarmes completamente durante o feriado
        /// </summary>
        Disable = 1,

        /// <summary>
        /// Atrasa os alarmes por um tempo específico durante o feriado
        /// </summary>
        Delay = 2,

        /// <summary>
        /// Pula os alarmes no feriado (não dispara, mas mantém programação normal no dia seguinte)
        /// </summary>
        Skip = 3
    }
}
