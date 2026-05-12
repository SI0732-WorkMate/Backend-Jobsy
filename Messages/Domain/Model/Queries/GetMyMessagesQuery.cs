using Jobsy.Messages.Domain.Model.Entities;
using MediatR;

namespace Jobsy.Messages.Domain.Model.Queries;

public record GetMyMessagesQuery() : IRequest<IEnumerable<MessageDto>>;