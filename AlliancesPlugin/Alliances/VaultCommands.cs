using System.Collections.Concurrent;
using System.Collections.Generic;
using AlliancesPlugin.Shipyard;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Groups;
using VRage.ObjectBuilders;

namespace AlliancesPlugin.Alliances
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

                var result = ShipyardCommands.ConsumeComponents(invents, temp, Context.Player.SteamUserId);
                if (result.Item1)
                {


                    if (DatabaseForBank.DepositToVault(alliance, id, amount))
                    {
                        AllianceChat.SendChatMessage(alliance.AllianceId, "Vault", Context.Player.DisplayName + " deposited " + amount.ToString() + " " + id.ToString() + " to the vault.", true, 0L);

                    }
                    else
                    {
                        Context.Respond("Error adding to the database, open a ticket to get compensation. " + id.ToString() + " " + amount);
                    }

                }
                else
                {
                    Context.Respond("Targeted grid doesnt contain enough.");
                }
            }
            else
            {
                Context.Respond("Could not find that item. Example !vault deposit Ingot Iron 50");
            }

        }
        [Command("withdraw", "withdraw from vault")]
        [Permission(MyPromoteLevel.None)]
        public void VaultWithdraw(string type, string subtype, int amount)
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
            if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.BankWithdraw))
            {
                if (MyDefinitionId.TryParse("MyObjectBuilder_" + type, subtype, out MyDefinitionId id))
                {

                    MyItemType itemType = new MyInventoryItemFilter("MyObjectBuilder_" + type + "/" + subtype).ItemType;
                    if (Context.Player.Character.GetInventory() != null && Context.Player.Character.GetInventory().CanItemsBeAdded((MyFixedPoint)amount, itemType))
                    {
                        if (DatabaseForBank.WithdrawFromVault(alliance, id, amount))
                        {
                            AllianceChat.SendChatMessage(alliance.AllianceId, "Vault", Context.Player.DisplayName + " withdrew " + amount.ToString() + " " + id.ToString() + " to the vault.", true, 0L);
                            Context.Player.Character.GetInventory().AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                        }
                        else
                        {
                            Context.Respond("Error withdrawing from the database." + id.ToString() + " " + amount);
                        }
                    }
                    else
                    {
                        Context.Respond("Player inventory cannot hold that much!");
                    }
                }
            }
            else
            {
                Context.Respond("You dont have permission to withdraw from the vault.");
            }
        }
    }
}
