using System.Security.Claims;
using Jobsy.Messages.Domain.Model.Entities;
using Jobsy.Messages.Domain.Model.Queries;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Messages.Application.QueryServices;

public class GetMyMessagesService : IRequestHandler<GetMyMessagesQuery, IEnumerable<MessageDto>>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetMyMessagesService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }

    public async Task<IEnumerable<MessageDto>> Handle(GetMyMessagesQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var receiverId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(receiverId) || role != "CANDIDATE")
            throw new UnauthorizedAccessException("Solo candidatos pueden ver mensajes recibidos.");

        var messages = await _context.Messages
            .Where(m => m.receiver_id == int.Parse(receiverId))
            .OrderByDescending(m => m.sent_at)
            .Select(m => new MessageDto
            {
                id = m.id,
                sender_id = m.sender_id,
                content = m.content,
                sent_at = m.sent_at
            })
            .ToListAsync(cancellationToken);

        return messages;
    }
}