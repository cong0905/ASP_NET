using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Auth.Commands;
using ShopNet.Application.Features.Auth.DTOs;
using ShopNet.Application.Features.Auth.Queries;

namespace ShopNet.API.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetMe()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return HandleResult(result);
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
