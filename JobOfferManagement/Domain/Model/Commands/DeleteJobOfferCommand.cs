using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;

public record DeleteJobOfferCommand(string id) : IRequest<Unit>;