using Jobsy.JobsyAi.Application.CommandServices;
using Jobsy.JobsyAi.Domain.Model.Entities;
using Jobsy.JobsyAi.Domain.Model.ValueObjects;
using MediatR;

namespace Jobsy.JobsyAi.Application.Handlers;

public class StartChatHandler : IRequestHandler<StartChatCommand, Guid>
{
    // Aquí podrías inyectar un repositorio para persistir el chat
    public async Task<Guid> Handle(StartChatCommand request, CancellationToken cancellationToken)
    {
        var model = new AIModel(request.ModelName);
        var chat = new Chat(request.Title, model);

        // Aquí podrías guardar en base de datos si lo necesitas más adelante
        // await _chatRepository.SaveAsync(chat);

        return await Task.FromResult(chat.Id);
    }
}