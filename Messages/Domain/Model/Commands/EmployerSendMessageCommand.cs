using MediatR;

namespace Jobsy.Messages.Domain.Model.Commands;

public record EmployerSendMessageCommand(int receiver_id, string content) : IRequest<string>;