using Microsoft.AspNetCore.Mvc;
using WhackerLinkLib.Interfaces;
using System.IO;

namespace WhackerLinkServer.RestApi.Controllers
{
    [ApiController]
    [Route("api/masters")]
    public class RemoveMasterController : ControllerBase
    {
        private readonly IMasterServiceRegistry _registry;

        public RemoveMasterController(IMasterServiceRegistry registry)
        {
            _registry = registry;
        }

        [HttpPost("{masterName}/remove")]
        public IActionResult DisableMaster([FromRoute] string masterName)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            // Stop and remove the master
            // Reflect success or failure in the status code
            bool success = Program.RemoveMaster(masterName);
            if (!success)
            {
                return StatusCode(500, new { error = $"Failed to stop and remove Master {masterName}." });
            }

            return Ok(new { success = true, message = $"Master '{masterName}' has been stopped and removed." });
        }
    }
}
