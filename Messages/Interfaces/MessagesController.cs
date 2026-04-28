using Jobsy.Messages.Domain.Model.Commands;
using Jobsy.Messages.Domain.Model.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Messages.Interfaces;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // La validación de rol EMPLOYER la hace EmployerSendMessageService internamente
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] EmployerSendMessageCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    // La validación de rol CANDIDATE la hace GetMyMessagesService internamente
    [Authorize]
    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox()
    {
        try
        {
            var messages = await _mediator.Send(new GetMyMessagesQuery());
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }
}