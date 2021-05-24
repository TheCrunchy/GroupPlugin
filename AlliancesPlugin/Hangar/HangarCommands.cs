using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace AlliancesPlugin
{
    [Category("ah")]
    public class HangarCommands : CommandModule
    {

        public static Boolean IsDeniedLocation(Vector3 Position)
        {

            return false;
        }
        [Command("log", "View the bank log")]
        [Permission(MyPromoteLevel.None)]
        public void BankLog(string timeformat = "MM-dd-yyyy")
        {

            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("You must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }
                if (alliance.hasUnlockedHangar)
                {
                    HangarData data = alliance.LoadHangar();
                    HangarLog log = data.GetHangarLog(alliance);
                    StringBuilder sb = new StringBuilder();
                    log.log.Reverse();
                    foreach (HangarLogItem item in log.log)
                    {

                        sb.AppendLine(item.time.ToString(timeformat) + " : " + MyMultiplayer.Static.GetMemberName(item.steamid) + " " + item.action + " " + item.GridName.Split('_')[0]);
                        continue;
                    }
                    DialogMessage m = new DialogMessage("Alliance Hangar Records", alliance.name, sb.ToString());
                    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                }
                else
                {
                    Context.Respond("Alliance has not unlocked hangar.");
                }
            }
        
        }

        [Command("list", "View the hangar")]
        [Permission(MyPromoteLevel.None)]
        public void HangarList()
        {
            if (!AlliancePlugin.config.HangarEnabled)
            {
                Context.Respond("Hangar not enabled.");
                return;
            }
            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("Must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked hangar.");
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
                    Context.Respond("Hangar Slots available : " + hangar.SlotsAmount, Color.LightBlue, "Alliance Hangar");
                    for (int i = 1; i <= hangar.SlotsAmount; i++)
                    {
                        if (hangar.ItemsInHangar.ContainsKey(i))
                        {
                            hangar.ItemsInHangar.TryGetValue(i, out HangarItem slot);
                         
                               Context.Respond(slot.name.Split('_')[0] + " : " + MyMultiplayer.Static.GetMemberName(slot.steamid), Color.LightBlue, "[ " + i + " ]");
                            
                        }
                        else
                        {
                            Context.Respond("",  Color.Green,"[ Available ]");
                        }
                    }
                }

            }

        }

        public static Dictionary<int, UpgradeCost> slotUpgrades = new Dictionary<int, UpgradeCost>();

        public static UpgradeCost LoadUpgradeCost(string path)
        {

            if (!File.Exists(path))
            {

                return null;
            }
            UpgradeCost cost = new UpgradeCost();
            String[] line;
            line = File.ReadAllLines(path);
            cost.id = int.Parse(line[0].Split(',')[0]);
            cost.type = line[0].Split(',')[1];
            cost.NewLevel = float.Parse(line[0].Split(',')[2]);
            for (int i = 2; i < line.Length; i++)
            {

                String[] split = line[i].Split(',');
                foreach (String s in split)
                {
                    s.Replace(" ", "");
                }
                if (split[0].ToLower().Contains("money"))
                {
                    cost.MoneyRequired += int.Parse(split[1]);
                }
                else
                {
                    if (MyDefinitionId.TryParse(split[0], split[1], out MyDefinitionId id))
                    {
                        if (cost.itemsRequired.ContainsKey(id))
                        {
                            cost.itemsRequired[id] += int.Parse(split[2]);
                        }
                        else
                        {
                            cost.itemsRequired.Add(id, int.Parse(split[2]));
                        }

                    }
                }
            }
            switch (cost.type.ToLower())
            {
                case "slots":
                    slotUpgrades.Add(cost.id, cost);
                    break;
                default:
                    AlliancePlugin.Log.Error("Upgrade file has no defined type");
                    break;
            }
            return cost;
        }


        [Command("upgrade", "Upgrade shipyard slots")]
        [Permission(MyPromoteLevel.None)]
        public void Upgrade(Boolean upgrade = false)
        {
            if (!AlliancePlugin.config.HangarEnabled)
            {
                Context.Respond("Hangar not enabled.");
                return;
            }
            if (Context.Player != null)
            {
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {

                    ShipyardCommands.SendMessage("[Hangar]", " You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }
    
                if (!alliance.hasUnlockedHangar)
                {
                   ShipyardCommands.SendMessage("[Alliance Hangar]", "To unlock use !ah unlock", Color.Cyan, (long)Context.Player.SteamUserId);
                    return;
                }
                HangarData hangar = alliance.LoadHangar();
                if (!upgrade)
                {
                    UpgradeCost cost = new UpgradeCost();
               
                    ShipyardCommands.SendMessage("[Alliance Hangar]", "To upgrade use !ah upgrade true ,while looking at an owned grid.", Color.Cyan, (long)Context.Player.SteamUserId);
                    StringBuilder sb = new StringBuilder();


                            try
                            {
                                cost = slotUpgrades[hangar.SlotUpgradeNum += 1];
                            }
                            catch (Exception ex)
                            {
                                Context.Respond("Cannot upgrade any further as there are no more defined upgrade files.");
                                return;
                            }
                            if (cost != null)
                            {
                                if (cost.MoneyRequired > 0)
                                {
                            ShipyardCommands.SendMessage("[Alliance Hangar]", "SC Cost for next speed upgrade " + String.Format("{0:n0}", cost.MoneyRequired), Color.Cyan, (long)Context.Player.SteamUserId);
                                }

                                sb.AppendLine("Items required.");
                                foreach (KeyValuePair<MyDefinitionId, int> id in cost.itemsRequired)
                                {
                                    sb.AppendLine(id.Key.ToString() + " - " + id.Value);
                                }
                                Context.Respond(sb.ToString());
                            }

                        
                else
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

                            if (hangar.SlotsAmount >= AlliancePlugin.config.MaxHangarSlots)
                            {
                                Context.Respond("Cannot upgrade any further");
                                return;
                            }

                            try
                            {
                                cost = slotUpgrades[hangar.SlotUpgradeNum +=1];
                            }
                            catch (Exception)
                            {
                                Context.Respond("Cannot upgrade any further as there are no more defined upgrade files.");
                                return;
                            }
                            if (cost != null)
                            {

                                if (cost.MoneyRequired > 0)
                                {
                                    if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                                    {
                                        if (ShipyardCommands.ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                        {
                                            EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                        hangar.SlotsAmount = (int) cost.NewLevel;
                                        hangar.SlotUpgradeNum++;
                                        hangar.SaveHangar(alliance);
                                        AlliancePlugin.SaveAllianceData(alliance);
                                        ShipyardCommands.SendMessage("[Alliance Hangar]", "Upgrading slots. You were charged: " + String.Format("{0:n0}", cost.MoneyRequired), Color.LightBlue, (long)Context.Player.SteamUserId);
                                        }
                                    }
                                    else
                                    {
                                    ShipyardCommands.SendMessage("[Alliance Hangar]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                                    }
                                }
                                else
                                {
                                    if (ShipyardCommands.ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                    {
                                    hangar.SlotsAmount = (int) cost.NewLevel;
                                    hangar.SlotUpgradeNum++;
                                    hangar.SaveHangar(alliance);
                                    AlliancePlugin.SaveAllianceData(alliance);
                                }
                                }
                            }
                            else
                            {
                                Context.Respond("Error loading upgrade details.");
                                return;
                            }
                    }

                }
            }
        }

        [Command("unlock", "save a grid to alliance hangar")]
        [Permission(MyPromoteLevel.None)]
        public void UnlockHangar()
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
            if (!alliance.hasUnlockedHangar)
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
                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());

                UpgradeCost cost = new UpgradeCost();
                List<VRage.Game.ModAPI.IMyInventory> invents = new List<VRage.Game.ModAPI.IMyInventory>();
                foreach (MyCubeGrid grid in grids)
                {
                    invents.AddList(ShipyardCommands.GetInventories(grid));
                }


                cost = ShipyardCommands.LoadUnlockCost(AlliancePlugin.path + "//HangarUnlockCost.txt");
                if (cost != null)
                {

                    if (cost.MoneyRequired > 0)
                    {
                        if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                        {
                            if (ShipyardCommands.ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                            {
                                EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                alliance.hasUnlockedHangar = true;
                                HangarData hangar = alliance.LoadHangar();

                                AlliancePlugin.SaveAllianceData(alliance);
                                ShipyardCommands.SendMessage("[Alliance Hangar]", "Unlocking the hangar. You were charged: " + String.Format("{0:n0}", cost.MoneyRequired) + " and components taken", Color.Green, (long)Context.Player.SteamUserId);

                            }
                        }
                        else
                        {
                            ShipyardCommands.SendMessage("[Alliance Hangar]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        if (ShipyardCommands.ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                        {
                            alliance.hasUnlockedHangar = true;
                            HangarData hangar = alliance.LoadHangar();

                            AlliancePlugin.SaveAllianceData(alliance);
                        }
                    }
                }
                else
                {
                    Context.Respond("Error loading upgrade details.");
                    return;
                }
            }
        }
        [Command("load", "save a grid to alliance hangar")]
        [Permission(MyPromoteLevel.None)]
        public void LoadFromHangar(string slotNumber)
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
                int slot;
                try
                {
                    slot = int.Parse(slotNumber);
                }
                catch (Exception)
                {
                    Context.Respond("Cannot parse that number.");
                    return;
                }
                if (!hangar.ItemsInHangar.ContainsKey(slot))
                {
                    Context.Respond("No grid available to load for that number!");
                    return;
                }
                HangarItem item = hangar.ItemsInHangar[slot];

                //this took up way too much of one line
                if (item.steamid.Equals(Context.Player.SteamUserId) || alliance.SupremeLeader.Equals(Context.Player.SteamUserId)
                    || alliance.officers.Contains(Context.Player.SteamUserId) || alliance.admirals.Contains(Context.Player.SteamUserId)){
                  if (hangar.LoadGridFromHangar(slot, Context.Player.SteamUserId, alliance, Context.Player.Identity as MyIdentity, fac))
                    {
                        Context.Respond("Grid should be loaded!");
                    }
                  else
                    {
                        Context.Respond("Could not load, are there enemies within 15km?");
                        MyGps gps = new MyGps
                        {
                            Coords = item.position,
                            Name = item.name + " Failed load location",
                            DisplayName = item.name + " Failed load location",
                            Description = "Failed load location",
                            GPSColor = Color.LightBlue,
                            IsContainerGPS = true,
                            ShowOnHud = true,
                            DiscardAt = new TimeSpan(50000)
                        };
                        gps.UpdateHash();
                        MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;


                        gpscol.SendAddGps(Context.Player.IdentityId, ref gps);
                    }

                }
                else {
                    Context.Respond("That is not your grid and you do not have the rank to load it.");
                }


            }
            else
            {
                Context.Respond("Alliance has not unlocked the hangar to unlock use !ah unlock.");
            }
        }
        [Command("save", "save a grid to alliance hangar")]
        [Permission(MyPromoteLevel.None)]
        public void SaveToHangar()
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
                string name = "";
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
                                Context.Respond(grid.DisplayName);
                                if (name == "")
                                {
                                    name = grid.DisplayName;
                                    name = name.Replace("/", "");
                                    name = name.Replace("-", "");
                                    name = name.Replace("\\", "");
                                }
                            }
                            else
                            {
                                Context.Respond("The grid you are looking at includes a grid that isnt owned by you or a faction member.");
                                return;
                            }
                        }

                    }
                    if (grids.Count == 0)
                    {
                        Context.Respond("Could not find grid.");
                        return;
                    }
                    if (name == "")
                    {
                        name = "Temporary Name";

                    }
                    bool result = hangar.SaveGridToHangar(name + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now), Context.Player.SteamUserId, alliance, Context.Player.Character.PositionComp.GetPosition(), fac, grids, Context.Player.IdentityId);
                    if (!result)
                    {
                        Context.Respond("Could not save. Are enemies within 15km?");
                    }
                    else
                    {
                        Context.Respond("Grid saved.");
                        foreach (MyCubeGrid grid in grids)
                        {
                            grid.Close();
                        }
                    }
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
