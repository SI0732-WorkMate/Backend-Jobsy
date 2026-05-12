
using Jobsy.ApplicationManagement.Domain.Model.Aggregates;
using Jobsy.Messages.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Shared.Infrastructure.Persistencia.Configuration;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    public DbSet<User> Usuarios { get; set; }
    public DbSet<JobOffer> JobOffers { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .ToTable(tb => tb.HasTrigger("Trigger_Usuarios")); // el nombre puede ser ficticio

        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<JobOffer>(Entity =>
        {
            Entity.ToTable("job_offers");
        });
        
    }
    
}