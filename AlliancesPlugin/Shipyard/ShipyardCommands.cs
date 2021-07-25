using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Session;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using Torch.API.Managers;
using Torch.Server.ViewModels;
using Sandbox.Engine.Multiplayer;
using Torch.ViewModels;
using Torch.Managers;
using Sandbox.Game;
using Sandbox.Definitions;
using System.Collections.Concurrent;
using VRage.Groups;
using System.Globalization;
using Sandbox.ModAPI.Ingame;
using System.Threading;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;
using Sandbox.Game.Entities.Blocks;
using System.Text.RegularExpressions;
using System.IO;
using Sandbox.Game.GameSystems;
using System.Collections;
using VRage;
using Torch.Mod.Messages;
using Torch.Mod;
using AlliancesPlugin.Alliances;

namespace AlliancesPlugin.Shipyard
{
    [Category("shipyard")]
    public class ShipyardCommands : CommandModule
    {
        public static void SendMessage(string author, string message, Color color, long steamID)
        {


            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }

        static Dictionary<long, string> confirmationSave = new Dictionary<long, string>();
        static Dictionary<long, string> confirmationBuild = new Dictionary<long, string>();
        [Command("reload", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig()
        {
            AlliancePlugin.ReloadShipyard();

            Context.Respond("Reloaded config");
        }
        private static Logger _log = LogManager.GetCurrentClassLogger();


        //i cant remember if i even use this anymore
        //adapted from https://steamcommunity.com/sharedfiles/filedetails/?id=1840862148 with permission
        private Int64 priceWorth(MyCubeBlockDefinition.Component component)
        {
            MyBlueprintDefinitionBase bpDef = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(component.Definition.Id);
            int p = 0;
            float price = 0;
            //calculate by the minimal price per unit for modded components, vanilla is aids
            if (component.Definition.MinimalPricePerUnit > 1)
            {
                Int64 amn = Math.Abs(component.Definition.MinimalPricePerUnit);
                price = price + amn;
            }
            //if keen didnt give the fucker a minimal price calculate by the ores that make up the ingots, because fuck having an integer for an economy right?
            else
            {
                for (p = 0; p < bpDef.Prerequisites.Length; p++)
                {
                    if (bpDef.Prerequisites[p].Id != null)
                    {
                        MyDefinitionBase oreDef = MyDefinitionManager.Static.GetDefinition(bpDef.Prerequisites[p].Id);
                        if (oreDef != null)
                        {
                            MyPhysicalItemDefinition ore = oreDef as MyPhysicalItemDefinition;
                            float amn = Math.Abs(ore.MinimalPricePerUnit);
                            float count = (float)bpDef.Prerequisites[p].Amount;
                            amn = (float)Math.Round(amn * count * 3);
                            price = price + amn;
                        }
                    }
                }
            }
            return Convert.ToInt64(price);
        }

        //adapted from https://steamcommunity.com/sharedfiles/filedetails/?id=1840862148 with permission
        private GridCosts GetComponentsAndCost(VRage.Game.ModAPI.IMyCubeGrid grid)
        {
            Int64 creditCosts = 0;
            Dictionary<MyDefinitionId, int> componentCosts = new Dictionary<MyDefinitionId, int>();
            componentCosts = GetComponents(grid);
            Dictionary<string, MyItemType> componentItemTypes = new Dictionary<string, MyItemType>();
            var blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
            int i = 0;
            grid.GetBlocks(blocks, b => b != null);

            GridCosts costs = new GridCosts();
            costs.setComponents(componentCosts);
            costs.setCredits(creditCosts);
            return costs;
        }


        [Command("adminlog", "View the queue")]
        [Permission(MyPromoteLevel.Admin)]
        public void ShipyardAdminInfo(string tag)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }


            //     DialogMessage m = new DialogMessage("Alliance Info", alliance.name, alliance.OutputAlliance());
            //    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        [Command("log", "View the queue")]
        [Permission(MyPromoteLevel.None)]
        public void ShipyardLog(string timeformat = "MM-dd-yyyy")
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
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
                    SendMessage("[Shipyard]", "You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
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
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                PrintLog log = queue.GetLog(alliance);
                StringBuilder sb = new StringBuilder();
                log.log.Reverse();
                foreach (PrintLogItem item in log.log)
                {
                    if (!item.Claimed)
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + ": " + AlliancePlugin.GetPlayerName(item.SteamId) + " Started " + item.Grid.Split('_')[0]);
                    }
                    else
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + ": " + AlliancePlugin.GetPlayerName(item.SteamId) + " Claimed " + item.Grid.Split('_')[0]);
                    }
                }
                DialogMessage m = new DialogMessage("Shipyard Log", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }

        }
        [Command("info", "View the queue")]
        [Permission(MyPromoteLevel.None)]
        public void ShipyardInfo()
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
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
                    SendMessage("[Shipyard]", "You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
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
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                SendMessage("[Shipyard]", "Shipyard slots: " + queue.upgradeSlots, Color.Cyan, (long)Context.Player.SteamUserId);
                if (speedUpgrades.ContainsKey(queue.SpeedUpgrade))
                {
                    SendMessage("[Shipyard]", "Shipyard speed: blockCount * base speed * " + speedUpgrades[queue.SpeedUpgrade].NewLevel, Color.Cyan, (long)Context.Player.SteamUserId);
                }
                else
                {
                    SendMessage("[Shipyard]", "Shipyard speed: blockCount * base speed * " + queue.upgradeSpeed, Color.Cyan, (long)Context.Player.SteamUserId);
                }
   
                for (int i = 1; i <= queue.upgradeSlots; i++)
                {
                    if (queue.getQueue().ContainsKey(i))
                    {
                        queue.getQueue().TryGetValue(i, out PrintQueueItem slot);
                        DateTime start = slot.startTime;
                        DateTime end = slot.endTime;
                        if (start == null || end == null || slot.name == null)
                        {
                            SendMessage("[ " + i + " ]", "Broken slot delete with !shipyard delete " + i, Color.Green, (long)Context.Player.SteamUserId);
                            continue;
                        }
                        if (DateTime.Now > end)
                        {
                            SendMessage("[ " + i + " ]", slot.name.Split('_')[0] + " : " + slot.ownerName + " : Claim with !shipyard claim " + i, Color.Green, (long)Context.Player.SteamUserId);
                        }
                        else
                        {
                            var diff = end.Subtract(DateTime.Now);
                            string time = String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds);
                            SendMessage("[ " + i + " ]", slot.name.Split('_')[0] + " : " + slot.ownerName + " : Ready in: " + time, Color.Red, (long)Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        SendMessage("[ Y ]", i.ToString() + " : Empty - Can use", Color.Green, (long)Context.Player.SteamUserId);
                    }
                }
            }

        }
        public static Dictionary<int, UpgradeCost> speedUpgrades = new Dictionary<int, UpgradeCost>();
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
                if (split[0].ToLower().Contains("metapoints"))
                {
                    cost.MetaPointCost += int.Parse(split[1]);
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
                case "speed":
                    speedUpgrades.Add(cost.id, cost);
                    break;
                case "slots":
                    slotUpgrades.Add(cost.id, cost);
                    break;
                default:
                    AlliancePlugin.Log.Error("Upgrade file has no defined type");
                    break;
            }
            return cost;
        }

        public static UpgradeCost LoadUnlockCost(string path)
        {
            if (!File.Exists(path))
            {

                return null;
            }
            UpgradeCost cost = new UpgradeCost();
            String[] line;
            line = File.ReadAllLines(path);
            for (int i = 1; i < line.Length; i++)
            {

                String[] split = line[i].Split(',');
                foreach (String s in split)
                {
                    s.Replace(" ", "");
                }
                if (split[0].ToLower().Contains("metapoints"))
                {
                    cost.MetaPointCost += int.Parse(split[1]);
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
            return cost;
        }
        [Command("unlock", "Unlock the shipyard")]
        [Permission(MyPromoteLevel.None)]
        public void Unlock()
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            if (Context.Player != null)
            {
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {

                    SendMessage("[Shipyard]", " You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
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
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    queue = new PrintQueue();
                    queue.allianceId = alliance.AllianceId;

                }
                if (!alliance.hasUnlockedShipyard)
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
                        invents.AddList(GetInventories(grid));
                    }


                    cost = LoadUnlockCost(AlliancePlugin.path + "//ShipyardUnlockCost.txt");
                    if (cost != null)
                    {
                        if (cost.MetaPointCost > 0)
                        {
                            if (alliance.CurrentMetaPoints < cost.MetaPointCost)
                            {
                                Context.Respond("Cannot afford the meta point cost of " + cost.MetaPointCost);
                                return;
                            }
                        }
                        if (cost.MoneyRequired > 0)
                        {
                            if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                            {
                                if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                {
                                    alliance.CurrentMetaPoints -= cost.MetaPointCost;
                                    EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                    alliance.hasUnlockedShipyard = true;
                                    queue.upgradeSlots = 1;
                                    alliance.SavePrintQueue(queue);
                                    AlliancePlugin.SaveAllianceData(alliance);
                                    SendMessage("[Shipyard]", "Unlocking the shipyard. You were charged: " + String.Format("{0:n0}", cost.MoneyRequired) + " and components taken", Color.Green, (long)Context.Player.SteamUserId);

                                }
                            }
                            else
                            {
                                SendMessage("[Shipyard]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                            }
                        }
                        else
                        {
                            if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                            {
                                alliance.hasUnlockedShipyard = true;
                                queue.upgradeSlots = 1;
                                alliance.CurrentMetaPoints -= cost.MetaPointCost;
                                alliance.SavePrintQueue(queue);
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

        [Command("upgrade", "Upgrade shipyard slots")]
        [Permission(MyPromoteLevel.None)]
        public void Upgrade(string type = "", Boolean upgrade = false)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            if (Context.Player != null)
            {
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {

                    SendMessage("[Shipyard]", " You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
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
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    queue = new PrintQueue();
                    queue.allianceId = alliance.AllianceId;

                }
                if (!alliance.hasUnlockedShipyard)
                {
                    SendMessage("[Shipyard]", "To upgrade use !shipyard unlock", Color.Cyan, (long)Context.Player.SteamUserId);
                    return;
                }
                if (!upgrade)
                {
                    UpgradeCost cost = new UpgradeCost();
                    String[] line;
                    SendMessage("[Shipyard]", "To upgrade use !shipyard upgrade speed/slots true", Color.Cyan, (long)Context.Player.SteamUserId);
                    StringBuilder sb = new StringBuilder();

                    //split this shit into methods
                    switch (type.ToLower())
                    {
                        case "speed":
                            if (queue.upgradeSpeed <= AlliancePlugin.shipyardConfig.MaxSpeedReduction)
                            {
                                Context.Respond("Cannot upgrade any further");
                                return;
                            }
                           // Context.Respond(queue.SpeedUpgrade + " ");

                            try
                            {
                                cost = speedUpgrades[queue.SpeedUpgrade += 1];
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
                                    SendMessage("[Shipyard]", "SC Cost for next speed upgrade " + String.Format("{0:n0}", cost.MoneyRequired), Color.Cyan, (long)Context.Player.SteamUserId);
                                }
                                if (cost.MetaPointCost > 0)
                                {
                                    
                                        SendMessage("[Shipyard]", "Metapoint cost for next speed upgrade " + cost.MetaPointCost, Color.Cyan, (long)Context.Player.SteamUserId);
                                  
                                    
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
                                Context.Respond("Error loading upgrade details.");
                                return;
                            }
                            break;
                        case "slots":
                            if (queue.upgradeSlots >= AlliancePlugin.shipyardConfig.MaxShipyardSlots)
                            {
                                Context.Respond("Cannot upgrade any further");
                                return;
                            }


                            try
                            {
                                cost = slotUpgrades[queue.SlotsUpgrade += 1];
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
                                    SendMessage("[Shipyard]", "SC Cost for next slot upgrade " + String.Format("{0:n0}", cost.MoneyRequired), Color.Cyan, (long)Context.Player.SteamUserId);
                                }
                                if (cost.MetaPointCost > 0)
                                {

                                    SendMessage("[Shipyard]", "Metapoint cost for next slot upgrade " + cost.MetaPointCost, Color.Cyan, (long)Context.Player.SteamUserId);
                                    

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
                                Context.Respond("Error loading upgrade details.");
                                return;
                            }
                            break;
                        default:
                            Context.Respond("Use !shipyard upgrade speed or slots");
                            return;
                    }
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
                    //Do stuff with taking components from grid storage
                    //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                    //gridCosts.setComponents(localGridCosts.getComponents());
                    UpgradeCost cost = new UpgradeCost();
                    List<VRage.Game.ModAPI.IMyInventory> invents = new List<VRage.Game.ModAPI.IMyInventory>();
                    foreach (MyCubeGrid grid in grids)
                    {
                        invents.AddList(GetInventories(grid));
                    }
                    String[] line;
                    switch (type.ToLower())
                    {
                        case "speed":
                            if (queue.upgradeSpeed <= AlliancePlugin.shipyardConfig.MaxSpeedReduction)
                            {
                                Context.Respond("Cannot upgrade any further");
                                return;
                            }
                            int upgradeNum = Convert.ToInt16(AlliancePlugin.shipyardConfig.StartingSpeedMultiply - queue.upgradeSpeed);

                            try
                            {
                                cost = speedUpgrades[queue.SpeedUpgrade += 1];
                            }
                            catch (Exception)
                            {
                                Context.Respond("Cannot upgrade any further as there are no more defined upgrade files.");
                                return;
                            }
                            if (cost != null)
                            {
                                if (cost.MetaPointCost > 0)
                                {
                                    if (alliance.CurrentMetaPoints < cost.MetaPointCost)
                                    {
                                        Context.Respond("Cannot afford the meta point cost of " + cost.MetaPointCost);
                                        return;
                                    }
                                }
                                if (cost.MoneyRequired > 0)
                                {
                                    if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                                    {
                                        if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                        {
                                            alliance.CurrentMetaPoints -= cost.MetaPointCost;
                                            EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                            queue.upgradeSpeed = (float)cost.NewLevel;
                                            queue.SpeedUpgrade++;
                                            alliance.SavePrintQueue(queue);
                                            AlliancePlugin.SaveAllianceData(alliance);
                                            SendMessage("[Shipyard]", "Upgrading speed decrease. You were charged: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Green, (long)Context.Player.SteamUserId);
                                        }
                                    }
                                    else
                                    {
                                        SendMessage("[Shipyard]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                                    }
                                }
                                else
                                {
                                    if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                    {
                                        alliance.CurrentMetaPoints -= cost.MetaPointCost;
                                        queue.upgradeSpeed = (float)cost.NewLevel;
                                        queue.SpeedUpgrade++;
                                        alliance.SavePrintQueue(queue);
                                        AlliancePlugin.SaveAllianceData(alliance);
                                    }
                                }
                            }
                            else
                            {
                                Context.Respond("Error loading upgrade details.");
                                return;
                            }
                            break;
                        case "slots":
                            if (queue.upgradeSlots >= AlliancePlugin.shipyardConfig.MaxShipyardSlots)
                            {
                                Context.Respond("Cannot upgrade any further");
                                return;
                            }

                            try
                            {
                                cost = slotUpgrades[queue.SlotsUpgrade += 1];
                            }
                            catch (Exception)
                            {
                                Context.Respond("Cannot upgrade any further as there are no more defined upgrade files.");
                                return;
                            }
                            if (cost != null)
                            {
                                if (cost.MetaPointCost > 0)
                                {
                                    if (alliance.CurrentMetaPoints < cost.MetaPointCost)
                                    {
                                        Context.Respond("Cannot afford the meta point cost of " + cost.MetaPointCost);
                                        return;
                                    }
                                }
                                if (cost.MoneyRequired > 0)
                                {
                                    if (EconUtils.getBalance(Context.Player.IdentityId) >= cost.MoneyRequired)
                                    {
                                        if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                        {
                                            alliance.CurrentMetaPoints -= cost.MetaPointCost;
                            
                                           
                                            EconUtils.takeMoney(Context.Player.IdentityId, cost.MoneyRequired);
                                            queue.upgradeSlots = (int)cost.NewLevel;
                                            queue.SlotsUpgrade++;
                                            alliance.SavePrintQueue(queue);
                                            AlliancePlugin.SaveAllianceData(alliance);
                                            SendMessage("[Shipyard]", "Upgrading slot increase. You were charged: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Green, (long)Context.Player.SteamUserId);
                                        }
                                    }
                                    else
                                    {
                                        SendMessage("[Shipyard]", "You cant afford the upgrade price of: " + String.Format("{0:n0}", cost.MoneyRequired), Color.Red, (long)Context.Player.SteamUserId);
                                    }
                                }
                                else
                                {
                                    if (ConsumeComponents(invents, cost.itemsRequired, Context.Player.SteamUserId))
                                    {
                                        alliance.CurrentMetaPoints -= cost.MetaPointCost;
                                        queue.upgradeSlots = (int)cost.NewLevel;
                                        alliance.SavePrintQueue(queue);
                                        queue.SlotsUpgrade++;
                                        AlliancePlugin.SaveAllianceData(alliance);
                                    }
                                }
                            }
                            else
                            {
                                Context.Respond("Error loading upgrade details.");
                                return;
                            }
                            break;
                        default:
                            Context.Respond("Use !shipyard upgrade speed or slots");
                            return;
                    }

                }
            }
        }
        [Command("restore", "Admin force spawn a grid")]
        [Permission(MyPromoteLevel.Admin)]
        public void ClaimPrintAdmin(string factionTag, string name, string targetPlayerName)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            IMyFaction faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            MyPlayer player = Sync.Players.GetPlayerByName(targetPlayerName);
            if (player == null)
            {
                Context.Respond("Cant find that player.");
            }
            if (faction == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
            if (alliance == null)
            {
                Context.Respond("Target faction not member of alliance.");
                return;
            }
            if (AlliancePlugin.HasFailedUpkeep(alliance))
            {
                Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                return;
            }
            PrintQueue queue = alliance.LoadPrintQueue();
            if (queue == null)
            {
                SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                return;
            }
            if (File.Exists(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + name + ".xml"))
            {
                if (GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + name + ".xml", player.GetPosition(), false, player.Id.SteamId, name))
                {
                    Context.Respond("Loaded grid");
                }
                else
                {
                    Context.Respond("Could not load the grid.");
                }
            }
            else
            {
                Context.Respond("Cant find that file, are you using the full name including the timestamp?");
            }
        }
        [Command("purge", "delete data for grids that have been claimed")]
        [Permission(MyPromoteLevel.Admin)]
        public void PurgeClaimedPrints()
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            int purged = 0;
            foreach (Alliance alliance in AlliancePlugin.AllAlliances.Values)
            {
                if (!alliance.hasUnlockedShipyard)
                    continue;

                PrintQueue queue = alliance.LoadPrintQueue();
                int beforePurge = 0;
                List<String> purgedString = new List<string>();
                if (queue != null)
                {
                    foreach (String s in queue.claimedGrids)
                    {
                        beforePurge++;
                        if (File.Exists(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + s + ".xml"))
                        {
                            File.Delete(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + s + ".xml");
                            purged++;
                        }
                        if (File.Exists(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\COSTS_") + s + ".json"))
                        {
                            File.Delete(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\COSTS_") + s + ".json");

                        }
                        purgedString.Add(s);

                    }
                    foreach (String s in purgedString)
                    {
                        queue.claimedGrids.Remove(s);
                    }
                }
                if (beforePurge > 0)
                {
                    alliance.SavePrintQueue(queue);
                }
            }
            Context.Respond("Purged " + purged + " claimed grids.");
        }
        [Command("fee", "change the fee")]
        [Permission(MyPromoteLevel.None)]
        public void ChangeFee(string inputAmount)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            if (Context.Player != null)
            {


                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                int slot;
                if (faction == null)
                {
                    SendMessage("[Shipyard]", "You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
 

                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("Target faction not member of alliance.");
                    return;
                }
                if (AlliancePlugin.HasFailedUpkeep(alliance))
                {
                    Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                    return;
                }
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.BankWithdraw))
                {
                    alliance.ShipyardFee = amount;
                    AlliancePlugin.SaveAllianceData(alliance);
                    Context.Respond("Fee changed.");
                }
                else {
                    Context.Respond("You do not have permission to change the fee!, Must have bank withdraw perms.");
                }
            }
        }
        [Command("delete", "delete a print")]
        [Permission(MyPromoteLevel.None)]
        public void DeletePrint(string slotNumber)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            if (Context.Player != null)
            {
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                int slot;
                if (faction == null)
                {
                    SendMessage("[Shipyard]", "You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                try
                {
                    slot = int.Parse(slotNumber);
                }
                catch (Exception)
                {
                    SendMessage("[Shipyard]", "Cannot parse that number.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("Target faction not member of alliance.");
                    return;
                }
                if (!alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaim) && !alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaimOther))
                {
                    Context.Respond("Current rank does not have access to claim shipyard builds.");
                    return;
                }
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                PrintQueueItem item;
                queue.getQueue().TryGetValue(slot, out item);
                if (item.name == null || item.startTime == null || item.endTime == null)
                {
                    if (item.ownerSteam.Equals((long)Context.Player.SteamUserId) || alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaimOther))
                    {
                        queue.removeFromQueue(slot);
                        alliance.SavePrintQueue(queue);
                        PrintLog log = queue.GetLog(alliance);
                        PrintLogItem newLog = new PrintLogItem();
                        newLog.Claimed = true;
                        newLog.Grid = item.name;
                        newLog.SteamId = Context.Player.SteamUserId;
                        newLog.TimeClaimed = DateTime.Now;
                        log.log.Add(newLog);
                        queue.SaveLog(alliance, log);
                        Context.Respond("Deleted.");
                    }
                    else
                    {
                        SendMessage("[Shipyard]", "That isnt yours to claim and you do not have the officer override.", Color.Red, (long)Context.Player.SteamUserId);
                        return;
                    }
                }
                else
                {
                    Context.Respond("This slot doesnt appear to be broken.");
                }
                // 
                //   

            }

        }


        [Command("claim", "Claim a print")]
        [Permission(MyPromoteLevel.None)]
        public void ClaimPrint(string slotNumber)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            if (Context.Player != null)
            {
                if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
                {
                    SendMessage("[Shipyard]", "You cannot use this command in natural gravity!", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                int slot;
                if (faction == null)
                {
                    SendMessage("[Shipyard]", "You arent in a faction.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                try
                {
                    slot = int.Parse(slotNumber);
                }
                catch (Exception)
                {
                    SendMessage("[Shipyard]", "Cannot parse that number.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("Target faction not member of alliance.");
                    return;
                }
                if (AlliancePlugin.HasFailedUpkeep(alliance))
                {
                    Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                    return;
                }
                if (!alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaim) && !alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaimOther))
                {
                    Context.Respond("Current rank does not have access to claim shipyard builds.");
                    return;
                }
                PrintQueue queue = alliance.LoadPrintQueue();
                if (queue == null)
                {
                    SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                PrintQueueItem item;
                queue.getQueue().TryGetValue(slot, out item);

                if (item.ownerSteam.Equals((long)Context.Player.SteamUserId) || alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaimOther))
                {
               
                    DateTime start = item.startTime;
                    DateTime end = item.endTime;
                    if (start == null || end == null || item.name == null)
                    {
                        queue.removeFromQueue(slot);
                        alliance.SavePrintQueue(queue);
                        SendMessage("[ " + slot + " ]", "Bugged grid deleted from queue.", Color.Green, (long)Context.Player.SteamUserId);
                        return;
                    }
                    if (!item.ownerSteam.Equals((long)Context.Player.SteamUserId) && alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardClaimOther)){
                        end.AddDays(3);
                    }
                    if (DateTime.Now > end)
                    {

                        if (AlliancePlugin.shipyardConfig.NearStartPointToClaim)
                        {
                            Vector3D position = new Vector3D(item.x, item.y, item.z);
                            float distance = Vector3.Distance(Context.Player.Character.GetPosition(), position);
                            if (distance > 2000)
                            {
                                Context.Respond("Must be within 2km of the start position.");
                                    MyAPIGateway.Session?.GPS.AddGps(Context.Player.Identity.IdentityId, item.GetGps());
                                
                              
                                return;
                            }
                        }
                        if (GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + item.name + ".xml", Context.Player.GetPosition(), false, Context.Player.SteamUserId, item.name))
                        {
                            if (AlliancePlugin.GridBackupInstalled)
                            {
                                AlliancePlugin.BackupGridMethod(GridManager.GetObjectBuilders(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\" + item.name + ".xml"), Context.Player.IdentityId);
                            }
                            PrintLog log = queue.GetLog(alliance);
                            PrintLogItem newLog = new PrintLogItem();
                            newLog.Claimed = true;
                            newLog.Grid = item.name;
                            newLog.SteamId = Context.Player.SteamUserId;
                            newLog.TimeClaimed = DateTime.Now;
                            log.log.Add(newLog);
                            queue.SaveLog(alliance, log);
                            AlliancePlugin.Log.Info("SHIPYARD SPAWNING " + item.name + " FOR " + Context.Player.DisplayName);
                            //  MoneyPlugin.DoOwnerShipTask((long)Context.Player.SteamUserId, item.name);
                            queue.claimedGrids.Add(item.name);


                                SendMessage("[Shipyard]", "Spawning the grid! It will be placed near you.", Color.Green, (long)Context.Player.SteamUserId);
                            
                            queue.removeFromQueue(slot);
                            alliance.SavePrintQueue(queue);
                        }

                        else
                        {

                            SendMessage("[Shipyard]", "Didnt load the grid, try a new location?", Color.Red, (long)Context.Player.SteamUserId);
                        }
                    }
                    else
                    {
                        var diff = end.Subtract(DateTime.Now);
                        SendMessage("[Shipyard]", "Print is not finished, wait " + String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds), Color.Red, (long)Context.Player.SteamUserId);
                        return;
                    }
                }
                else
                {
                    SendMessage("[Shipyard]", "That isnt yours to claim and you do not have the officer override.", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                // 
                //   

            }

        }


        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {

                for (int i = 0; i < block.InventoryCount; i++)
                {
                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }


        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>> toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    SendMessage("[Shipyard]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long) steamid);

                    return false;
                }
            }

            foreach (MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint> item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }


        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (VRage.Game.ModAPI.IMyInventory inv in inventories)
            {
                VRage.Game.ModAPI.IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    if (invItem.Amount >= targetAmount)
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                        targetAmount = 0;
                        break;
                    }
                    else
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                        targetAmount -= invItem.Amount;
                    }
                }
            }
            return targetAmount;
        }


        public static void GetComponents(MyCubeBlockDefinition def, IDictionary<MyDefinitionId, int> components)
        {
            if (def?.Components != null)
            {
                foreach (MyCubeBlockDefinition.Component c in def.Components)
                {
                    MyDefinitionId id = c.Definition.Id;
                    int num;
                    if (components.TryGetValue(id, out num))
                        components[id] = num + c.Count;
                    else
                        components.Add(id, c.Count);
                }
            }
        }


        public static Dictionary<MyDefinitionId, int> GetComponents(VRage.Game.ModAPI.IMyCubeGrid projection)
        {
            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();
            List<VRage.Game.ModAPI.IMySlimBlock> temp = new List<VRage.Game.ModAPI.IMySlimBlock>(0);
            projection.GetBlocks(temp, (slim) =>
            {
                GetComponents((MyCubeBlockDefinition)slim.BlockDefinition, comps);
                return false;
            });
            return comps;
        }

        private static Dictionary<long, long> confirmations = new Dictionary<long, long>();
        [Command("start", "start to print a projection")]
        [Permission(MyPromoteLevel.None)]
        public void start(string name)
        {
            if (!AlliancePlugin.config.ShipyardEnabled)
            {
                Context.Respond("Shipyard not enabled.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(name, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);

            if (!match.Success || string.IsNullOrEmpty(name))
            {
                Context.Respond("Name does not validate, try again.");
                return;
            }
            name = name.Replace("/", "");
            name = name.Replace("-", "");
            name = name.Replace("\\", "");
            IMyPlayer player = Context.Player;
            long playerId;
            double cost = 0;

            //DO COST AND TIME CALCS

            if (player == null)
            {
                Context.Respond("Console cant do this");
                return;
            }
            else
            {
                playerId = player.IdentityId;
            }
            IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Faction is required.");
                return;
            }
            IMyCharacter character = player.Character;
            if (character == null)
            {
                Context.Respond("Leave cockpit or spawn a body");
                return;
            }
            GridCosts gridCosts = new GridCosts();
            Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
            if (alliance == null)
            {
                Context.Respond("Target faction not member of alliance.");
                return;
            }
            if (AlliancePlugin.HasFailedUpkeep(alliance))
            {
                Context.Respond("Alliance failed to pay upkeep. Upgrades disabled.");
                return;
            }
            if (!alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ShipyardStart))
            {
                Context.Respond("Current rank does not have access to start shipyard builds.");
                return;
            }
            PrintQueue queue = alliance.LoadPrintQueue();
            if (queue == null)
            {
                SendMessage("[Shipyard]", "Alliance has no queue, to unlock use !shipyard upgrade", Color.Red, (long)Context.Player.SteamUserId);
                return;
            }
            bool owned = false;
            List<MyCubeGrid> gridsToSave = new List<MyCubeGrid>();
            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids;

            // SendMessage("[Shipyard]", "Saving from projection is currently disabled.", Color.Red, (long)Context.Player.SteamUserId);


            gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);

            MyCubeGrid store = null;
            MyCubeSize size = MyCubeSize.Large;
            bool confirmed = false;
            List<MyCubeGrid> grids = new List<MyCubeGrid>();
            ShipyardBlockConfig printerConfig = null;
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;


                    owned = true;
                    bool found = false;
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
                    foreach (MyCubeBlock proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
                    {

                        if (proj != null && !found && store == null && proj is Sandbox.ModAPI.IMyProjector projector)
                        {
                            if (AlliancePlugin.shipyardConfig.GetPrinterConfig(projector.BlockDefinition.SubtypeId) != null)
                            {
                                printerConfig = AlliancePlugin.shipyardConfig.GetPrinterConfig(projector.BlockDefinition.SubtypeId);
                                if (!printerConfig.enabled)
                                {
                                    continue;
                                }
                                if (projector.BlockDefinition.SubtypeName.ToLower().Contains("console"))
                                {
                                    Context.Respond("Cannot use a console block as the printer");
                                    return;

                                }

                                store = grid;
                                VRage.Game.ModAPI.IMyCubeGrid projectedGrid = projector.ProjectedGrid;
                                if (projectedGrid == null || !projector.IsProjecting)
                                {
                                    continue;
                                }
                                found = true;

                                List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
                                projectedGrid.GetBlocks(blocks);


                                gridCosts.setGridName(name + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now));
                                GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                                gridCosts.setComponents(localGridCosts.getComponents());
                                gridCosts.setCredits(localGridCosts.getCredits());
                                int blockCount = blocks.Count;
                                if (blockCount > AlliancePlugin.shipyardConfig.MaximumBlockSize)
                                {
                                    Context.Respond("Projected grid exceeds maximum limit of " + AlliancePlugin.shipyardConfig);
                                    return;
                                }
                                gridCosts.setPCU(blockCount);
                                gridCosts.BlockCount += blockCount;
                                if (gridsToSave.Count == 0)
                                {
                                    gridsToSave.Add(projectedGrid as MyCubeGrid);
                                }
                                foreach (MyCubeGrid checkGrid in gridsToSave)
                                {
                                    if (checkGrid.EntityId.Equals(projectedGrid.EntityId))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        gridsToSave.Add(projectedGrid as MyCubeGrid);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!owned)
            {
                SendMessage("[Shipyard]", "Cant find that grid, do you own it? ", Color.Red, (long)Context.Player.SteamUserId);
                return;
            }


            if (store == null)
            {
                Context.Respond("No projector named Projector Printer.");
                return;
            }
            if (gridsToSave.Count == 0)
            {
                Context.Respond("No projected grids found.");
                return;
            }
            Int64 price = Convert.ToInt64(gridCosts.BlockCount * printerConfig.SCPerBlock);
            if (confirmations.ContainsKey(playerId))
            {


                confirmations.TryGetValue(playerId, out long time);

                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= time)
                {
                    Context.Respond("Time ran out, use !shipyard start");
                    confirmations.Remove(Context.Player.IdentityId);
                    return;
                }
                confirmations.Remove(Context.Player.IdentityId);
                bool NeedsMoney = false;
                if (printerConfig.SCPerBlock > 0)
                {
                    NeedsMoney = true;
                }
                bool NeedsFuel = false;
                if (printerConfig.FuelPerInterval > 0)
                {
                    NeedsFuel = true;
                }
                Int64 balance = EconUtils.getBalance(Context.Player.IdentityId);
           

                if (price < 0)
                {
                    SendMessage("[Shipyard]", "Someone broke the cost multiplier, report to an admin", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                if (NeedsMoney)
                {
                    long tempPrice = price;
                    if (alliance.ShipyardFee > 0 && DatabaseForBank.ReadyToSave)
                    {
                        tempPrice += alliance.ShipyardFee;
                    }
                    if (balance < tempPrice)
                    {
                        SendMessage("[Shipyard]", "You cant afford the start price of " + String.Format("{0:n0}", price) + " SC", Color.Red, (long)Context.Player.SteamUserId);
                        return;

                    }
                }

                if (!queue.canAddToQueue())
                {
                    SendMessage("[Shipyard]", "Cannot add to queue, is it full?", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }

                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                foreach (MyCubeGrid grid in grids)
                {
                    inventories.AddRange(GetInventories(grid));

                }
                if (inventories == null)
                {
                    Context.Respond("Null inventories?");
                    return;
                }

                int upgradeLevel = queue.SpeedUpgrade;

                double seconds;
                if (speedUpgrades.ContainsKey(upgradeLevel))
                {
                    seconds = gridCosts.BlockCount * printerConfig.SecondsPerBlock * speedUpgrades[upgradeLevel].NewLevel;
                } 
                else
                {
                    seconds = gridCosts.BlockCount * printerConfig.SecondsPerBlock * queue.upgradeSpeed;
                }
              
                DateTime end = DateTime.Now.AddSeconds(seconds);
                int fuel = 0;
                if (printerConfig.FuelPerInterval > 0)
                {
                    MyDefinitionId.TryParse(printerConfig.FuelTypeId, printerConfig.FuelSubTypeId, out MyDefinitionId id);
                    if (printerConfig.SecondsPerInterval > 0)
                    {
                        fuel = Convert.ToInt32((seconds / printerConfig.SecondsPerInterval) * printerConfig.FuelPerInterval);
                        gridCosts.addToComp(id, fuel);
                    }
                }
                var diff = end.Subtract(DateTime.Now);
                if (ConsumeComponents(inventories, gridCosts.getComponents(), Context.Player.SteamUserId))
                {
                    PrintLog log = queue.GetLog(alliance);
                    PrintLogItem newLog = new PrintLogItem();
                    newLog.Claimed = false;
                    newLog.Grid = gridCosts.getGridName();
                    newLog.SteamId = Context.Player.SteamUserId;
                    newLog.TimeClaimed = DateTime.Now;
                    log.log.Add(newLog);
                    queue.SaveLog(alliance, log);
                    if (NeedsMoney)
                    {
                    
                        if (alliance.ShipyardFee > 0 && DatabaseForBank.ReadyToSave)
                        {
                       
                             if (DatabaseForBank.PayShipyardFee(alliance, alliance.ShipyardFee, Context.Player.SteamUserId))
                            {
                                price += alliance.ShipyardFee;
                            }
                        }
                        EconUtils.takeMoney(Context.Player.IdentityId, price);
                        if (AlliancePlugin.shipyardConfig.ShowMoneyTakenOnStart)
                        {
                            SendMessage("[Shipyard]", "Taking the cost to print : " + String.Format("{0:n0}", price) + " SC", Color.Green, (long)Context.Player.SteamUserId);
                        }
                    }
                    SendMessage("[Shipyard]", "The printer used " + fuel + " " + printerConfig.FuelSubTypeId.ToString().Replace("MyObjectBuilder_", "") + " " + printerConfig.FuelTypeId.ToString().Replace("MyObjectBuilder_", "") + " to feed the machines.", Color.Green, (long)Context.Player.SteamUserId);
                    SendMessage("[Shipyard]", "It will be complete in: " + String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds), Color.Green, (long)Context.Player.SteamUserId);
                    queue.addToQueue(gridCosts.getGridName(), (long)Context.Player.SteamUserId, Context.Player.IdentityId, Context.Player.DisplayName, DateTime.Now, end, Context.Player.GetPosition().X, Context.Player.GetPosition().Y, Context.Player.GetPosition().Z);
                    //  Task<GameSaveResult> task =
                    if (AlliancePlugin.shipyardConfig.SaveGameOnPrintStart)
                    {
                        Context.Torch.CurrentSession.Torch.Save();
                    }
                    alliance.SavePrintQueue(queue);
                }
                else
                {
                    return;
                }

                confirmed = true;

                //  Shipyard.SaveGridCosts(gridCosts);




                GridManager.SaveGrid(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + alliance.AllianceId + "\\") + gridCosts.getGridName() + ".xml", gridCosts.getGridName(), false, true, gridsToSave);
            }

            else
            {

                long timeToAdd = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 20000);

                confirmations.Add(Context.Player.IdentityId, timeToAdd);
                Context.Respond("Run command again within 20 seconds to confirm, it will cost " + String.Format("{0:n0} ", price) + " SC");
                if (alliance.ShipyardFee > 0)
                {
                    Context.Respond("It also has a fee of " + String.Format("{0:n0} ", alliance.ShipyardFee) + " SC");
                }
                int upgradeLevel = queue.SpeedUpgrade;

                double seconds;
                if (speedUpgrades.ContainsKey(upgradeLevel))
                {
                    seconds = gridCosts.BlockCount * printerConfig.SecondsPerBlock * speedUpgrades[upgradeLevel].NewLevel;
                }
                else
                {
                    seconds = gridCosts.BlockCount * printerConfig.SecondsPerBlock * queue.upgradeSpeed;
                }
                DateTime end = DateTime.Now.AddSeconds(seconds);
                int fuel = 0;
                if (printerConfig.FuelPerInterval > 0)
                {
                    MyDefinitionId.TryParse(printerConfig.FuelTypeId, printerConfig.FuelSubTypeId, out MyDefinitionId id);
                    if (printerConfig.SecondsPerInterval > 0)
                    {
                        fuel = Convert.ToInt32((seconds / printerConfig.SecondsPerInterval) * printerConfig.FuelPerInterval);
                        gridCosts.addToComp(id, fuel);
                    }
                }
                Context.Respond("It will also cost " + fuel + " " + printerConfig.FuelSubTypeId.ToString().Replace("MyObjectBuilder_", "") + " " + printerConfig.FuelTypeId.ToString().Replace("MyObjectBuilder_", ""));
            }

        }
        public static long GetOwner(MyCubeGrid grid)
        {

            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                return gridOwnerList[0];
            else if (ownerCnt > 1)
                return gridOwnerList[1];

            return gridOwner;
        }

        public bool IsOwnerOrFactionOwned(MyCubeGrid grid, long playerId, bool doFactionCheck)
        {
            if (grid.BigOwners.Contains(playerId))
            {
                return true;
            }
            else
            {
                if (!doFactionCheck)
                {
                    return false;
                }
                //check if the owner is a faction member, i honestly dont know the difference between grid.BigOwners and grid.SmallOwners

                long ownerId = GetOwner(grid);
                //check if the owner is a faction member, i honestly dont know the difference between grid.BigOwners and grid.SmallOwners
                return FacUtils.InSameFaction(playerId, ownerId);
            }
        }
    }
}

