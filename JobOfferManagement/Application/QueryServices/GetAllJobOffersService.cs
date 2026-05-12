using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Queries;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Recruiter.JobOfferManagement.Application.QueryServices;

public class GetAllJobOffersService : IRequestHandler<GetAllJobOffersQuery, IEnumerable<JobOffer>>
{
    private readonly AppDbContext _context;

    public GetAllJobOffersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobOffer>> Handle(GetAllJobOffersQuery request, CancellationToken cancellationToken)
    {
        return await _context.JobOffers.ToListAsync(cancellationToken);
    }
}