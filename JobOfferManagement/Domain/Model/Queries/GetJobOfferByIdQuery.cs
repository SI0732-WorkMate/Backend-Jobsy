using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Queries;

/// <summary>
/// Query to retrieve a job offer by its ID.
/// </summary>
/// <param name="id">The ID of the job offer</param>
public record GetJobOfferByIdQuery(string id) : IRequest<JobOffer>;