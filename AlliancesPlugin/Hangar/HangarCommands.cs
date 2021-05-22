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
using VRage.Game.ModAPI;
using VRage.Groups;

namespace AlliancesPlugin
{
    [Category("ah")]
    public class HangarCommands : CommandModule
    {
        [Command("save", "save a grid to alliance hangar")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceInfo()
        {
            if (!AlliancePlugin.config.HangarEnabled)
            {
                Context.Respond("Alliance hangar is not enabled.");
                return;
            }
            Boolean console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("You must be in a faction to use alliance features.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("You are not a member of an alliance.");
                return;
            }
            if (alliance.hasUnlockedHangar)
            {
                HangarData hangar = alliance.LoadHangar();
                if (hangar == null)
                {
                    Context.Respond("Error loading the hangar.");
                    return;
                }
                MyCubeGrid baseGrid = null;
                if (hangar.getAvailableSlot() > 0)
                {
                    ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
                    List<MyCubeGrid> grids = new List<MyCubeGrid>();
                    foreach (var item in gridWithSubGrids)
                    {
                        foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                        {
                            MyCubeGrid grid = groupNodes.NodeData;

                        

                            if (grid.Projector != null)
                                continue;
                            if (FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                            {
                                grids.Add(grid);
                                if (baseGrid == null)
                                {
                                    baseGrid = grid.GetBiggestGridInGroup();
                                }
                            }
                            else
                            {
                                Context.Respond("The grid you are looking at includes a grid that isnt owned by you or a faction member.");
                                return;
                            }
                        }

                    }
                    hangar.SaveGridToHangar(baseGrid.DisplayNameText + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now), Context.Player.SteamUserId, alliance, Context.Player.Character.PositionComp.GetPosition(), fac, grids);
                }
                else
                {
                    Context.Respond("Hangar is full, to upgrade use !ah upgrade true.");
                }
            }
            else
            {
                Context.Respond("Alliance has not unlocked the hangar to unlock use !ah unlock.");
            }
        }
    }
}
