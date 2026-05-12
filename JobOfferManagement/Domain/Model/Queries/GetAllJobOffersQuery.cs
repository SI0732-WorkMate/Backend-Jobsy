using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Queries;

/// <summary>
/// Query to retrieve all job offers.
/// </summary>
public record GetAllJobOffersQuery : IRequest<IEnumerable<JobOffer>>;