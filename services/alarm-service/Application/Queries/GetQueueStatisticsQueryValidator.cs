using FluentValidation;

namespace SmartAlarm.AlarmService.Application.Queries
{
    /// <summary>
    /// Validator para query de estatísticas da fila
    /// </summary>
    public class GetQueueStatisticsQueryValidator : AbstractValidator<GetQueueStatisticsQuery>
    {
        public GetQueueStatisticsQueryValidator()
        {
            RuleFor(x => x.QueueName)
                .NotEmpty()
                .Length(1, 100)
                .Matches("^[a-zA-Z0-9_-]+$")
                .WithMessage("QueueName deve conter apenas letras, números, hífens e underscores");
        }
    }
}
