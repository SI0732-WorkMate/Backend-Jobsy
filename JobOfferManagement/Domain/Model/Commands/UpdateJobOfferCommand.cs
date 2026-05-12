using Jobsy.Recruiter.JobOfferManagement.Domain.Model.ValueObjects;
using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;

public record UpdateJobOfferCommand(
    string id,
    string title,
    string description,
    string requirements,
    string location,
    decimal salary_range,
    Status status
) : IRequest<Unit>;