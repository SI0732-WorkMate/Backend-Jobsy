using MediatR;

namespace Jobsy.ApplicationManagement.Domain.Model.Commands;

public record CreateApplicationCommand(string job_offer_id, string cv_url) : IRequest<string>;