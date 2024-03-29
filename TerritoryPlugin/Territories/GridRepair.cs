﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using AlliancesPlugin.Integrations;
//using AlliancesPlugin.Shipyard;
//using AlliancesPlugin.Territory_Version_2.Models;
//using Sandbox.Definitions;
//using Sandbox.Game;
//using Sandbox.Game.Entities;
//using Sandbox.Game.Entities.Blocks;
//using Sandbox.Game.Entities.Cube;
//using Sandbox.Game.World;
//using Sandbox.ModAPI;
//using Torch.Managers.PatchManager;
//using VRage.Game;
//using VRage.Game.Components;
//using VRage.Game.ModAPI;
//using VRage.Groups;
//using VRage.Utils;
//using VRageMath;

//namespace AlliancesPlugin.Alliances.Upgrades
//{
//    public static class GridRepair
//    {

//        public static List<long> WorkingProjectors = new List<long>();
//        public static void BuildProjected(MyCubeGrid grid, long identityId, int fixPerCycle, int buildPerCycle, string prefix, float priceMultiplier, Dictionary<string, ComponentCost> componentCosts, bool requireMoney, bool consumeComponents, List<IMyInventory> inventories)
//        {
//            WorkingProjectors.Clear();
//            WorkingProjectors.Add(grid.EntityId);
//            int BannedCount = 0;
//            int blockCount = 0;
//            int fixedblocks = 0;
//            long PriceSoFar = 0;
//            var steamid = (long)MySession.Static.Players.TryGetSteamId(identityId);
//            //     GroupPlugin.Log.Info("build 1");
//            HashSet<MySlimBlock> blocks3 = grid.GetBlocks();

//            var converted = new Dictionary<MyDefinitionId, int>();
//            var convertedRemove = new Dictionary<MyDefinitionId, int>();

//            foreach (MySlimBlock block in blocks3)
//            {
//                if (fixedblocks >= fixPerCycle)
//                {
//                    if (PriceSoFar > 0 && requireMoney)
//                    {
//                        EconUtils.takeMoney(identityId, PriceSoFar);
//                        //      GroupPlugin.Log.Info("pay 1");
//                    }
//                    return;
//                }
//                long owner = block.OwnerId;
//                if (owner == 0)
//                    owner = identityId;

//                if (block.CurrentDamage > 0 || block.HasDeformation || !block.IsFullIntegrity)
//                {
//                    Dictionary<string, int> temp = new Dictionary<string, int>();
//                    block.GetMissingComponents(temp);
//                    bool banned = false;
//                    long tempPrice = 0;
//                    BannedCount = CalcPriceAndBanned(priceMultiplier, componentCosts, temp, BannedCount, ref banned, ref tempPrice);

//                    if (banned)
//                    {
//                        continue;
//                    }

//                    foreach (var comp in temp)
//                    {
//                        if (MyDefinitionId.TryParse(comp.Key, out var def))
//                        {
//                            converted.Add(def, comp.Value);
//                        }
//                    }
//                    if (requireMoney)
//                    {
//                        if (EconUtils.getBalance(identityId) >= (PriceSoFar + tempPrice))
//                        {
//                            if (consumeComponents)
//                            {
//                                if (!ShipyardCommands.CanBuild(inventories, converted,
//                                        (ulong)steamid))
//                                {
//                                    if (PriceSoFar > 0)
//                                    {
//                                        EconUtils.takeMoney(identityId, PriceSoFar);
//                                        PriceSoFar = 0;
//                                    }

//                                    ShipyardCommands.ConsumeComponents(inventories, convertedRemove,
//                                        (ulong)steamid);
//                                    return;
//                                }

//                                convertedRemove = converted.ToDictionary(x => x.Key, x => x.Value); ;
//                            }
//                            PriceSoFar += tempPrice;
//                            fixedblocks = RepairBlock(grid, block, owner, fixedblocks);
//                        }
//                        else
//                        {

//                            if (PriceSoFar > 0)
//                            {
//                                EconUtils.takeMoney(identityId, PriceSoFar);
//                                PriceSoFar = 0;
//                            }
//                            ShipyardCommands.SendMessage(prefix, $"Cannot afford repair cost, cancelling repair. You require {tempPrice:n0}" + " more SC.", Color.Red, steamid);
//                            return;
//                        }
//                    }
//                    else if (consumeComponents)
//                    {
//                        if (!ShipyardCommands.CanBuild(inventories, converted,
//                                (ulong)steamid))
//                        {
//                            ShipyardCommands.ConsumeComponents(inventories, convertedRemove,
//                                (ulong)steamid);
//                            return;
//                        }
//                        convertedRemove = converted.ToDictionary(x => x.Key, x => x.Value); ;
//                    }

//                    fixedblocks = RepairBlock(grid, block, owner, fixedblocks);
//                }

//            }

//            if (consumeComponents)
//            {
//                ShipyardCommands.ConsumeComponents(inventories, convertedRemove,
//                    (ulong)steamid);
//            }

//            if (PriceSoFar > 0 && requireMoney)
//            {
//                EconUtils.takeMoney(identityId, PriceSoFar);
//                PriceSoFar = 0;
//            }


