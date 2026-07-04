using MediatR;

namespace Jobsy.ApplicationManagement.Domain.Model.Commands;

public record CalculateMatchScoreCommand(string application_id) : IRequest<bool>;