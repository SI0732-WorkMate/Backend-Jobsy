using Jobsy.InterviewManagement.Domain.Model.Aggregates;
using Jobsy.Messages.Domain.Model.Aggregates;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.InterviewManagement.Application.Services;

public class InterviewReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _intervaloRevision = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _ventanaRecordatorio = TimeSpan.FromHours(24);

    public InterviewReminderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnviarRecordatoriosPendientesAsync(stoppingToken);
            }
            catch
            {
                // No detiene el servicio si una corrida falla
            }

            await Task.Delay(_intervaloRevision, stoppingToken);
        }
    }

    private async Task EnviarRecordatoriosPendientesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ahora = DateTime.UtcNow;
        var limite = ahora.Add(_ventanaRecordatorio);

        var pendientes = await context.Interviews
            .Where(i => i.status == "scheduled"
                        && !i.reminder_sent
                        && i.scheduled_at > ahora
                        && i.scheduled_at <= limite)
            .ToListAsync(stoppingToken);

        if (!pendientes.Any()) return;

        foreach (var interview in pendientes)
        {
            var application = await context.Applications
                .FirstOrDefaultAsync(a => a.id == interview.application_id, stoppingToken);
            var offer = application != null
                ? await context.JobOffers.FindAsync(new object[] { application.job_offer_id }, stoppingToken)
                : null;

            var tituloOferta = offer?.title ?? "tu proceso de selección";
            var contenido = $"Recordatorio: tienes una entrevista para \"{tituloOferta}\" el {interview.scheduled_at:dd/MM/yyyy} a las {interview.scheduled_at:HH:mm}.";

            context.Messages.Add(new Message
            {
                sender_id = interview.employer_id,
                receiver_id = interview.candidate_id,
                content = contenido
            });

            interview.reminder_sent = true;
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}