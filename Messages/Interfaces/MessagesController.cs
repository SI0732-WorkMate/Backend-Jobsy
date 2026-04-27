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

    [Authorize(Roles = "EMPLOYER")]
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] EmployerSendMessageCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }
    
    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox()
    {
        var messages = await _mediator.Send(new GetMyMessagesQuery());
        return Ok(messages);
    }
    
}