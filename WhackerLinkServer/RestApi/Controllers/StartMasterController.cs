using Microsoft.AspNetCore.Mvc;
using WhackerLinkLib.Interfaces;
using System.IO;

namespace WhackerLinkServer.RestApi.Controllers
{
    [ApiController]
    [Route("api/masters")]
    public class StartMasterController : ControllerBase
    {
        private readonly IMasterServiceRegistry _registry;

        public StartMasterController(IMasterServiceRegistry registry)
        {
            _registry = registry;
        }

        [HttpPost("{masterName}/start")]
        public IActionResult DisableMaster([FromRoute] string masterName)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            // Stop the master
            // Reflect success or failure in the status code
            bool success = Program.StartMaster(masterName);
            if (!success)
            {
                return StatusCode(500, new { error = $"Failed to start Master {masterName}." });
            }

            return Ok(new { success = true, message = $"Master '{masterName}' has been started." });
        }
    }
}
