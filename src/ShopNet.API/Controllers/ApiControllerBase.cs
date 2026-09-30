using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;

namespace ShopNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        return result.StatusCode switch
        {
            201 => StatusCode(201, result),
            204 => NoContent(),
            400 => BadRequest(result),
            401 => Unauthorized(result),
            403 => Forbid(),
            404 => NotFound(result),
            _ => Ok(result)
        };
    }
}
