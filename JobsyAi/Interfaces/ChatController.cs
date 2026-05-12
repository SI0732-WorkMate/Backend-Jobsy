using Jobsy.JobsyAi.Application.CommandServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.JobsyAi.Interfaces;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost("start")]
    public async Task<IActionResult> StartChat([FromBody] StartChatCommand command)
    {
        var chatId = await _mediator.Send(command);
        return Ok(new { chatId });
    }
    
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(new { response });
    }
}