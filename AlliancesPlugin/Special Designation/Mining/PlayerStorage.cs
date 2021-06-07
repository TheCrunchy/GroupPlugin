using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.Special_Designation
{
    public class PlayerStorage
    {
        public ulong steamId;
        public Dictionary<Guid, ContractHolder> MiningContracts = new Dictionary<Guid, ContractHolder>();


        public Boolean AddToContractAmount(string OreSubtype, int amount)
        {
            foreach (ContractHolder holder in MiningContracts.Values)
            {

                if (holder.contract.subTypeId.Equals(OreSubtype))
                {
                    if (holder.minedAmount >= holder.amountToMine)
                        continue;

                    holder.minedAmount += amount;
                    if (holder.minedAmount >= holder.amountToMine)
                        holder.minedAmount = holder.amountToMine;
                    return true;
                }

            }
            return false;
        }
        public int reputation;
        public Boolean CheckIfInMiningArea(Vector3 position, String MaterialName)
        {
            foreach (ContractHolder holder in MiningContracts.Values)
            {
                if (Vector3.Distance(holder.GetMiningArea(), position) <= holder.MiningRadius)
                {
                    if (holder.contract.subTypeId.Equals(MaterialName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
