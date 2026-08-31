using Microsoft.AspNetCore.Mvc;

namespace FintechBackend.Api.Features.SystemStatus
{
    [ApiController]
    [Route("api/v1/system")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SystemStatusController: ControllerBase
    {
        [HttpGet("status")]
        public ActionResult<SystemStatusResponse> GetStatus()
        {
            return Ok(
                new SystemStatusResponse("Fintech Backend Lab", "Running"));
        }
    }
}
