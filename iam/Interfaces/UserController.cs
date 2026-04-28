using System.IdentityModel.Tokens.Jwt;
using Jobsy.UserAuthentication.Domain.Exception;
using Jobsy.UserAuthentication.Domain.Model.Commands;
using Jobsy.UserAuthentication.Domain.Model.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.UserAuthentication.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ENDPOINT DE DIAGNÓSTICO TEMPORAL - muestra los claims del token tal como los ve el backend
    [Authorize]
    [HttpGet("debug-claims")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();
        var isInRoleCandidate = User.IsInRole("CANDIDATE");
        var isInRoleEmployer = User.IsInRole("EMPLOYER");
        return Ok(new {
            claims,
            isInRoleCandidate,
            isInRoleEmployer,
            identity_name = User.Identity?.Name,
            is_authenticated = User.Identity?.IsAuthenticated
        });
    }

    //registro
    [HttpPost]
    public async Task<IActionResult> RegistrarUsuario([FromBody] RegisterUserCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetUserById), new { id }, new { id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Token inválido." });

        var user = await _mediator.Send(new GetUserByIdQuery(int.Parse(userId)));

        if (user == null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(new
        {
            user.id,
            user.name,
            user.email,
            user.role,
            user.description,
            user.created_at
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        try
        {
            var token = await _mediator.Send(command);
            return Ok(new { token });
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        try
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}