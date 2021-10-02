using AlliancesPlugin.KOTH;
using AlliancesPlugin.Shipyard;
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
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    [Category("gridrepair")]
    public class GridRepairCommands : CommandModule
    {
        [Command("repair", "repair grid near waystation")]
        [Permission(MyPromoteLevel.None)]
        public void DoGridRepair()
        {
            if (!AlliancePlugin.config.RepairEnabled)
            {
                Context.Respond("Grid repair not enabled.");
                return;
            }

            //Do checks for if near a waystation and a member of the alliance#
            IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("You must be in a faction to use alliance features.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
            if (alliance == null)
            {
                Context.Respond("You are not a member of an alliance");
                return;
            }
            foreach (Territory ter in AlliancePlugin.Territories.Values)
            {
                if (ter.HasStation && ter.Alliance == alliance.AllianceId)
                {
                    float distance = Vector3.Distance(new Vector3(ter.stationX, ter.stationY, ter.stationZ), Context.Player.GetPosition());
                    if (distance <= 500)
                    {
                        ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
                        foreach (var item in gridWithSubGrids)
                        {
                            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                            {
                                GridRepair.Repair(item, Context.Player.SteamUserId, alliance.AllianceId, ter.Id);
                                Context.Respond("Starting grid repair.");
                            }
                        }
                        return;
                    }
                }
            }
            Context.Respond("You do not own this waystation.");
   

        }

        [Command("upgrade", "purchase next upgrades")]
        [Permission(MyPromoteLevel.None)]
        public void BuyUpgrades()
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Not a member of an alliance, alliance is required.");
                return;
            }
            int num = alliance.GridRepairUpgrade;
            int newUpgrade = num + 1;
            if (GridRepair.upgrades.TryGetValue(newUpgrade, out GridRepairUpgrades upgrade))
            {
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroupMechanical(Context.Player.Character);


                List<MyCubeGrid> grids = new List<MyCubeGrid>();
                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        MyCubeGrid grid = groupNodes.NodeData;



                        if (grid.Projector != null)
                            continue;

                        if (FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid)) != null)
                        {
                            if (FacUtils.InSameFaction(FacUtils.GetOwner(grid), Context.Player.IdentityId))
                            {
                                if (!grids.Contains(grid))
                                    grids.Add(grid);
                            }
                        }
                        else
                        {
                            if (FacUtils.GetOwner(grid).Equals(Context.Player.Identity.IdentityId))
                            {
                                if (!grids.Contains(grid))
                                    grids.Add(grid);
                            }
                        }
                    }
                }
                List<VRage.Game.ModAPI.IMyInventory> invents = new List<VRage.Game.ModAPI.IMyInventory>();
                foreach (MyCubeGrid grid in grids)
                {
                    invents.AddList(ShipyardCommands.GetInventories(grid));
                }


                if (upgrade.MetaPointsRequired > 0)
                {

                    if (alliance.CurrentMetaPoints < upgrade.MetaPointsRequired)
                    {
                        Context.Respond("Cannot afford the meta point cost of " + upgrade.MetaPointsRequired);
                        return;
                    }
                }
                if (upgrade.MoneyRequired > 0)
                {

                    if (EconUtils.getBalance(Context.Player.IdentityId) >= upgrade.MoneyRequired)
                    {
                        if (ShipyardCommands.ConsumeComponents(invents, upgrade.getItemsRequired(), Context.Player.SteamUserId))
                        {
                            alliance.CurrentMetaPoints -= upgrade.MetaPointsRequired;
                            EconUtils.takeMoney(Context.Player.IdentityId, upgrade.MoneyRequired);
                            alliance.GridRepairUpgrade = newUpgrade;
                            AlliancePlugin.SaveAllianceData(alliance);
                            ShipyardCommands.SendMessage("[Grid Repair]", "Upgrading Grid Repair. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        ShipyardCommands.SendMessage("[Grid Repair]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", upgrade.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                    }
                }
                else
                {
                    if (ShipyardCommands.ConsumeComponents(invents, upgrade.getItemsRequired(), Context.Player.SteamUserId))
                    {
                        alliance.CurrentMetaPoints -= upgrade.MetaPointsRequired;
                        alliance.GridRepairUpgrade = newUpgrade;
                        ShipyardCommands.SendMessage("[Grid Repair]", "Upgrading Grid Repair. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                }

            }
            else
            {
                Context.Respond("No more upgrades available.");
            }

        }
        [Command("view", "view the upgrades")]
        [Permission(MyPromoteLevel.None)]
        public void ViewUpgrades()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GridRepairUpgrades upgrade in GridRepair.upgrades.Values)
            {
                sb.AppendLine("Upgrade number " + upgrade.UpgradeId);
                //    sb.AppendLine("Adds to weekly upkeep " + String.Format("{0:n0}", upgrade.AddsToUpkeep) + " SC.");
                if (upgrade.MoneyRequired > 0)
                {
                    sb.AppendLine("Costs " + String.Format("{0:n0}", upgrade.MoneyRequired) + " SC.");
                }
                if (upgrade.MetaPointsRequired > 0)
                {
                    sb.AppendLine("Costs " + String.Format("{0:n0}", upgrade.MetaPointsRequired) + " Meta Points.");
                }
                foreach (ItemRequirement item in upgrade.items)
                {
                    if (item.Enabled)
                    {
                        sb.AppendLine("Costs " + item.RequiredAmount + " " + item.TypeId + " " + item.SubTypeId);

                    }
                }
                sb.AppendLine("Seconds between cycles " + upgrade.SecondsPerCycle);
                sb.AppendLine("Blocks fixed per cycle " + upgrade.RepairBlocksPerCycle);
                sb.AppendLine("Projected blocks built per cycle " + upgrade.ProjectedBuildPerCycle);
                sb.AppendLine("");
            }


            DialogMessage m2 = new DialogMessage("Available upgrades", "", sb.ToString());
            ModCommunication.SendMessageTo(m2, Context.Player.SteamUserId);
        }

    }
}
