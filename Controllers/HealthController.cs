using Microsoft.AspNetCore.Mvc;

namespace BurnAnalysisApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet("/healthz")]
        public IActionResult Get()
        {
            return Ok("Healthy");
        }
    }
}
