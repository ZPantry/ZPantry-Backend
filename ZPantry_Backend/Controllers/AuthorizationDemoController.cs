using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZPantry_Backend.Controllers;

[ApiController]
[Route("api/demo")]
public class AuthorizationDemoController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        return Ok(new
        {
            Message = "Public endpoint - no token required."
        });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin-report")]
    public IActionResult AdminReport()
    {
        return Ok(new
        {
            Message = "Admin endpoint #1 - only admin can access."
        });
    }

    [Authorize(Roles = "admin")]
    [HttpPost("admin-sync")]
    public IActionResult AdminSync()
    {
        return Ok(new
        {
            Message = "Admin endpoint #2 - only admin can access."
        });
    }
}