//            IMyCubeGrid projectedGrid = null;
//            IMyProjector projector = null;
//            foreach (MyProjectorBase proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
//            {
//                if (proj.IsFunctional && proj.ProjectedGrid != null && projector == null)
//                {


//                    projector = proj as IMyProjector;
//                    if (projector.BlockDefinition.SubtypeName != null && projector.BlockDefinition.SubtypeName.ToLower().Contains("console"))
//                    {
//                        projector = null;
//                        continue;

//                    }
//                    projectedGrid = proj.ProjectedGrid;
//                }
//            }

//            List<VRage.Game.ModAPI.IMySlimBlock> remove = new List<IMySlimBlock>();
//            List<VRage.Game.ModAPI.IMySlimBlock> notConnected = new List<IMySlimBlock>();
//            if (projectedGrid == null) return;

//            List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();

//            int Cycle = 0;
//            PriceSoFar = 0;
//            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();
//            Dictionary<MyDefinitionId, int> removeComps = new Dictionary<MyDefinitionId, int>();
//            while (blockCount <= buildPerCycle)
//            {
//                if (projector.BuildableBlocksCount == 0)
//                {
//                    if (consumeComponents)
//                    {
//                        ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                            (ulong)steamid);
//                    }
//                    if (PriceSoFar > 0 && requireMoney)
//                    {
//                        EconUtils.takeMoney(identityId, PriceSoFar);
//                        PriceSoFar = 0;
//                    }
//                    return;
//                }

//                if (projector.RemainingBlocks == BannedCount)
//                {
//                    if (consumeComponents)
//                    {
//                        ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                            (ulong)steamid);
//                    }
//                    if (PriceSoFar > 0 && requireMoney)
//                    {
//                        EconUtils.takeMoney(identityId, PriceSoFar);
//                        PriceSoFar = 0;
//                    }
//                    ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, steamid);
//                    return;

//                }
//                Cycle++;
//                projectedGrid.GetBlocks(blocks);

//                if (Cycle >= 20)
//                {
//                    //   GroupPlugin.Log.Info("Hit 20 cycle");
//                    if (consumeComponents)
//                    {
//                        ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                            (ulong)steamid);
//                    }
//                    if (PriceSoFar > 0 && requireMoney)
//                    {
//                        EconUtils.takeMoney(identityId, PriceSoFar);
//                        PriceSoFar = 0;
//                    }
//                    if (BannedCount == blocks.Count)
//                    {
//                        ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, steamid);
//                    }
//                    return;
//                }


//                if (blocks.Count == 0)
//                {
//                    if (consumeComponents)
//                    {
//                        ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                            (ulong)steamid);
//                    }

//                    ShipyardCommands.SendMessage(prefix, "Grid repair should be complete.", Color.Green, steamid);
//                    if (PriceSoFar > 0 && requireMoney)
//                    {
//                        EconUtils.takeMoney(identityId, PriceSoFar);
//                        PriceSoFar = 0;
//                    }
//                    return;
//                }
//                foreach (VRage.Game.ModAPI.IMySlimBlock block2 in blocks)
//                {

//                    if (blockCount >= buildPerCycle)
//                    {
//                        if (consumeComponents)
//                        {
//                            ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                                (ulong)steamid);
//                        }
//                        if (PriceSoFar > 0 & requireMoney)
//                        {
//                            EconUtils.takeMoney(identityId, PriceSoFar);
//                            PriceSoFar = 0;
//                        }
//                        return;
//                    }
//                    Dictionary<string, int> convertedProj = new Dictionary<string, int>();

//                    if (projector.CanBuild(block2, true) == BuildCheckResult.OK)
//                    {

//                        ShipyardCommands.GetComponents((MyCubeBlockDefinition)block2.BlockDefinition, comps);
//                        foreach (KeyValuePair<MyDefinitionId, int> pair in comps)
//                        {
//                            if (convertedProj.ContainsKey(pair.Key.SubtypeName.ToString()))
//                            {
//                                convertedProj[pair.Key.SubtypeName.ToString()] += pair.Value;
//                            }
//                            else
//                            {
//                                convertedProj.Add(pair.Key.SubtypeName.ToString(), pair.Value);
//                            }

//                        }
//                        bool banned = false;
//                        long tempPrice = 0;
//                        BannedCount = CalcPriceAndBanned(priceMultiplier, componentCosts, convertedProj, BannedCount, ref banned, ref tempPrice);
//                        //      GroupPlugin.Log.Info($"{PriceSoFar}");
//                        if (banned)
//                        {
//                            continue;
//                        }

//                        if (requireMoney)
//                        {
//                            if (EconUtils.getBalance(identityId) >= PriceSoFar + tempPrice)
//                            {
//                                if (consumeComponents)
//                                {
//                                    if (!ShipyardCommands.CanBuild(inventories, comps,
//                                            (ulong)steamid))
//                                    {
//                                        if (PriceSoFar > 0)
//                                        {
//                                            EconUtils.takeMoney(identityId, PriceSoFar);
//                                            PriceSoFar = 0;
//                                        }
//                                        foreach (var item in removeComps)
//                                        {
//                                            GroupPlugin.Log.Info($"{item.Key} {item.Value}");
//                                        }
//                                        ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                                            (ulong)steamid);
//                                        return;
//                                    }

