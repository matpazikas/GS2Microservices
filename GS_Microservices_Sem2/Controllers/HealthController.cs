using Microsoft.AspNetCore.Mvc;

namespace GS_Microservices_Sem2.Controllers
{
    [ApiController]
    [Route("api/[health]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealthStatus()
        {
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    }
}
