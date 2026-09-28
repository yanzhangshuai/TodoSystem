using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TodoSystem.Web.Controllers;


[ApiController]
[AllowAnonymous]
[Route($"[controller]")]
public class AuthController:ControllerBase
{
    [HttpGet("callback/google")]
    public async Task GoogleCallbackAsync()
    {
        
    }
}