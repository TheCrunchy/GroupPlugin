using AlliancesPlugin.Alliances;
using AlliancesPlugin.Shipyard;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace AlliancesPlugin
{
    [Category("vault")]
    public class VaultCommands : CommandModule
    {
        [Command("deposit", "deposit to vault")]
        [Permission(MyPromoteLevel.None)]
        public void VaultDeposit(string type, string subtype, int amount)
        {
            Alliance alliance = null;

            if (MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) != null)
            {
                alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) as MyFaction);


            }
            else
            {
                Context.Respond("You must be in an alliance to use alliance commands.");
            }


            if (alliance == null)
            {
                Context.Respond("You are not a member of an alliance.");
                return;
            }
            if (MyDefinitionId.TryParse("MyObjectBuilder_" + type, subtype, out MyDefinitionId id))
            {
                if (!AlliancePlugin.ItemUpkeep.ContainsKey(id))
                {
                    Context.Respond("This item isnt required for upkeep. Cannot deposit into vault.");
                    return;
                }
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids;


                List<MyCubeGrid> grids = new List<MyCubeGrid>();
                gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);

                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        MyCubeGrid grid = groupNodes.NodeData;


                        if (FacUtils.IsOwnerOrFactionOwned(grid, FacUtils.GetOwner(grid), true))
                        {
                                if (!grids.Contains(grid))
                                    grids.Add(grid);
                        }
                    }
                }
                List<VRage.Game.ModAPI.IMyInventory> invents = new List<VRage.Game.ModAPI.IMyInventory>();
                Dictionary<MyDefinitionId, int> temp = new Dictionary<MyDefinitionId, int>();
                temp.Add(id, amount);
                foreach (MyCubeGrid grid in grids)
                {
                    invents.AddRange(ShipyardCommands.GetInventories(grid));
                }

                if (ShipyardCommands.ConsumeComponents(invents, temp, Context.Player.SteamUserId))
                {

               
                       if (DatabaseForBank.DepositToVault(alliance, id, amount))
                    {
                        AllianceChat.SendChatMessage(alliance.AllianceId, "Vault", Context.Player.DisplayName + " deposited " + amount.ToString() + " " + id.ToString() + " to the vault.", true, 0L);

                    }
                       else {
                        Context.Respond("Error adding to the database, open a ticket to get compensation. " + id.ToString() + " " + amount);
                    }
                   
                }
                else{
                    Context.Respond("Targeted grid doesnt contain enough.");
                }
            }
             else
            {
                Context.Respond("Could not find that item. Example !vault deposit Ingot Iron 50");
            }

        }
    }
}
