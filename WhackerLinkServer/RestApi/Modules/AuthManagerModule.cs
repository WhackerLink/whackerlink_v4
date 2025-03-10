using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using WhackerLinkLib.Interfaces;
using WhackerLinkServer.Managers;

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class AuthManagerModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public AuthManagerModule(IMasterService masterService) : base("/api/auth")
        {
            Post("/reload", _ =>
            {
                AuthKeyFileManager authManager = masterService.GetAuthManager();
                authManager.ReloadAuthFile();

                return Response.AsJson(new { success = true });
            });
        }
    }
}
