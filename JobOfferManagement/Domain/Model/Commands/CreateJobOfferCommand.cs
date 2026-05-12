using Jobsy.Recruiter.JobOfferManagement.Domain.Model.ValueObjects;
using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;

/// <summary>
/// Command to create a new job offer.
/// </summary>
public record CreateJobOfferCommand(
    string title,
    string description,
    string requirements,
    string location,
    decimal salary_range
) : IRequest<string>;
