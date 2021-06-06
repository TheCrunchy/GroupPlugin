using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation.Mining
{
   public class PlayerStorage
    {
        public ulong steamId;
        public Dictionary<Guid, ContractHolder> contracts = new Dictionary<Guid, ContractHolder>();

        public Boolean CheckIfInMiningArea(Vector3 position, String MaterialName)
        {
            foreach (ContractHolder holder in contracts.Values)
            {
                if (Vector3.Distance(holder.GetMiningArea(), position) <= holder.MiningRadius)
                {
                    if (holder.contract is Easy easy)
                    {
                        if (holder.contract.subTypeId.Equals(MaterialName))
                        {
                            return true;
                        }
                    }
                   
                    
                    return true;
                }
            }
            return false;
        }
    }
}
