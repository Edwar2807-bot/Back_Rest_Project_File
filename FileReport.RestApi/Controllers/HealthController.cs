using Microsoft.AspNetCore.Mvc;

namespace FileReport.RestApi.Controllers
{
    [ApiController]
    [Route("api/v1/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Health()
            => Ok(new { status = "UP" });
    }
}
