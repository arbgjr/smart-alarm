using FluentValidation;
using MediatR;

namespace SmartAlarm.Application.Webhooks.Commands.DeleteWebhook
{
    /// <summary>
    /// Comando para deletar um webhook
    /// </summary>
    public class DeleteWebhookCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public DeleteWebhookCommand(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }

    /// <summary>
    /// Validador para o comando de deleção de webhook
    /// </summary>
    public class DeleteWebhookCommandValidator : AbstractValidator<DeleteWebhookCommand>
    {
        public DeleteWebhookCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID do webhook é obrigatório");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("ID do usuário é obrigatório");
        }
    }
}