//                                    removeComps = comps.ToDictionary(x => x.Key, x => x.Value);
//                                }
//                                PriceSoFar += tempPrice;
//                                blockCount = BuildProjected(identityId, projector, block2, blockCount);
//                            }
//                            else
//                            {
//                                if (PriceSoFar > 0)
//                                {
//                                    EconUtils.takeMoney(identityId, PriceSoFar);
//                                    PriceSoFar = 0;
//                                }
//                                if (consumeComponents)
//                                {
//                                    ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                                        (ulong)steamid);
//                                }
//                                ShipyardCommands.SendMessage(prefix, "Cannot afford repair cost, cancelling repair. You require " + String.Format("{0:n0}", tempPrice) + " more SC.", Color.Red, steamid);
//                                return;
//                            }
//                        }
//                        else if (consumeComponents)
//                        {

//                            if (!ShipyardCommands.CanBuild(inventories, comps,
//                                    (ulong)steamid))
//                            {
//                                ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                                    (ulong)steamid);

//                                return;
//                            }
//                            removeComps = comps.ToDictionary(x => x.Key, x => x.Value); ;
//                        }


//                        blockCount = BuildProjected(identityId, projector, block2, blockCount);
//                    }

//                }

//                if (PriceSoFar > 0 & requireMoney)
//                {
//                    EconUtils.takeMoney(identityId, PriceSoFar);
//                    PriceSoFar = 0;
//                }
//            }

//            if (consumeComponents)
//            {
//                ShipyardCommands.ConsumeComponents(inventories, removeComps,
//                    (ulong)steamid);
//            }

//        }

//        private static int BuildProjected(long identityId, IMyProjector projector, IMySlimBlock block2, int blockCount)
//        {
//            projector.Build(block2, identityId, identityId, true);
//            blockCount++;
//            return blockCount;
//        }

//        private static int RepairBlock(MyCubeGrid grid, MySlimBlock block, long owner, int fixedblocks)
//        {
//            block.ClearConstructionStockpile(null);

//            block.IncreaseMountLevel(block.MaxIntegrity, owner, null, 10000, true);

//            MyCubeBlock cubeBlock = block.FatBlock;

//            if (cubeBlock != null)
//            {
//                grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
//                if (owner != 0)
//                    grid.ChangeOwnerRequest(grid, cubeBlock, owner, MyOwnershipShareModeEnum.Faction);
//            }





//            fixedblocks++;
//            return fixedblocks;
//        }


//        public class yeet
//        {
//            [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
//            public class CrunchChat : MySessionComponentBase
//            {

//                private bool _isInitialized; // Is this instance is initialized
//                public ulong TickCounter { get; private set; } // Big enough for years

//                public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
//                {
//                    base.Init(sessionComponent);

//                    // Set TickCounter always to zero at startup
//                    TickCounter = 0;
//                }

//                public override void UpdateAfterSimulation()
//                {
//                    TickCounter += 1;
//                    if (TickCounter % 32 == 0)
//                    {
//                        MyAPIGateway.Utilities.ShowMessage("debug", "1");
//                        var player = MyAPIGateway.Session.LocalHumanPlayer;
//                        if (player?.Controller?.ControlledEntity?.Entity?.Parent is MyCubeGrid)
//                        {
//                            MyAPIGateway.Utilities.ShowMessage("debug","1");
//                            var grid = player.Controller.ControlledEntity.Entity.Parent as MyCubeGrid;
//                            MyAPIGateway.Utilities.ShowMessage("debug", "2");
//                            var beacon = grid.GetFatBlocks().OfType<IMyBeacon>();
//                            MyAPIGateway.Utilities.ShowMessage("debug", "3");
//                            if (beacon.Any())
//                            {
//                                MyAPIGateway.Utilities.ShowMessage("debug", "4");
//                                var distance = beacon.First() as IMyBeacon;
                            
//                                MyAPIGateway.Utilities.ShowNotification($"Grid Signature {distance.Radius:N0}");
//                            }
//                            MyAPIGateway.Utilities.ShowMessage("debug", "5");
//                        }
//                    }
//                }
//            }
//        }

//        private static int CalcPriceAndBanned(float priceMultiplier, Dictionary<string, ComponentCost> componentCosts, Dictionary<string, int> temp,
//            int BannedCount, ref bool banned, ref long tempPrice)
//        {
//            foreach (var keyset in temp)
//            {
//                if (componentCosts.TryGetValue(keyset.Key, out var comp))
//                {
//                    if (comp.IsBannedComponent)
//                    {
//                        BannedCount++;
//                        banned = true;
//                    }

//                    tempPrice += (long)((temp.Count * comp.Cost) * priceMultiplier);
//                }
//                else
//                {
//                    tempPrice += (long)((temp.Count * 10000) * priceMultiplier);
//                }
//            }

//            return BannedCount;
//        }
//    }
//}
