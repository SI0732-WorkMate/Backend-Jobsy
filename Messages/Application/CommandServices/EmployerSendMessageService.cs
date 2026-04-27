using System.Security.Claims;
using Jobsy.Messages.Domain.Model.Aggregates;
using Jobsy.Messages.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Messages.Application.CommandServices;

public class EmployerSendMessageService : IRequestHandler<EmployerSendMessageCommand, string>

{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployerSendMessageService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }

    public async Task<string> Handle(EmployerSendMessageCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var senderId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(senderId) || role != "EMPLOYER")
            throw new UnauthorizedAccessException("Solo empleadores pueden enviar mensajes.");

        // Validar que el receiver (candidato) haya postulado a alguna oferta del employer
        var employerOfferIds = await _context.JobOffers
            .Where(o => o.employer_id == int.Parse(senderId))
            .Select(o => o.id)
            .ToListAsync(cancellationToken);

        var validApplication = await _context.Applications
            .AnyAsync(a => a.candidate_id == request.receiver_id && employerOfferIds.Contains(a.job_offer_id), cancellationToken);

        if (!validApplication)
            throw new UnauthorizedAccessException("No puedes contactar a un candidato que no ha postulado a tus ofertas.");

        var message = new Message
        {
            sender_id = int.Parse(senderId),
            receiver_id = request.receiver_id,
            content = request.content
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return message.id;
    }
}