using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Recruiter.JobOfferManagement.Interfaces;

[ApiController]
[Route("api/joboffers")]
public class JobOfferController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobOfferController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobOfferCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ofertas = await _mediator.Send(new GetAllJobOffersQuery());
        return Ok(ofertas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var oferta = await _mediator.Send(new GetJobOfferByIdQuery(id));
            return Ok(oferta);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateJobOfferCommand command)
    {
        var fullCommand = command with { id = id };
        await _mediator.Send(fullCommand);
        return NoContent();
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _mediator.Send(new DeleteJobOfferCommand(id));
        return NoContent();
    }
}