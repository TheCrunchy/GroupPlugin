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
    [Category("refinery")]
    public class RefineryCommands : CommandModule
    {
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
            int num = alliance.RefineryUpgradeLevel;
            int newUpgrade = num + 1;
            if (MyProductionPatch.upgrades.TryGetValue(newUpgrade, out RefineryUpgrade upgrade))
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
                            alliance.RefineryUpgradeLevel = newUpgrade;
                                AlliancePlugin.SaveAllianceData(alliance);
                                ShipyardCommands.SendMessage("[Alliance Refineries]", "Upgrading refinery. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
                            }
                        }
                        else
                        {
                            ShipyardCommands.SendMessage("[Alliance Refineries]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", upgrade.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        if (ShipyardCommands.ConsumeComponents(invents, upgrade.getItemsRequired(), Context.Player.SteamUserId))
                        {
                            alliance.CurrentMetaPoints -= upgrade.MetaPointsRequired;
                        alliance.RefineryUpgradeLevel = newUpgrade;
                        ShipyardCommands.SendMessage("[Alliance Refineries]", "Upgrading refinery. Items taken.", Color.LightBlue, (long)Context.Player.SteamUserId);
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
            foreach (RefineryUpgrade upgrade in MyProductionPatch.upgrades.Values)
            {
                sb.AppendLine("Upgrade number " + upgrade.UpgradeId);
                if (upgrade.MoneyRequired > 0)
                {
                    sb.AppendLine("Costs " + String.Format("{0:n0}", upgrade.MoneyRequired) + " SC.");
                }
                if (upgrade.MetaPointsRequired > 0)
                {
                    sb.AppendLine("Costs " + String.Format("{0:n0}", upgrade.MetaPointsRequired) + " Meta Points.");
                }
                foreach (RefineryUpgrade.ItemRequirement item in upgrade.items)
                {
                    if (item.Enabled)
                    {
                        sb.AppendLine("Costs " + item.RequiredAmount + " " + item.TypeId + " " + item.SubTypeId);
                    }
                }
                foreach (RefineryUpgrade.RefineryBuffList buffed in upgrade.buffedRefineries)
                {
                    
                    foreach (RefineryUpgrade.RefineryBuff b in buffed.buffs)
                    {
                        if (b.Enabled)
                        {
                            sb.AppendLine("Buffs yield of " + b.SubtypeId + " by " + buffed.UpgradeAddsYield * 100 + "% and by " + buffed.UpgradeGivesBuffInTerritory * 100 + "% in owned territory.");
                        }
                    }
                }
                sb.AppendLine("");
            }


            DialogMessage m2 = new DialogMessage("Available upgrades", "", sb.ToString());
            ModCommunication.SendMessageTo(m2, Context.Player.SteamUserId);
        }

    }
}
