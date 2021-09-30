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

namespace AlliancesPlugin.Alliances
{
   public class GridRepair
    {
        public static Dictionary<long, long> gridsToRepair = new Dictionary<long, long>();
        public static Dictionary<long, DateTime> gridCooldowns = new Dictionary<long, DateTime>();
        public static Dictionary<long, Guid> AllianceIds = new Dictionary<long, Guid>();
        public static Dictionary<int, GridRepairUpgrades> upgrades = new Dictionary<int, GridRepairUpgrades>();
        public long CalculatePriceForComponents(Dictionary<MyDefinitionId, int> components, long identityIdForAllianceChecks) 
        {



            return 0l;

        }
        
        public static void DoRepairCycle()
        {
            List<long> yeet = new List<long>();
            foreach (KeyValuePair<long, long> pair in gridsToRepair)
            {
                if (gridCooldowns.TryGetValue(pair.Key, out DateTime time))
                {
                    if (DateTime.Now >= time)
                    {
                        if (MyAPIGateway.Entities.GetEntityById(pair.Key) != null && MyAPIGateway.Entities.GetEntityById(pair.Key) is MyCubeGrid grid)
                        {
                            GridRepairUpgrades upgrade = upgrades[AlliancePlugin.GetAllianceNoLoading(AllianceIds[pair.Key]).GridRepairUpgrade];
                            if (upgrade == null)
                            {
                                AlliancePlugin.Log.Error("Could find this uppgrade id " + AlliancePlugin.GetAllianceNoLoading(AllianceIds[pair.Key]).GridRepairUpgrade + " CANCELLING REPAIR");
                                yeet.Add(pair.Key);
                                return;
                            }
                            BuildProjected(grid, pair.Value, upgrade.RepairBlocksPerCycle, upgrade.ProjectedBuildPerCycle);
                            gridCooldowns[pair.Key] = DateTime.Now.AddSeconds(upgrade.SecondsPerCycle);
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
        }

        //repair blocks on the grid first
        public static void Repair(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, ulong steamId)
        {
            long newowner = MySession.Static.Players.TryGetIdentityId(steamId);
            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes)
            {
       
                MyCubeGrid grid = groupNodes.NodeData;
            
                    if (!gridsToRepair.ContainsKey(grid.EntityId))
                    {
                        gridsToRepair.Add(grid.EntityId, newowner);
                    gridCooldowns.Add(grid.EntityId, DateTime.Now.AddSeconds(5));
                    }
                


 
            }
        }

        //now we want to build projected blocks

        public static void BuildProjected(MyCubeGrid grid, long identityId, int fixPerCycle, int buildPerCycle)
        {
            AlliancePlugin.Log.Info("Doing buildy shit");
            int blockCount = 0;
            int fixedblocks = 0;
            long PriceSoFar = 0;
            HashSet<MySlimBlock> blocks3 = grid.GetBlocks();
            foreach (MySlimBlock block in blocks3)
            {
                if (fixedblocks >= fixPerCycle)
                {
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
                    Dictionary<MyDefinitionId, int> converted = new Dictionary<MyDefinitionId, int>();

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
       
            }

            IMyCubeGrid projectedGrid = null;
            IMyProjector projector = null;
            foreach (MyProjectorBase proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
            {
                if (proj.Enabled && proj.IsFunctional && proj.ProjectedGrid != null && projector == null)
                {
                    if (projector.BlockDefinition.SubtypeName.ToLower().Contains("console"))
                    {
                        continue;

                    }
                    projectedGrid = proj.ProjectedGrid;
                    projector = proj as IMyProjector;
                }
            }
            List<VRage.Game.ModAPI.IMySlimBlock> notConnected = new List<IMySlimBlock>();
            if (projectedGrid != null)
            {
                AlliancePlugin.Log.Info("Projector");
                List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
               
                projectedGrid.GetBlocks(blocks);
                if (blocks.Count == 0)
                {
                    gridsToRepair.Remove(grid.EntityId);
                    gridCooldowns.Remove(grid.EntityId);
                    AlliancePlugin.Log.Info("No projected blocks so removing the grid from cycle");
                    return;
                }
         
                foreach (VRage.Game.ModAPI.IMySlimBlock block2 in blocks)
                {

                    if (blockCount >= buildPerCycle)
                    {
                        return;
                    }
                    if (projector.CanBuild(block2, true) == BuildCheckResult.NotConnected)
                    {
                        if (!notConnected.Contains(block2))
                        {
                            notConnected.Add(block2);
                            continue;
                        }
                    }
                  
                    if (projector.CanBuild(block2, true) == BuildCheckResult.OK)
                    {

                        Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();

                        //we still want this method to calculate SC cost

                        ShipyardCommands.GetComponents((MyCubeBlockDefinition)block2.BlockDefinition, comps);

                        //make a method to calculate price

                        projector.Build(block2, identityId, identityId, true);
                        AlliancePlugin.Log.Info("building");
                        blockCount++;
                    }
                    if (blockCount >= buildPerCycle)
                    {
                        return;
                    }
                    foreach (VRage.Game.ModAPI.IMySlimBlock block3 in notConnected)
                    {
                        if (blockCount >= buildPerCycle)
                        {
                            return;
                        }
                        if (projector.CanBuild(block3, true) == BuildCheckResult.OK)
                        {

                            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();

                            //we still want this method to calculate SC cost

                            ShipyardCommands.GetComponents((MyCubeBlockDefinition)block2.BlockDefinition, comps);

                            //make a method to calculate price

                            projector.Build(block3, identityId, identityId, true);
                            blockCount++;
                        }
                    }
                }
            }

            AlliancePlugin.Log.Info("Couldnt find projector");
        }
    }
}
