using Microsoft.AspNetCore.Mvc;

namespace BurnAnalysisApp.Controllers
{
    [ApiController]
    [Route("/")]
    public class RootController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("✅ BurnAnalysis API is running!");
        }
    }
}
