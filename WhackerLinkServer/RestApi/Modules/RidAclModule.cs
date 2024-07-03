using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Models;

namespace WhackerLinkServer.RestApi.Modules
{
    public class RidAclModule : NancyModule
    {
        public RidAclModule(IMasterService masterService) : base("/api/rid")
        {
            Get("/query", _ =>
            {
                var rids = masterService.GetRidAcl();
                bool aclEnabled = masterService.GetRidAclEnabled();

                var response = new
                {
                    Enabled = aclEnabled,
                    RidAcl = rids
                };

                return Response.AsJson(response);
            });
        }
    }
}