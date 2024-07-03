using Nancy;
using WhackerLinkCommonLib.Interfaces;

namespace WhackerLinkServer.Models
{
    public class AffiliationsModule : NancyModule
    {
        public AffiliationsModule(IMasterService masterService)
        {
            Get("/api/affiliations", _ =>
            {
                var affiliations = masterService.GetAffiliations();
                return Response.AsJson(affiliations);
            });
        }
    }
}