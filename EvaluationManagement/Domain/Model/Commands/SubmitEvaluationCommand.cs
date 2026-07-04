using MediatR;

namespace Jobsy.EvaluationManagement.Domain.Model.Commands;

public record AnswerInput(string scenario_id, string option_id);

public record SubmitEvaluationCommand(string application_id, List<AnswerInput> answers) : IRequest<object>;