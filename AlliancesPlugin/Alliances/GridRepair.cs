using AlliancesPlugin.KOTH;
using AlliancesPlugin.Shipyard;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    public class GridRepair
    {
        public static Dictionary<long, long> gridsToRepair = new Dictionary<long, long>();
        public static Dictionary<long, DateTime> gridCooldowns = new Dictionary<long, DateTime>();
        public static Dictionary<long, Guid> AllianceIds = new Dictionary<long, Guid>();
        public static Dictionary<int, GridRepairUpgrades> upgrades = new Dictionary<int, GridRepairUpgrades>();
        public static Dictionary<long, Guid> location = new Dictionary<long, Guid>();
        public static long CalculatePriceForComponents(Dictionary<String, int> components, long identityIdForAllianceChecks)
        {
            long output = 0;
            if (AllianceIds.TryGetValue(identityIdForAllianceChecks, out Guid allianceId))
            {
                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
                if (alliance != null)
                {
                    if (upgrades.TryGetValue(alliance.GridRepairUpgrade, out GridRepairUpgrades level))
                    {


                        foreach (KeyValuePair<String, int> pair in components)
                        {
                            output += level.getCost(pair.Key) * pair.Value;
                        }
                    }
                    else
                    {
                        return 500000000000000l;
                    }
                }
            }

            return output;

        }

        public static Boolean IsBannedComponent(Dictionary<String, int> components, long identityIdForAllianceChecks)
        {
            if (AllianceIds.TryGetValue(identityIdForAllianceChecks, out Guid allianceId))
            {
                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
                if (alliance != null)
                {
                    GridRepairUpgrades level = new GridRepairUpgrades();
                    //upgrades[alliance.GridRepairUpgrade];

                    foreach (KeyValuePair<String, int> pair in components)
                    {
                        if (level.BannedComponents.Contains(pair.Key))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        public static List<long> yeet = new List<long>();
        public static void DoRepairCycle()
        {

            foreach (KeyValuePair<long, long> pair in gridsToRepair)
            {
                if (gridCooldowns.TryGetValue(pair.Key, out DateTime time))
                {
                    if (DateTime.Now >= time)
                    {
                        if (MyAPIGateway.Entities.GetEntityById(pair.Key) != null && MyAPIGateway.Entities.GetEntityById(pair.Key) is MyCubeGrid grid)
                        {
                            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(AllianceIds[pair.Value]);
                            if (upgrades.TryGetValue(alliance.GridRepairUpgrade, out GridRepairUpgrades upgrade))
                            {
                                BuildProjected(grid, pair.Value, upgrade.RepairBlocksPerCycle, upgrade.ProjectedBuildPerCycle);
                                gridCooldowns[pair.Key] = DateTime.Now.AddSeconds(upgrade.SecondsPerCycle);

                            }
                            else
                            {
                                AlliancePlugin.Log.Error("Couldnt find this uppgrade id " + AlliancePlugin.GetAllianceNoLoading(AllianceIds[pair.Key]).GridRepairUpgrade + " CANCELLING REPAIR");
                                yeet.Add(pair.Key);
                                return;
                            }

                        }
                        else
                        {
                            yeet.Add(pair.Key);
                        }

                    }
                }
                else
                {
                    yeet.Add(pair.Key);
                }
            }
            foreach (long id in yeet)
            {
                gridsToRepair.Remove(id);
                gridCooldowns.Remove(id);
            }
            yeet.Clear();
        }

        //repair blocks on the grid first
        public static void Repair(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, ulong steamId, Guid AllianceId, Guid territoryId)
        {
            long newowner = MySession.Static.Players.TryGetIdentityId(steamId);
            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes)
            {

                MyCubeGrid grid = groupNodes.NodeData;

                if (!gridsToRepair.ContainsKey(grid.EntityId))
                {
                    gridsToRepair.Add(grid.EntityId, newowner);
                    gridCooldowns.Add(grid.EntityId, DateTime.Now.AddSeconds(1));
                    AllianceIds.Remove(newowner);
                    location.Remove(grid.EntityId);
                    location.Add(grid.EntityId, territoryId);
                    AllianceIds.Add(newowner, AllianceId);
                }
                else
                {
                    //send message about already being repaired

                }




            }
        }

        //now we want to build projected blocks

        public static void BuildProjected(MyCubeGrid grid, long identityId, int fixPerCycle, int buildPerCycle)
        {
            Territory ter = AlliancePlugin.Territories[location[grid.EntityId]];
            float distance = Vector3.Distance(grid.PositionComp.GetPosition(), new Vector3(ter.stationX, ter.stationY, ter.stationZ));
            if (distance >= 1000)
            {
                yeet.Add(grid.EntityId);
                AllianceIds.Remove(identityId);
                ShipyardCommands.SendMessage("ACME", "Grid left building range. Cancelling repair.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                return;
            }
            //  AlliancePlugin.Log.Info("Doing buildy shit");
            int blockCount = 0;
            int fixedblocks = 0;
            long PriceSoFar = 0;
            HashSet<MySlimBlock> blocks3 = grid.GetBlocks();
            foreach (MySlimBlock block in blocks3)
            {
                if (fixedblocks >= fixPerCycle)
                {
                    if (PriceSoFar > 0)
                    {
                        EconUtils.takeMoney(identityId, PriceSoFar);
                    }
                    return;
                }
                long owner = block.OwnerId;
                if (owner == 0)
                    owner = identityId;

                if (block.CurrentDamage > 0 || block.HasDeformation)
                {
                    //check for components and if they can afford it
                    Dictionary<string, int> temp = new Dictionary<string, int>();
                    block.GetMissingComponents(temp);
                    //CONVERT THE TEMP TO DEFINITION IDS, OR IDS TO STRINGS AND CHECK AGAINST THOSE


                    if (IsBannedComponent(temp, identityId))
                    {
                        continue;
                    }
                    long tempPrice = CalculatePriceForComponents(temp, identityId);
                    if (EconUtils.getBalance(identityId) >= (PriceSoFar + tempPrice))
                    {
                        PriceSoFar += tempPrice;
                        block.ClearConstructionStockpile(null);


                        block.IncreaseMountLevel(block.MaxIntegrity, owner, null, 10000, true);

                        MyCubeBlock cubeBlock = block.FatBlock;

                        if (cubeBlock != null)
                        {

                            grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
                            if (owner != 0)
                                grid.ChangeOwnerRequest(grid, cubeBlock, owner, MyOwnershipShareModeEnum.Faction);
                        }
                        fixedblocks++;
                    }
                    else
                    {
                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }
                        yeet.Add(grid.EntityId);
                        AllianceIds.Remove(identityId);
                        ShipyardCommands.SendMessage("ACME", "Cannot afford repair cost, cancelling repair. You require " + String.Format("{0:n0}", tempPrice) + " more SC.", Color.Red, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        return;
                    }

                }

            }
            PriceSoFar = 0;
            IMyCubeGrid projectedGrid = null;
            IMyProjector projector = null;
            foreach (MyProjectorBase proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
            {
                if (proj.IsFunctional && proj.ProjectedGrid != null && projector == null)
                {


                    projector = proj as IMyProjector;
                    if (projector.BlockDefinition.SubtypeName != null && projector.BlockDefinition.SubtypeName.ToLower().Contains("console"))
                    {
                        projector = null;
                        continue;

                    }
                    projectedGrid = proj.ProjectedGrid;
                }
            }

            List<VRage.Game.ModAPI.IMySlimBlock> notConnected = new List<IMySlimBlock>();
            if (projectedGrid != null)
            {
                // AlliancePlugin.Log.Info("Projector");
                List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();


                int Cycle = 0;

                //im 100% sure this will be an infinite loop

                while (blockCount <= buildPerCycle)
                {

                    Cycle++;

                    if (Cycle >= 20)
                    {
                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }
                        return;

                    }

                    projectedGrid.GetBlocks(blocks);

                    int baseBlocks = grid.GetBlocks().Count;
                    int Percent = Convert.ToInt32(blocks.Count * 0.5);
                    if (baseBlocks < Percent)
                    {
                        yeet.Add(grid.EntityId);
                        ShipyardCommands.SendMessage("ACME", "50 Percent of grid is not built. Cancelling repair. " + (Percent - baseBlocks) + " Built blocks are required", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        return;
                    }
                    if (blocks.Count == 0)
                    {
                        yeet.Add(grid.EntityId);
                        AllianceIds.Remove(identityId);
                        ShipyardCommands.SendMessage("ACME", "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }

                        //   AlliancePlugin.Log.Info("No projected blocks so removing the grid from cycle");
                        return;
                    }
                    foreach (VRage.Game.ModAPI.IMySlimBlock block2 in blocks)
                    {

                        if (blockCount >= buildPerCycle)
                        {
                            if (PriceSoFar > 0)
                            {
                                EconUtils.takeMoney(identityId, PriceSoFar);
                            }
                            return;
                        }


                        if (projector.CanBuild(block2, true) == BuildCheckResult.OK)
                        {

                            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();

                            //we still want this method to calculate SC cost

                            Dictionary<string, int> converted = new Dictionary<string, int>();

                            ShipyardCommands.GetComponents((MyCubeBlockDefinition)block2.BlockDefinition, comps);

                            //make a method to calculate price
                            foreach (KeyValuePair<MyDefinitionId, int> pair in comps)
                            {
                                converted.Add(pair.Key.SubtypeName.ToString(), pair.Value);
                                //     AlliancePlugin.Log.Info(pair.Key.SubtypeName.ToString());
                            }
                            if (IsBannedComponent(converted, identityId))
                            {
                                continue;
                            }
                            long tempPrice = CalculatePriceForComponents(converted, identityId);

                            if (EconUtils.getBalance(identityId) >= PriceSoFar + tempPrice)
                            {
                                PriceSoFar += tempPrice;
                                projector.Build(block2, identityId, identityId, true);
                                //       AlliancePlugin.Log.Info("building");
                                blockCount++;
                            }
                            else
                            {
                                if (PriceSoFar > 0)
                                {
                                    EconUtils.takeMoney(identityId, PriceSoFar);
                                }
                                yeet.Add(grid.EntityId);
                                AllianceIds.Remove(identityId);
                                ShipyardCommands.SendMessage("ACME", "Cannot afford repair cost, cancelling repair. You require " + String.Format("{0:n0}", tempPrice) + " more SC.", Color.Red, (long)MySession.Static.Players.TryGetSteamId(identityId));
                                return;
                            }

                        }
                        if (blockCount >= buildPerCycle)
                        {
                            if (PriceSoFar > 0)
                            {
                                EconUtils.takeMoney(identityId, PriceSoFar);
                            }
                            return;
                        }
                    }
                }

                if (blockCount >= buildPerCycle)
                {
                    if (PriceSoFar > 0)
                    {
                        EconUtils.takeMoney(identityId, PriceSoFar);
                    }
                }
            }
            yeet.Add(grid.EntityId);
            AllianceIds.Remove(identityId);
            ShipyardCommands.SendMessage("ACME", "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
            //    AlliancePlugin.Log.Info("Couldnt find projector");
        }
    }
}
