using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Special_Designation
{
    [Category("miningcontract")]
    public class MiningCommands : CommandModule
    {
        [Command("take", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig(string subtype, int min, int max, int price)
        {
            if (DrillPatch.playerWithContract.ContainsKey(Context.Player.IdentityId))
            {
                MiningContract contract = new MiningContract();
                contract.subTypeId = subtype;
                contract.minimunAmount = min;
                contract.maximumAmount = max;
                
                ContractHolder holder = new ContractHolder();
                holder.DeliveryX = Context.Player.GetPosition().X;
                holder.DeliveryY = Context.Player.GetPosition().Y;
                holder.DeliveryZ = Context.Player.GetPosition().Z;
                holder.MiningRadius = 50000;
                holder.AreaX = Context.Player.GetPosition().X;
                holder.AreaY = Context.Player.GetPosition().Y;
                holder.AreaZ = Context.Player.GetPosition().Z;
                holder.contract = contract;
                holder.GenerateAmountToMine();
                holder.price = price;
                DrillPatch.playerWithContract[Context.Player.IdentityId].MiningContracts.Add(holder.Id, holder);
            }
            else
            {
                PlayerStorage storage = new PlayerStorage();
                storage.steamId = Context.Player.SteamUserId;
                storage.reputation = 0;
                MiningContract contract = new MiningContract();
                contract.subTypeId = subtype;
                contract.minimunAmount = min;
                contract.maximumAmount = max;

                ContractHolder holder = new ContractHolder();
                holder.DeliveryX = Context.Player.GetPosition().X;
                holder.DeliveryY = Context.Player.GetPosition().Y;
                holder.DeliveryZ = Context.Player.GetPosition().Z;
                holder.MiningRadius = 50000;
                holder.AreaX = Context.Player.GetPosition().X;
                holder.AreaY = Context.Player.GetPosition().Y;
                holder.AreaZ = Context.Player.GetPosition().Z;
                holder.contract = contract;
                holder.GenerateAmountToMine();
                holder.price = price;
                storage.MiningContracts.Add(holder.Id, holder);
                DrillPatch.playerWithContract.Add(Context.Player.IdentityId, storage);
            }
        }
    }
}
