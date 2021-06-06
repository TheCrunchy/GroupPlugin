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
                    if (holder.contract is Medium medium)
                    {
                        if (medium.subTypeId.Equals(MaterialName))
                        {
                            return true;
                        }
                        if (medium.subTypeIdTwo.Equals(MaterialName) && medium.HasSecondItem)
                        {
                            return true;
                        }
                    }
                    if (holder.contract is Hard hard)
                    {
                        if (hard.subTypeId.Equals(MaterialName))
                        {
                            return true;
                        }
                        if (hard.subTypeIdTwo.Equals(MaterialName) && hard.HasSecondItem)
                        {
                            return true;
                        }
                        if (hard.subTypeIdThree.Equals(MaterialName) && hard.HasThirdItem)
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
