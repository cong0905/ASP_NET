using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Admin.DTOs;
using ShopNet.Application.Features.Admin.Queries;

namespace ShopNet.API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : ApiControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(Result<DashboardStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetDashboard()
    {
        var result = await Mediator.Send(new GetAdminDashboardQuery());
        return HandleResult(result);
    }
}
