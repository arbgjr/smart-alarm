using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa as preferências de um usuário para feriados específicos.
    /// Define como alarmes devem se comportar durante feriados específicos.
    /// </summary>
    public class UserHolidayPreference
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid HolidayId { get; private set; }
        public bool IsEnabled { get; private set; }
        public HolidayPreferenceAction Action { get; private set; }
        public int? DelayInMinutes { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation properties
        public virtual User User { get; private set; } = null!;
        public virtual Holiday Holiday { get; private set; } = null!;

        // Private constructor for EF Core
        private UserHolidayPreference() { }

        /// <summary>
        /// Cria uma nova preferência de feriado para um usuário.
        /// </summary>
        /// <param name="id">ID da preferência (Guid.Empty para gerar novo)</param>
        /// <param name="userId">ID do usuário</param>
        /// <param name="holidayId">ID do feriado</param>
        /// <param name="isEnabled">Se a preferência está ativa</param>
        /// <param name="action">Ação a ser tomada no feriado</param>
        /// <param name="delayInMinutes">Atraso em minutos (opcional, usado com Delay action)</param>
        /// <exception cref="ArgumentException">Quando os parâmetros são inválidos</exception>
        public UserHolidayPreference(Guid id, Guid userId, Guid holidayId, bool isEnabled,
            HolidayPreferenceAction action, int? delayInMinutes = null)
        {
            ValidateParameters(userId, holidayId, action, delayInMinutes);

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            UserId = userId;
            HolidayId = holidayId;
            IsEnabled = isEnabled;
            Action = action;
            DelayInMinutes = delayInMinutes;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ativa a preferência de feriado.
        /// </summary>
        public void Enable()
        {
            IsEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Desativa a preferência de feriado.
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza a ação e o atraso da preferência.
        /// </summary>
        /// <param name="action">Nova ação</param>
        /// <param name="delayInMinutes">Novo atraso em minutos (opcional)</param>
        public void UpdateAction(HolidayPreferenceAction action, int? delayInMinutes = null)
        {
            ValidateActionAndDelay(action, delayInMinutes);

            Action = action;
            DelayInMinutes = delayInMinutes;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica se a preferência é aplicável para uma data específica.
        /// </summary>
        /// <param name="date">Data a verificar</param>
        /// <param name="holiday">Feriado a verificar</param>
        /// <returns>True se a preferência se aplica</returns>
        public bool IsApplicableForDate(DateTime date, Holiday holiday)
        {
            if (!IsEnabled) return false;
            if (holiday.Id != HolidayId) return false;

            return holiday.IsOnDate(date);
        }

        /// <summary>
        /// Calcula o atraso efetivo baseado na ação configurada.
        /// </summary>
        /// <returns>Atraso em minutos (0 para Disable, valor configurado para Delay)</returns>
        public int GetEffectiveDelayInMinutes()
        {
            return Action switch
            {
                HolidayPreferenceAction.Disable => 0,
                HolidayPreferenceAction.Delay => DelayInMinutes ?? 0,
                HolidayPreferenceAction.Skip => 0,
                _ => 0
            };
        }

        #region Private Validation Methods

        private static void ValidateParameters(Guid userId, Guid holidayId,
            HolidayPreferenceAction action, int? delayInMinutes)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId não pode ser vazio.", nameof(userId));

            if (holidayId == Guid.Empty)
                throw new ArgumentException("HolidayId não pode ser vazio.", nameof(holidayId));

            ValidateActionAndDelay(action, delayInMinutes);
        }

        private static void ValidateActionAndDelay(HolidayPreferenceAction action, int? delayInMinutes)
        {
            if (!Enum.IsDefined(typeof(HolidayPreferenceAction), action))
                throw new ArgumentException("Action deve ser um valor válido.", nameof(action));

            if (action == HolidayPreferenceAction.Delay)
            {
                if (!delayInMinutes.HasValue)
                    throw new ArgumentException("DelayInMinutes é obrigatório quando Action é Delay.", nameof(delayInMinutes));

                if (delayInMinutes.Value <= 0)
                    throw new ArgumentException("DelayInMinutes deve ser maior que zero.", nameof(delayInMinutes));

                if (delayInMinutes.Value > 1440) // 24 horas
                    throw new ArgumentException("DelayInMinutes não pode ser maior que 1440 (24 horas).", nameof(delayInMinutes));
            }
            else if (delayInMinutes.HasValue && delayInMinutes.Value != 0)
            {
                throw new ArgumentException("DelayInMinutes só deve ser especificado quando Action é Delay.", nameof(delayInMinutes));
            }
        }

        #endregion
    }
}
