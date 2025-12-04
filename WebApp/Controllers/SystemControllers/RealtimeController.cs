using Common.System;
using Data.SystemAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;
using WebApp.Services.Realtime;

namespace WebApp.Controllers.SystemControllers;

[ApiController]
[Route("api/realtime")]
public sealed class RealtimeController(ISdk sdk, IConnectionTokenService tokenService) : ControllerBase
{
    [HttpPost("token")]
    [Authorize]
    public async Task<IActionResult> IssueToken()
    {
        var user =   sdk.CurrentUser;
        if (user == null)
        {
            return Unauthorized();
        }
        var token = tokenService.Generate(user.Id, user.Username);
        return Ok(new { token });
    }
}




