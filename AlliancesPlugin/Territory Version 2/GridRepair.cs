using System;
using System.Collections.Generic;
using System.Linq;
using AlliancesPlugin.Shipyard;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace AlliancesPlugin.Alliances.Upgrades
{
    public class GridRepair
    {
        public static Dictionary<long, Guid> AllianceIds = new Dictionary<long, Guid>();
        public static Dictionary<int, GridRepairUpgrades> upgrades = new Dictionary<int, GridRepairUpgrades>();
        public static long CalculatePriceForComponents(Dictionary<String, int> components, long identityIdForAllianceChecks)
        {
            long output = 1;
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
                    if (upgrades.TryGetValue(alliance.GridRepairUpgrade, out GridRepairUpgrades upgrade))
                    {
                     

                        foreach (KeyValuePair<String, int> pair in components)
                        {
                            if (upgrade.BannedComponents.Contains(pair.Key))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        AlliancePlugin.Log.Info("Couldnt find the upgrade level");
                        return true;
                    }
                }
            }

            return false;
        }
        public static List<long> yeet = new List<long>();

        public static void BuildProjected(MyCubeGrid grid, long identityId, int fixPerCycle, int buildPerCycle, string prefix, float priceMultiplier)
        {
            int BannedCount = 0;
            int blockCount = 0;
            int fixedblocks = 0;
            long PriceSoFar = 0;
       //     AlliancePlugin.Log.Info("build 1");
            HashSet<MySlimBlock> blocks3 = grid.GetBlocks();
            foreach (MySlimBlock block in blocks3)
            {
                if (fixedblocks >= fixPerCycle)
                {
                    if (PriceSoFar > 0)
                    {
                        EconUtils.takeMoney(identityId, PriceSoFar);
                  //      AlliancePlugin.Log.Info("pay 1");
                    }
                    return;
                }
                long owner = block.OwnerId;
                if (owner == 0)
                    owner = identityId;

                if (block.CurrentDamage > 0 || block.HasDeformation || !block.IsFullIntegrity)
                {
                    //check for components and if they can afford it
                    Dictionary<string, int> temp = new Dictionary<string, int>();
                    block.GetMissingComponents(temp);
                    //CONVERT THE TEMP TO DEFINITION IDS, OR IDS TO STRINGS AND CHECK AGAINST THOSE


                    if (IsBannedComponent(temp, identityId))
                    {
                        BannedCount++;
                        continue;
                    }

                    long tempPrice = (long)(CalculatePriceForComponents(temp, identityId) * priceMultiplier);
               //     AlliancePlugin.Log.Info($"{tempPrice} {PriceSoFar}");
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
                   //     AlliancePlugin.Log.Info("pay 2");

                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }
                        yeet.Add(grid.EntityId);
                        AllianceIds.Remove(identityId);
                        ShipyardCommands.SendMessage(prefix, $"Cannot afford repair cost, cancelling repair. You require {tempPrice:n0}" + " more SC.", Color.Red, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        return;
                    }

                }

            }
            if (PriceSoFar > 0)
            {
                EconUtils.takeMoney(identityId, PriceSoFar);
             //   AlliancePlugin.Log.Info("pay 3");

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

            List<VRage.Game.ModAPI.IMySlimBlock> remove = new List<IMySlimBlock>();
            List<VRage.Game.ModAPI.IMySlimBlock> notConnected = new List<IMySlimBlock>();
            if (projectedGrid != null)
            {
                // AlliancePlugin.Log.Info("Projector");
                List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();


                int Cycle = 0;

                //im 100% sure this will be an infinite loop

                while (blockCount <= buildPerCycle)
                {
                    if (projector.RemainingBlocks == BannedCount)
                    {
                        yeet.Add(grid.EntityId);
                        AllianceIds.Remove(identityId);

                        ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        return;

                    }
                    Cycle++;
                    projectedGrid.GetBlocks(blocks);

                    //foreach (VRage.Game.ModAPI.IMySlimBlock block in remove)
                    //{

                    //    projectedGrid.RemoveBlock(block);
                    //    blocks.Remove(block);
                    //}

                    if (Cycle >= 20)
                    {
                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }
                        if (BannedCount == blocks.Count)
                        {
                            yeet.Add(grid.EntityId);
                            AllianceIds.Remove(identityId);
                            ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        }
                        return;
                        //   blockCount = buildPerCycle;

                    }



                    int baseBlocks = grid.BlocksCount;

                    int Percent = Convert.ToInt32(projector.TotalBlocks * 0.5);
                    if (blocks.Count == 0)
                    {
                        yeet.Add(grid.EntityId);
                        AllianceIds.Remove(identityId);
                        ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
                        if (PriceSoFar > 0)
                        {
                            EconUtils.takeMoney(identityId, PriceSoFar);
                        }

                        //   AlliancePlugin.Log.Info("No projected blocks so removing the grid from cycle");
                        return;
                    }
                    if (baseBlocks < Percent)
                    {
                        yeet.Add(grid.EntityId);
                        ShipyardCommands.SendMessage(prefix, "50 Percent of grid is not built. Cancelling repair. " + (Percent - baseBlocks) + " Built blocks are required", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
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
                                remove.Add(block2);
                                BannedCount++;
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
                                ShipyardCommands.SendMessage(prefix, "Cannot afford repair cost, cancelling repair. You require " + String.Format("{0:n0}", tempPrice) + " more SC.", Color.Red, (long)MySession.Static.Players.TryGetSteamId(identityId));
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

            //    yeet.Add(grid.EntityId);
            //    AllianceIds.Remove(identityId);
            //    ShipyardCommands.SendMessage("ACME", "Grid repair should be complete.", Color.Green, (long)MySession.Static.Players.TryGetSteamId(identityId));
            //    //    AlliancePlugin.Log.Info("Couldnt find projector");
            }
        }
}
