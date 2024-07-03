using System.Collections.Generic;
using WhackerLinkServer.Models;

namespace WhackerLinkCommonLib.Interfaces
{
    public interface IMasterService
    {
        List<Affiliation> GetAffiliations();
        void Start();
    }
}