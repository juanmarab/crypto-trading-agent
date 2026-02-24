using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "CryptoAgent API",
        timestamp = DateTimeOffset.UtcNow
    });
}
