using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems;
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
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Shipyard;

namespace AlliancesPlugin.Hangar
{
    [Category("ah")]
    public class HangarCommands : CommandModule
    {
        public static Boolean IsDeniedLocation(Vector3 Position)
        {

            return false;
        }
        [Command("log", "View the hangar log")]
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
                if (AlliancePlugin.HasFailedUpkeep(alliance))
                {
                    Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
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

                        sb.AppendLine(item.time.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.steamid) + " " + item.action + " " + item.GridName.Split('_')[0]);
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
                if (AlliancePlugin.HasFailedUpkeep(alliance))
                {
                    Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
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

                            Context.Respond(slot.name.Split('_')[0] + " : " + AlliancePlugin.GetPlayerName(slot.steamid), Color.LightBlue, "[ " + i + " ]");

                        }
                        else
                        {
                            Context.Respond("", Color.Green, "[ Available ]");
                        }
                    }
                }
                else
                {
                    Context.Respond("Hangar not unlocked, !ah upgrade true");
                }

            }

        }

        public static Dictionary<int, HangarUpgrade> slotUpgrades = new Dictionary<int, HangarUpgrade>();

        //public static UpgradeCost LoadUpgradeCost(string path)
        //{

        //    if (!File.Exists(path))
        //    {

        //        return null;
        //    }
        //    UpgradeCost cost = new UpgradeCost();
        //    try
        //    {
        //        String[] line;
        //        line = File.ReadAllLines(path);
        //        cost.id = int.Parse(line[0].Split(',')[0]);
        //        cost.type = line[0].Split(',')[1];
        //        cost.NewLevel = float.Parse(line[0].Split(',')[2]);
        //        for (int i = 2; i < line.Length; i++)
        //        {

        //            String[] split = line[i].Split(',');
        //            foreach (String s in split)
        //            {
        //                s.Replace(" ", "");
        //            }
        //            if (split[0].ToLower().Contains("metapoints"))
        //            {
        //                cost.MetaPointCost += int.Parse(split[1]);
        //            }
        //            if (split[0].ToLower().Contains("money"))
        //            {
        //                cost.MoneyRequired += long.Parse(split[1]);
        //            }
        //            else
        //            {
        //                if (MyDefinitionId.TryParse(split[0], split[1], out MyDefinitionId id))
        //                {
        //                    if (cost.itemsRequired.ContainsKey(id))
        //                    {
        //                        cost.itemsRequired[id] += int.Parse(split[2]);
        //                    }
        //                    else
        //                    {
        //                        cost.itemsRequired.Add(id, int.Parse(split[2]));
        //                    }

        //                }
        //            }
        //        }
        //        switch (cost.type.ToLower())
        //        {
        //            case "slots":
        //                slotUpgrades.Add(cost.id, cost);
        //                break;
        //            default:
        //                AlliancePlugin.Log.Error("Upgrade file has no defined type");
        //                break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        AlliancePlugin.Log.Error("ERROR READING THIS FILE " + path);
        //        AlliancePlugin.Log.Error(ex);
        //    }
        //    return cost;
        //}


        [Command("upgrade", "Upgrade hangar slots")]
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
                if (AlliancePlugin.HasFailedUpkeep(alliance))
                {
                    Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                    return;
                }
                HangarData hangar = alliance.LoadHangar();

                HangarUpgrade cost = new HangarUpgrade();
                if (!upgrade)
                {


                    ShipyardCommands.SendMessage("[Alliance Hangar]", "To upgrade use !ah upgrade true ,while looking at an owned grid.", Color.Cyan, (long)Context.Player.SteamUserId);
                    StringBuilder sb = new StringBuilder();

                    if (hangar != null)
                    {
                        sb.AppendLine("Current upgrade number : " + hangar.SlotUpgradeNum);
                    }
                    else
                    {
                        sb.AppendLine("Current upgrade number : 0");
                    }

                    foreach (KeyValuePair<int, HangarUpgrade> key in slotUpgrades)
                    {
                        sb.AppendLine("Upgrade number " + key.Key);
                        if (key.Value.MoneyRequired > 0)
                        {
                            sb.AppendLine("Costs " + String.Format("{0:n0}", key.Value.MoneyRequired) + " SC.");
                        }
                        if (key.Value.MetaPointsRequired > 0)
                        {
                            sb.AppendLine("Costs " + String.Format("{0:n0}", key.Value.MetaPointsRequired) + " Meta Points.");
                        }
                        sb.AppendLine("Items required.");
                        foreach (KeyValuePair<MyDefinitionId, int> id in key.Value.getItemsRequired())
                        {
                            sb.AppendLine(id.Key.ToString() + " - " + id.Value);
                        }
                        sb.AppendLine("New Slots " + key.Value.NewSlots);
                        sb.AppendLine("");
                    }

                    DialogMessage m3 = new DialogMessage("Available upgrades", "", sb.ToString());
                    ModCommunication.SendMessageTo(m3, Context.Player.SteamUserId);

                }
                else
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



                    try
                    {
                        if (hangar == null)
                        {
                            cost = slotUpgrades[1];
                        }
                        else
                        {


                            cost = slotUpgrades[hangar.SlotUpgradeNum + 1];
                        }
                    }
                    catch (Exception)
                    {
                        Context.Respond("Cannot upgrade any further as there are no more defined upgrade files.");
                        return;
                    }
                    if (cost != null)
                    {
                        if (cost.MetaPointsRequired > 0)
                        {

                            if (alliance.CurrentMetaPoints < cost.MetaPointsRequired)
                            {
                                Context.Respond("Cannot afford the meta point cost of " + cost.MetaPointsRequired);
                                return;
                            }
                        }
                        if (cost.MoneyRequired > 0)
                        {

                            if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                            {
                                var result = ShipyardCommands.ConsumeComponents(invents, cost.getItemsRequired(),
                                    Context.Player.SteamUserId);
                                if (result.Item1)
                                {
                                    if (hangar == null)
                                    {
                                        hangar = new HangarData();
                                    }
                                    alliance.CurrentMetaPoints -= cost.MetaPointsRequired;
                                    EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                    hangar.SlotsAmount = (int)cost.NewSlots;
                                    hangar.SlotUpgradeNum++;
                                    alliance.hasUnlockedHangar = true;
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
                            var result = ShipyardCommands.ConsumeComponents(invents, cost.getItemsRequired(),
                                Context.Player.SteamUserId);
                            if (result.Item1)
                            {
                                if (hangar == null)
                                {
                                    hangar = new HangarData();
                                }
                                alliance.hasUnlockedHangar = true;
                                alliance.CurrentMetaPoints -= cost.MetaPointsRequired;
                                hangar.SlotsAmount = (int)cost.NewSlots;
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


        public static Dictionary<long, DateTime> cooldowns = new Dictionary<long, DateTime>();
        public string GetCooldownMessage(DateTime time)
        {
            var diff = time.Subtract(DateTime.Now);
            string output = String.Format("{0} Seconds", diff.Seconds) + " until command can be used.";
            return output;
        }

        [Command("reload", "reload the denied locations")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            String[] line;
            line = File.ReadAllLines(AlliancePlugin.path + "//HangarDeniedLocations.txt");
            AlliancePlugin.HangarDeniedLocations.Clear();
            for (int i = 1; i < line.Length; i++)
            {
                DeniedLocation loc = new DeniedLocation();
                String[] split = line[i].Split(',');
                foreach (String s in split)
                {
                    s.Replace(" ", "");
                }
                loc.name = split[0];
                loc.x = Double.Parse(split[1]);
                loc.y = Double.Parse(split[2]);
                loc.z = Double.Parse(split[3]);
                loc.radius = double.Parse(split[4]);
                AlliancePlugin.HangarDeniedLocations.Add(loc);
            }
            Context.Respond("Reloaded!");
        }



        [Command("load", "load a grid from alliance hangar")]
        [Permission(MyPromoteLevel.None)]
        public void LoadFromHangar(string slotNumber)
        {
            if (!AlliancePlugin.config.HangarEnabled)
            {
                Context.Respond("Alliance hangar is not enabled.");
                return;
            }
            if (cooldowns.TryGetValue(Context.Player.IdentityId, out DateTime value))
            {
                if (DateTime.Now <= value)
                {
                    Context.Respond(GetCooldownMessage(value));
                    return;
                }
                else
                {
                    cooldowns[Context.Player.IdentityId] = DateTime.Now.AddSeconds(60);
                }
            }
            else
            {
                cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
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
            if (AlliancePlugin.HasFailedUpkeep(alliance))
            {
                Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                return;
            }
            if (!alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.HangarLoad) && !alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.HangarLoadOther))
            {
                Context.Respond("Current rank does not have access to hangar load.");
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


                    gpscol.SendAddGpsRequest(Context.Player.IdentityId, ref gps);
                }




            }
            else
            {
                Context.Respond("Alliance has not unlocked the hangar to unlock use !ah upgrade true");
            }
        }
        public static FileUtils utils = new FileUtils();
        public static void LoadHangarUpgrade(string path)
        {

            if (!File.Exists(path))
            {

                return;
            }
            if (path.EndsWith(".txt"))
            {
                File.Delete(path);
                return;
            }
            HangarUpgrade upgrade = utils.ReadFromXmlFile<HangarUpgrade>(path);
            if (upgrade.Enabled && !slotUpgrades.ContainsKey(upgrade.UpgradeId))
            {
                slotUpgrades.Add(upgrade.UpgradeId, upgrade);
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
            if (MySession.Static.IsSaveInProgress)
            {
                Context.Respond("World is saving! Try again soon.");
                return;
            }
            if (cooldowns.TryGetValue(Context.Player.IdentityId, out DateTime value))
            {
                if (DateTime.Now <= value)
                {
                    Context.Respond(GetCooldownMessage(value));
                    return;
                }
                else
                {
                    cooldowns[Context.Player.IdentityId] = DateTime.Now.AddSeconds(60);
                }
            }
            else
            {
                cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
            }

            if (!AlliancePlugin.config.HangarInGravity)
            {
                if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
                {
                    Context.Respond("You cannot use this command in natural gravity!");
                    return;
                }
            }
            foreach (DeniedLocation denied in AlliancePlugin.HangarDeniedLocations)
            {
                if (Vector3.Distance(Context.Player.GetPosition(), new Vector3(denied.x, denied.y, denied.z)) <= denied.radius)
                {
                    Context.Respond("Cannot hangar here! Too close to a denied location.");
                    return;
                }
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
            if (AlliancePlugin.HasFailedUpkeep(alliance))
            {
                Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                return;
            }
            if (!alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.HangarSave))
            {
                Context.Respond("Current rank does not have access to hangar save.");
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
                int pcu = 0;
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
                                pcu += grid.BlocksPCU;
                                foreach (MyProjectorBase proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
                                {
                                    proj.Clipboard.Clear();
                                }
                                grids.Add(grid);
                                foreach (var block in grid.GetFatBlocks().OfType<MyCockpit>())
                                {
                                    if (block.Pilot != null)
                                    {
                                        block.RemovePilot();
                                    }
                                }
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
                    if (pcu >= AlliancePlugin.config.MaxHangarSlotPCU)
                    {
                        Context.Respond("PCU is greater than the configured maximum");
                        return;
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
                            if (grid != null)
                            {
                                grid.Close();
                            }
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
                Context.Respond("Alliance has not unlocked the hangar to unlock use !ah upgrade true");
            }
        }
    }
}
