using MediatR;

namespace Jobsy.InterviewManagement.Domain.Model.Commands;

public record ScheduleInterviewCommand(
    string application_id,
    DateTime scheduled_at,
    int duration_minutes,
    string? notes
) : IRequest<string>;