using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenEnds.BackEnd.Controllers;

[Authorize]
[Route("")]
public class LoginController : ControllerBase
{
    [HttpGet(nameof(Login))]
    public async Task<IActionResult> Login(string redirectUrl)
    {
        return new OkObjectResult(new { Message = "Login" });
    }

    [HttpGet(nameof(Logout))]
    public async Task<IActionResult> Logout()
    {
        return new OkObjectResult(new { Message = "Logout" });
    }
}