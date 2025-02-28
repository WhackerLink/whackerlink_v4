/*
* WhackerLink - WhackerLinkServer
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhackerLinkLib.Models;


#nullable disable

namespace WhackerLinkServer.Managers
{
    /// <summary>
    /// Class to manage affiliations
    /// </summary>
    public class AffiliationsManager
    {
        public List<Affiliation> Affiliations { get; private set; }

        /// <summary>
        /// Creates an instance of AffiliationManager
        /// </summary>
        public AffiliationsManager()
        {
            Affiliations = new List<Affiliation>();
        }

        /// <summary>
        /// Gets the current affiliations list
        /// </summary>
        /// <returns></returns>
        public List<Affiliation> GetAffiliations()
        {
            return Affiliations;
        }

        /// <summary>
        /// Helper to add affiliation
        /// </summary>
        /// <param name="affiliation"></param>
        public void AddAffiliation(Affiliation affiliation)
        {
            if (!isAffiliated(affiliation))
            {
                Affiliations.Add(affiliation);
            }
            else
            {
                Console.WriteLine($"srcId: {affiliation.SrcId} on dstId: {affiliation.DstId} already affiliated. skipping....");
            }
        }

        /// <summary>
        /// Helper to add affilation
        /// </summary>
        /// <param name="affiliation"></param>
        public void RemoveAffiliation(Affiliation affiliation)
        {
            Affiliations.RemoveAll(a => a.SrcId == affiliation.SrcId && a.DstId == affiliation.DstId);
        }

        /// <summary>
        /// Helper to remove and affiliation by srcid
        /// </summary>
        /// <param name="srcId"></param>
        public void RemoveAffiliation(string srcId)
        {
            Affiliations.RemoveAll(a => a.SrcId == srcId);
        }

        /// <summary>
        /// Helper to remove an affiliation by srcId and dstId.
        /// </summary>
        /// <param name="srcId">The source ID of the affiliation.</param>
        /// <param name="dstId">The destination ID (talkgroup) of the affiliation.</param>
        public void RemoveAffiliation(string srcId, string dstId)
        {
            Affiliations.RemoveAll(a => a.SrcId == srcId && a.DstId == dstId);
        }

        /// <summary>
        /// Helper to remove affiliation by client id
        /// </summary>
        /// <param name="clientId"></param>
        public void RemoveAffiliationByClientId(string clientId)
        {
            Affiliations.RemoveAll(a => a.ClientId == clientId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public Affiliation GetAffiliation(string clientId)
        {
            return Affiliations.FirstOrDefault(a => a.ClientId == clientId);
        }

        /// <summary>
        /// Helper to get affiliations by client id
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public List<Affiliation> GetAffiliationsByClientId(string clientId)
        {
            return Affiliations.Where(a => a.ClientId == clientId).ToList();
        }

        /// <summary>
        /// Helper to check if an affiliation insance is affiliated
        /// </summary>
        /// <param name="affiliation"></param>
        /// <returns></returns>
        public bool isAffiliated(Affiliation affiliation)
        {
            return Affiliations.Any(a => a.SrcId == affiliation.SrcId && a.DstId == affiliation.DstId);
        }

        /// <summary>
        /// Checks if a srcid is affiliated
        /// </summary>
        /// <param name="srcId"></param>
        /// <returns></returns>
        public bool isSrcIdAffiliated(string srcId)
        {
            return Affiliations.Any(a => a.SrcId == srcId);
        }

        /// <summary>
        /// Checks if a srcid is affiliated to a dstid
        /// </summary>
        /// <param name="srcId"></param>
        /// <returns></returns>
        public bool isSrcIdAffiliated(string srcId, string dstId)
        {
            return Affiliations.Any(a => a.SrcId == srcId && a.DstId == dstId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Affiliations table:");

            foreach (var affiliation in Affiliations)
            {
                sb.AppendLine($"SrcId: {affiliation.SrcId}, DstId: {affiliation.DstId}");
            }

            return sb.ToString();
        }
    }
}