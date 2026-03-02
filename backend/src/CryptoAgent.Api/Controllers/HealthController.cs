using CryptoAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    /// <summary>
    /// Health check: verifies the API is running and the database is reachable.
    /// Returns 200 if healthy, 503 if the DB is unreachable.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool dbOk;
        string dbError = string.Empty;

        try
        {
            // CanConnectAsync is lightweight — doesn't open a full connection
            dbOk = await _db.Database.CanConnectAsync(ct);
        }
        catch (Exception ex)
        {
            dbOk = false;
            dbError = ex.Message;
        }

        var result = new
        {
            status    = dbOk ? "healthy" : "degraded",
            service   = "CryptoAgent API",
            version   = "1.0.0",
            timestamp = DateTimeOffset.UtcNow,
            components = new
            {
                database = new
                {
                    status = dbOk ? "ok" : "error",
                    error  = dbOk ? null : dbError
                }
            }
        };

        return dbOk ? Ok(result) : StatusCode(503, result);
    }
}
