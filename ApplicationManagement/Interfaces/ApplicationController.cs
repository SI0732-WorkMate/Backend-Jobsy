using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.ApplicationManagement.Domain.Model.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.ApplicationManagement.Interfaces;

[ApiController]
[Route("api/applications")]
public class ApplicationController : ControllerBase

{
    private readonly IMediator _mediator;

    public ApplicationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }
    
    [Authorize(Roles = "EMPLOYER")]
    [HttpGet("my-offers")]
    public async Task<IActionResult> GetMyApplications()
    {
        var result = await _mediator.Send(new GetApplicationsByEmployerQuery());
        return Ok(result);
    }
}