using Jobsy.ApplicationManagement.Domain.Model.Entities;
using MediatR;

namespace Jobsy.ApplicationManagement.Domain.Model.Queries;

public record GetApplicationsByEmployerQuery() : IRequest<IEnumerable<ApplicationSummaryDto>>;