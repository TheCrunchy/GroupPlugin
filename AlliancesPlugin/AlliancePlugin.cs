using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using Torch.API.Managers;
using System.IO;
using VRage.Game.ModAPI;
using NLog;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Torch.Mod.Messages;
using Torch.Mod;
using Torch.Managers.ChatManager;
using Torch.Managers;
using Torch.API.Plugins;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.KOTH;
using AlliancesPlugin.Hangar;
using AlliancesPlugin.Shipyard;
using AlliancesPlugin.JumpGates;
using VRage.Utils;
using AlliancesPlugin.ShipMarket;

namespace AlliancesPlugin
{
    public class AlliancePlugin : TorchPluginBase
    {
        public static MethodInfo sendChange;
        TorchSessionState TorchState;
        private TorchSessionManager sessionManager;
        public static Config config;
        public static ShipyardConfig shipyardConfig;
        public static string path;
        public static string basePath;
        public static Logger Log = LogManager.GetLogger("Alliances");
        public DateTime NextUpdate = DateTime.Now;
        public static Dictionary<Guid, List<ulong>> playersInAlliances = new Dictionary<Guid, List<ulong>>();
        public static Dictionary<ulong, Guid> playersAllianceId = new Dictionary<ulong, Guid>();
        public static List<KothConfig> KOTHs = new List<KothConfig>();
        private static List<DateTime> captureIntervals = new List<DateTime>();
        private static Dictionary<String, int> amountCaptured = new Dictionary<String, int>();

        public static Dictionary<Guid, JumpGate> AllGates = new Dictionary<Guid, JumpGate>();

        public static FileUtils utils = new FileUtils();
        public static Dictionary<string, DenialPoint> denials = new Dictionary<string, DenialPoint>();
        public static ITorchPlugin GridBackup;
        public static MethodInfo BackupGrid;

        public static ITorchBase TorchBase;
        public static bool GridBackupInstalled = false;
        public static void InitPluginDependencies(PluginManager Plugins)
        {

            if (Plugins.Plugins.TryGetValue(Guid.Parse("75e99032-f0eb-4c0d-8710-999808ed970c"), out ITorchPlugin GridBackupPlugin))
            {

                BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                GridBackup = GridBackupPlugin;
                GridBackupInstalled = true;
            }

        }

        public static void BackupGridMethod(List<MyObjectBuilder_CubeGrid> Grids, long User)
        {
            try
            {
                BackupGrid?.Invoke(GridBackup, new object[] { Grids, User });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            if (!Directory.Exists(path + "//JumpGates//"))
            {
                Directory.CreateDirectory(path + "//JumpGates//");
            }
            if (!Directory.Exists(path + "//PlayerData//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//");
            }
            if (!Directory.Exists(path + "//ShipMarket//"))
            {
                Directory.CreateDirectory(path + "//ShipMarket//");
            }
            if (!Directory.Exists(path + "//ShipMarket//ForSale//"))
            {
                Directory.CreateDirectory(path + "//ShipMarket//ForSale//");
            }
            if (!Directory.Exists(path + "//ShipMarket//Sold//"))
            {
                Directory.CreateDirectory(path + "//ShipMarket//Sold//");
            }
            if (!Directory.Exists(path + "//ShipMarket//Grids//"))
            {
                Directory.CreateDirectory(path + "//ShipMarket//Grids//");
            }
            TorchBase = Torch;
            LoadAllAlliances();
            LoadAllGates();
            Log.Error(AllAlliances.Count());
        }

        public void SetupConfig()
        {
            FileUtils utils = new FileUtils();
            path = StoragePath;
            if (File.Exists(StoragePath + "\\Alliances.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\Alliances.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\Alliances.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\Alliances.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + "//Alliances");
            }
            else
            {
                folder = config.StoragePath;
            }
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = Path.Combine(StoragePath + "//Alliances//KOTH//");
            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//Alliances//AllianceData");
            }
            else
            {
                folder2 = config.StoragePath + "//AllianceData";
            }

            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//Alliances//ShipyardData");
            }
            else
            {
                folder2 = config.StoragePath + "//Alliance//ShipyardData";
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            FileUtils utils = new FileUtils();
            config = utils.ReadFromXmlFile<Config>(basePath + "\\Alliances.xml");
            KOTHs.Clear();
            foreach (String s in Directory.GetFiles(basePath + "//Alliances//KOTH//"))
            {


                KothConfig koth = utils.ReadFromXmlFile<KothConfig>(s);
                //  DateTime now = DateTime.Now;
                //if (now.Minute == 59 || now.Minute == 60)
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                //}
                //else
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                //}

                KOTHs.Add(koth);
            }

            return config;
        }
        public static void saveConfig()
        {
            FileUtils utils = new FileUtils();

            utils.WriteToXmlFile<Config>(path + "\\Alliances.xml", config);

            return;
        }
        public static void SaveAllianceData(Alliance alliance)
        {
            FileUtils jsonStuff = new FileUtils();

            jsonStuff.WriteToJsonFile<Alliance>(path + "//AllianceData//" + alliance.AllianceId + ".json", alliance);
            AlliancePlugin.AllAlliances[alliance.name] = alliance;
        }
        public static Alliance LoadAllianceData(Guid id)
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                Alliance alliance2 = jsonStuff.ReadFromJsonFile<Alliance>(path + "//AllianceData//" + id + ".json");
                return alliance2;
            }
            catch
            {
                return null;
            }
        }
        public static Alliance GetAlliance(string name)
        {
            //fuck it lets just return something that might be null
            Alliance temp = null;
            if (AllAlliances.ContainsKey(name))
            {
                temp = LoadAllianceData(AllAlliances[name].AllianceId);
            }

            //i null check in the command anyway

            return temp;
        }
        public static Alliance GetAllianceNoLoading(string name)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.name.Equals(name))
                {

                    return alliance.Value;
                }
            }
            return null;
        }
        public static Alliance GetAlliance(Guid guid)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceId == guid)
                {

                    return GetAlliance(alliance.Value.name);
                }
            }
            return null;
        }
        public static Alliance GetAllianceNoLoading(Guid guid)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceId == guid)
                {

                    return alliance.Value;
                }
            }
            return null;
        }
        public static Dictionary<long, String> FactionsInAlliances = new Dictionary<long, string>();
        public static Alliance GetAllianceNoLoading(MyFaction fac)
        {
            //fuck it lets just return something that might be null
            if (FactionsInAlliances.ContainsKey(fac.FactionId))
            {
                return AllAlliances[FactionsInAlliances[fac.FactionId]];
            }
            return null;
        }
        public static Alliance GetAlliance(MyFaction fac)
        {
            //fuck it lets just return something that might be null
            if (FactionsInAlliances.ContainsKey(fac.FactionId))
            {
                return GetAlliance(FactionsInAlliances[fac.FactionId]);
            }

            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceMembers.Contains(fac.FactionId))
                {

                    return GetAlliance(alliance.Value.name);
                }
            }
            return null;
        }
        public void SetupFriendMethod()
        {
            Type FactionCollection = MySession.Static.Factions.GetType().Assembly.GetType("Sandbox.Game.Multiplayer.MyFactionCollection");
            sendChange = FactionCollection?.GetMethod("SendFactionChange", BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static ChatManagerServer _chatmanager;
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (state == TorchSessionState.Unloading)
            {
                DiscordStuff.Stopdiscord();
            }
            if (state == TorchSessionState.Loaded)
            {
                AllianceChat.ApplyLogging();
                InitPluginDependencies(Torch.Managers.GetManager<PluginManager>());
                TorchState = TorchSessionState.Loaded;
                _chatmanager = Torch.CurrentSession.Managers.GetManager<ChatManagerServer>();

                if (_chatmanager == null)
                {
                    Log.Warn("No chat manager loaded!");
                }
                else
                {
                    _chatmanager.MessageProcessing += AllianceChat.DoChatMessage;
                    session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += AllianceChat.Login;
                    session.Managers.GetManager<IMultiplayerManagerBase>().PlayerLeft += AllianceChat.Logout;
                }

                // MySession.Static.Config.
                // MyMultiplayer.Static.SyncDistance
                if (!Directory.Exists(path + "//JumpZones//"))
                {
                    Directory.CreateDirectory(path + "//JumpZones//");
                }
                if (!File.Exists(path + "//JumpZones//example.xml"))
                {
                    utils.WriteToXmlFile<JumpZone>(path + "//JumpZones//example.xml", new JumpZone(), false);
                }
                //      if (!File.Exists(path + "//bank.db"))
                //    {
                //     File.Create(path + "//bank.db");
                // }
                if (!Directory.Exists(basePath + "//Alliances"))
                {
                    Directory.CreateDirectory(basePath + "//Alliances//");
                }
                if (!File.Exists(basePath + "//Alliances//KOTH//example.xml"))
                {
                    utils.WriteToXmlFile<KothConfig>(basePath + "//Alliances//KOTH//example.xml", new KothConfig(), false);
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//");
                }
                if (!Directory.Exists(path + "//HangarUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//HangarUpgrades//");
                }

                if (!File.Exists(path + "//ShipyardUpgrades//SpeedUpgrade1.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("1,Speed,7");
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    output.AppendLine("MetaPoints,50");
                    File.WriteAllText(path + "//ShipyardUpgrades//SpeedUpgrade1.txt", output.ToString());

                }
                if (!File.Exists(path + "//HangarUnlockCost.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    output.AppendLine("MetaPoints,50");
                    File.WriteAllText(path + "//HangarUnlockCost.txt", output.ToString());

                }
                if (!File.Exists(path + "//HangarDeniedLocations.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("Name,X,Y,Z,Radius");
                    output.AppendLine("Fred,0,0,0,50000");
                    File.WriteAllText(path + "//HangarDeniedLocations.txt", output.ToString());

                }
                else
                {
                    String[] line;
                    line = File.ReadAllLines(path + "//HangarDeniedLocations.txt");
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
                        HangarDeniedLocations.Add(loc);
                    }
                }
                if (!File.Exists(path + "//HangarUpgrades//SlotUpgrade1.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("1,Slots,2");
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    output.AppendLine("MetaPoints,50");
                    File.WriteAllText(path + "//HangarUpgrades//SlotUpgrade1.txt", output.ToString());

                }
                if (!File.Exists(path + "//ShipyardUnlockCost.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    output.AppendLine("MetaPoints,50");
                    File.WriteAllText(path + "//ShipyardUnlockCost.txt", output.ToString());

                }
                if (!File.Exists(path + "//ShipyardUpgrades//SlotUpgrade1.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("1,Slots,2");
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    output.AppendLine("MetaPoints,50");
                    File.WriteAllText(path + "//ShipyardUpgrades//SlotUpgrade1.txt", output.ToString());

                }
                if (!Directory.Exists(path + "//ShipyardBlocks//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardBlocks//");
                }

                if (!File.Exists(path + "//ShipyardBlocks//LargeProjector.xml"))
                {
                    ShipyardBlockConfig config33 = new ShipyardBlockConfig();
                    config33.SetShipyardBlockConfig("LargeProjector");
                    utils.WriteToXmlFile<ShipyardBlockConfig>(path + "//ShipyardBlocks//LargeProjector.xml", config33, false);

                }
                if (!File.Exists(path + "//ShipyardBlocks//SmallProjector.xml"))
                {
                    ShipyardBlockConfig config33 = new ShipyardBlockConfig();
                    config33.SetShipyardBlockConfig("SmallProjector");
                    utils.WriteToXmlFile<ShipyardBlockConfig>(path + "//ShipyardBlocks//SmallProjector.xml", config33, false);

                }
                if (!File.Exists(path + "//ShipyardConfig.xml"))
                {
                    utils.WriteToXmlFile<ShipyardConfig>(path + "//ShipyardConfig.xml", new ShipyardConfig(), false);

                }
                else
                {
                    ReloadShipyard();
                }

                foreach (String s in Directory.GetFiles(basePath + "//Alliances//KOTH//"))
                {


                    KothConfig koth = utils.ReadFromXmlFile<KothConfig>(s);

                    KOTHs.Add(koth);
                }
                SetupFriendMethod();

                LoadAllAlliances();
                LoadAllGates();

                DiscordStuff.RegisterDiscord();
                //        DatabaseForBank bank = new DatabaseForBank();
                //    bank.CreateTable(bank.CreateConnection());
            }
        }
        public static List<DeniedLocation> HangarDeniedLocations = new List<DeniedLocation>();
        public static void ReloadShipyard()
        {
            if (!File.Exists(path + "//ShipyardConfig.xml"))
            {
                Log.Info("File doesnt exist");

            }
            ShipyardCommands.speedUpgrades.Clear();
            ShipyardCommands.slotUpgrades.Clear();
            shipyardConfig = utils.ReadFromXmlFile<ShipyardConfig>(path + "//ShipyardConfig.xml");
            foreach (String s in Directory.GetFiles(path + "//ShipyardBlocks//"))
            {


                ShipyardBlockConfig conf = utils.ReadFromXmlFile<ShipyardBlockConfig>(s);

                shipyardConfig.AddToBlockConfig(conf);
            }
            foreach (String s in Directory.GetFiles(path + "//ShipyardUpgrades//"))
            {
                ShipyardCommands.LoadUpgradeCost(s);
            }
            foreach (String s in Directory.GetFiles(path + "//HangarUpgrades//"))
            {
                HangarCommands.LoadUpgradeCost(s);
            }
        }
        public int ticks;
        public static MyIdentity TryGetIdentity(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
        public static Dictionary<String, Alliance> AllAlliances = new Dictionary<string, Alliance>();
        public static void LoadAllJumpZones()
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                JumpPatch.Zones.Clear();
                foreach (String s in Directory.GetFiles(path + "//JumpZones//"))
                {

                    JumpPatch.Zones.Add(jsonStuff.ReadFromXmlFile<JumpZone>(s));


                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

        }
        public void LoadAllAlliances()
        {
            if (TorchState == TorchSessionState.Loaded)
            {
                FileUtils jsonStuff = new FileUtils();
                try
                {
                    AllAlliances.Clear();
                    FactionsInAlliances.Clear();
                    foreach (String s in Directory.GetFiles(path + "//AllianceData//"))
                    {

                        Alliance alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                        if (AllAlliances.ContainsKey(alliance.name))
                        {
                            alliance.name += " DUPLICATE";
                            AllAlliances.Add(alliance.name, alliance);

                            foreach (long id in alliance.AllianceMembers)
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                            SaveAllianceData(alliance);
                        }
                        else
                        {
                            AllAlliances.Add(alliance.name, alliance);
                            foreach (long id in alliance.AllianceMembers)
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
              //  Log.Info("Registering bots");
                foreach (Alliance alliance in AllAlliances.Values)
                {
                    alliance.ForceFriendlies();
                    alliance.ForceEnemies();
                    if (alliance.DiscordChannelId > 0 && !String.IsNullOrEmpty(alliance.DiscordToken) && TorchState == TorchSessionState.Loaded)
                    {
                        //  Log.Info(Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length);

                        try
                        {
                            if (Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length != 59)
                            {
                                Log.Error("Invalid bot token for " + alliance.AllianceId);
                                continue;
                            }

                        }
                        catch (Exception ex)
                        {
                            //  Log.Error(ex);
                            Log.Error("Invalid bot token for " + alliance.AllianceId);
                            continue;
                        }
                        
                        DiscordStuff.RegisterAllianceBot(alliance, alliance.DiscordChannelId);
                    }
                }

            }
        }
   
        public static void LoadAllGates()
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                AllGates.Clear();
                foreach (String s in Directory.GetFiles(path + "//JumpGates//"))
                {

                    JumpGate gate = jsonStuff.ReadFromJsonFile<JumpGate>(s);
                    AllGates.Add(gate.GateId, gate);

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }

        public static Boolean DoFeeStuff(MyPlayer player, JumpGate gate)
        {
            if (gate.fee > 0)
            {
                if (EconUtils.getBalance(player.Identity.IdentityId) >= gate.fee)
                {
                    Alliance temp = null;
                    foreach (Alliance alliance in AllAlliances.Values)
                    {
                        if (alliance.AllianceId == gate.OwnerAlliance)
                        {
                            if (DatabaseForBank.AddToBalance(alliance, gate.fee))
                            {
                                temp = LoadAllianceData(alliance.AllianceId);
                                if (temp != null)
                                {
                                    temp.GateFee(gate.fee, player.Id.SteamId, gate.GateName);
                                    EconUtils.takeMoney(player.Identity.IdentityId, gate.fee);
                                    SaveAllianceData(temp);
                                }

                            }
                        }
                    }
                    return true;
                }
                else
                {
                    NotificationMessage message2;
                    if (messageCooldowns.ContainsKey(player.Identity.IdentityId))
                    {
                        if (DateTime.Now < messageCooldowns[player.Identity.IdentityId])
                            return false;

                        message2 = new NotificationMessage("It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", 1000, "Red");
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                        messageCooldowns[player.Identity.IdentityId] = DateTime.Now.AddMilliseconds(500);
                        return false;
                    }
                    else
                    {


                        message2 = new NotificationMessage("It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", 1000, "Red");
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                        messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(500));
                        return false;
                    }
                    return false;
                }
            }

            return true;
        }

        public static Boolean DoFeeMessage(MyPlayer player, JumpGate gate, float Distance)
        {
            if (gate.fee > 0)

            {
                NotificationMessage message;
                NotificationMessage message2;
                if (EconUtils.getBalance(player.Identity.IdentityId) >= gate.fee)
                {
                    if (messageCooldowns.ContainsKey(player.Identity.IdentityId))
                    {
                        if (DateTime.Now < messageCooldowns[player.Identity.IdentityId])
                            return false;
                        message = new NotificationMessage("You will jump in " + Distance + " meters", 1000, "Green");
                        message2 = new NotificationMessage("It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", 1000, "Green");
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                        ModCommunication.SendMessageTo(message, player.Id.SteamId);
                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                        messageCooldowns[player.Identity.IdentityId] = DateTime.Now.AddMilliseconds(500);
                        return true;
                    }
                    else
                    {


                        message = new NotificationMessage("You will jump in " + Distance + " meters", 1000, "Green");
                        message2 = new NotificationMessage("It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", 1000, "Green");
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                        ModCommunication.SendMessageTo(message, player.Id.SteamId);
                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                        ModCommunication.SendMessageTo(message, player.Id.SteamId);
                        // MyAPIGateway.Utilities.ShowNotification
                        messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(500));
                        return true;
                    }
                }
            }
            return false;
        }
        public static Dictionary<long, long> TaxesToBeProcessed = new Dictionary<long, long>();
        public static Dictionary<long, DateTime> messageCooldowns = new Dictionary<long, DateTime>();

        public void DoJumpGateStuff()
        {
            List<MyPlayer> players = new List<MyPlayer>();
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (player?.Controller?.ControlledEntity is MyCockpit controller)
                {
                    if (controller.CubeGrid.IsStatic)
                        continue;

                    if (!controller.CubeGrid.Editable)
                        continue;

                    if (!controller.CubeGrid.DestructibleBlocks)
                        continue;
                    players.Add(player);
                }
            }

            foreach (KeyValuePair<Guid, JumpGate> key in AllGates)
            {
                JumpGate gate = key.Value;
                if (!gate.Enabled)
                    continue;
                if (gate.TargetGateId == gate.GateId)
                    continue;
                if (!AllGates.ContainsKey(gate.TargetGateId))
                    continue;

                JumpGate target = AllGates[gate.TargetGateId];
                if (!target.Enabled)
                    continue;
                if (target.TargetGateId == target.GateId)
                    continue;


                foreach (MyPlayer player in players)
                {
                    if (player?.Controller?.ControlledEntity is MyCockpit controller)
                    {

                        float Distance = Vector3.Distance(gate.Position, controller.CubeGrid.PositionComp.GetPosition());
                        if (Distance <= gate.RadiusToJump)
                        {
                            if (!DoFeeStuff(player, gate))
                                continue;
                            Random rand = new Random();
                            Vector3 offset = new Vector3(rand.Next(config.JumpGateMinimumOffset, config.JumPGateMaximumOffset), rand.Next(config.JumpGateMinimumOffset, config.JumPGateMaximumOffset), rand.Next(config.JumpGateMinimumOffset, config.JumPGateMaximumOffset));
                            Vector3D newPos = new Vector3D(target.Position + offset);
                            Vector3D? newPosition = MyEntities.FindFreePlace(newPos, (float)GridManager.FindBoundingSphere(controller.CubeGrid).Radius);
                            if (newPosition.Value == null)
                            {
                                break;
                            }
                            MatrixD worldMatrix = MatrixD.CreateWorld(newPosition.Value, controller.CubeGrid.WorldMatrix.Forward, controller.CubeGrid.WorldMatrix.Up);

                            controller.CubeGrid.Teleport(worldMatrix);
                        }
                        else
                        {
                            if (Distance <= 500)
                            {
                                NotificationMessage message;
                                if (messageCooldowns.ContainsKey(player.Identity.IdentityId))
                                {

                                    if (DateTime.Now < messageCooldowns[player.Identity.IdentityId])
                                        continue;

                                    if (DoFeeMessage(player, gate, Distance))
                                        continue;

                                    message = new NotificationMessage("You will jump in " + Distance + " meters", 1000, "Green");
                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                    ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                    messageCooldowns[player.Identity.IdentityId] = DateTime.Now.AddMilliseconds(500);
                                }

                                else
                                {
                                    if (DoFeeMessage(player, gate, Distance))
                                        continue;


                                    message = new NotificationMessage("You will jump in " + Distance + " meters", 1000, "Green");
                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                    ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                    messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(500));
                                }
                            }
                        }
                    }


                }
            }
        }
        public void OrganisePlayers()
        {
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                {
                    Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                    if (temp != null)
                    {
                        if (playersInAlliances.ContainsKey(temp.AllianceId))
                        {
                            if (!playersInAlliances[temp.AllianceId].Contains(player.Id.SteamId))
                            {
                                playersInAlliances[temp.AllianceId].Add(player.Id.SteamId);
                            }
                            if (!playersAllianceId.ContainsKey(player.Id.SteamId))
                            {
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                        }
                        else
                        {
                            List<ulong> bob = new List<ulong>();
                            bob.Add(player.Id.SteamId);
                            playersInAlliances.Add(temp.AllianceId, bob);

                            if (!playersAllianceId.ContainsKey(player.Id.SteamId))
                            {
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                        }
                    }
                }
            }
        }

        public void DoTaxStuff()
        {
            List<long> Processed = new List<long>();
            Dictionary<Guid, Dictionary<long, float>> taxes = new Dictionary<Guid, Dictionary<long, float>>();
            foreach (long id in TaxesToBeProcessed.Keys)
            {

                if (MySession.Static.Factions.TryGetPlayerFaction(id) != null)
                {

                    Alliance alliance = GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(id) as MyFaction);
                    if (alliance != null)
                    {

                        alliance = GetAlliance(alliance.name);

                        if (alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id)) > 0)
                        {

                            float tax = TaxesToBeProcessed[id] * alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id));
                      //      Log.Info(TaxesToBeProcessed[id] + " " + tax + " " + alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id)));
                            if (EconUtils.getBalance(id) >= tax)
                            {
                                if (taxes.ContainsKey(alliance.AllianceId))
                                {
                                    taxes[alliance.AllianceId].Remove(id);
                                    taxes[alliance.AllianceId].Add(id, tax);
                                }
                                else
                                {
                                    Dictionary<long, float> temp = new Dictionary<long, float>();
                                    temp.Add(id, tax);
                                    taxes.Add(alliance.AllianceId, temp);
                                }
                            }
                            Processed.Add(id);
                        }
                    }
                }
            }

            DatabaseForBank.Taxes(taxes);

            foreach (long id in Processed)
            {
                TaxesToBeProcessed.Remove(id);
            }
        }
        //public void DoTaxStuff()
        //{

        //    List<long> Processed = new List<long>();
        //    foreach (long id in TaxesToBeProcessed.Keys)
        //    {


        //        if (MySession.Static.Factions.TryGetPlayerFaction(id) != null)
        //        {

        //            Alliance alliance = GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(id) as MyFaction);
        //            if (alliance != null)
        //            {

        //                alliance = GetAlliance(alliance.name);

        //                if (alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id)) > 0)
        //                {

        //                    float tax = TaxesToBeProcessed[id] * alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id));
        //                    if (EconUtils.getBalance(id) >= tax)
        //                    {
        //                        if (DatabaseForBank.AddToBalance(alliance, (long)tax))
        //                        {

        //                            EconUtils.takeMoney(id, (long)tax);

        //                            alliance.DepositTax((long)tax, MySession.Static.Players.TryGetSteamId(id));
        //                            SaveAllianceData(alliance);
        //                            Processed.Add(id);
        //                        }
        //                        else
        //                        {

        //                            Processed.Add(id);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Processed.Add(id);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Processed.Add(id);
        //            }
        //        }
        //        else
        //        {
        //            Processed.Add(id);
        //        }
        //    }
        //    foreach (long id in Processed)
        //    {
        //        TaxesToBeProcessed.Remove(id);
        //    }
        //}
        public void DoKothStuff()
        {

            if (TorchState != TorchSessionState.Loaded)
            {
                return;
            }
            if (!config.KothEnabled)
            {
                return;
            }

            foreach (KothConfig config in KOTHs)
            {
                try
                {
                    if (!config.enabled)
                        continue;

                    bool contested = false;
                    Boolean hasActiveCaptureBlock = false;
                    // Log.Info("We capping?");
                    Vector3 position = new Vector3(config.x, config.y, config.z);
                    BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre);
                    if (DateTime.Now >= config.unlockTime)
                    {
                        config.unlockTime = DateTime.Now.AddYears(1);

                        try
                        {
                            DiscordStuff.SendMessageToDiscord(config.KothName + " Is now unlocked! ", config);
                        }
                        catch (Exception)
                        {
                            Log.Error("Cant do discord message for koth.");
                            SendChatMessage(config.KothName + " Is now unlocked! ");
                        }

                    }
                    if (DateTime.Now >= config.nextCaptureInterval)
                    {
                        config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                        //Log.Info(config.owner + " " + config.capturingNation);
                        //setup a time check for capture time
                        Guid capturingNation = Guid.Empty;
                        if (config.capturingNation != Guid.Empty)
                        {
                            capturingNation = config.capturingNation;
                        }
                        Boolean locked = false;

                        Log.Info("Yeah we capping");


                        int entitiesInCapPoint = 0;
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {

                            if (grid.Projector != null)
                                continue;

                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null && !fac.Tag.Equals(config.KothBuildingOwner) && fac.Tag.Length == 3)
                            {
                                entitiesInCapPoint++;
                                if (IsContested(fac, config, capturingNation) && capturingNation != Guid.Empty)
                                {

                                  //  Log.Info("Contested faction " + fac.Tag + " " + capturingNation);
                                    contested = true;
                                }
                                else
                                {
                                    Alliance alliance = GetNationTag(fac);
                                    if (alliance != null)
                                    {
                                        capturingNation = alliance.AllianceId;
                                        if (!hasActiveCaptureBlock)
                                        {
                                          //  Log.Info("Checking for a capture block");
                                            hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, config);
                                        }
                                    }
                                }

                            }
                            if (fac == null)
                            {

                              //  Log.Info("Contested no faction");
                                contested = true;
                            }
                        }

                        //if (!contested)
                        //{
                        //    //now check characters
                        //    foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                        //    {
                        //        if (character.MarkedForClose)
                        //            continue;

                        //        entitiesInCapPoint++;
                        //        IMyFaction fac = MySession.Static.Factions.GetPlayerFaction(character.GetPlayerIdentityId());
                        //        if (fac != null)
                        //        {
                        //            float distance = Vector3.Distance(position, character.PositionComp.GetPosition());
                        //            if (IsContested(fac, config, capturingNation) && capturingNation != Guid.Empty)
                        //            {
                        //                Log.Info("Contested character");
                        //                contested = true;
                        //                break;
                        //            }
                        //            else
                        //            {
                        //                Alliance alliance = GetNationTag(fac);
                        //                if (alliance != null)
                        //                {
                        //                    capturingNation = GetNationTag(fac).AllianceId;
                        //                }
                        //            }
                        //        }
                        //        else
                        //        {
                        //            Log.Info("Contested character");
                        //            contested = true;
                        //        }
                        //    }
                        //}


                        if (DateTime.Now >= config.nextCaptureAvailable)
                        {
                            //this errors, i think this is only for client side mods? 
                            // MatrixD matrix = MatrixD.CreateWorld(position);

                            // Color color = Color.Gray;
                            //if (matrix != null)
                            // {
                            //     MySimpleObjectDraw.DrawTransparentSphere(ref matrix, (float) config.CaptureRadiusInMetre, ref color, MySimpleObjectRasterizer.Solid, 20);
                            // }

                            if (entitiesInCapPoint == 0 && config.IsDenialPoint)
                            {
                                if (denials.TryGetValue(config.DeniedKoth, out DenialPoint den))
                                {
                                    den.RemoveCap(config.KothName);
                                    SaveKothConfig(config.KothName, config);
                                }
                            }
                            if (!contested && !config.CaptureStarted && capturingNation != Guid.Empty)
                            {
                                if (!capturingNation.Equals(config.owner))
                                {
                                    if (hasActiveCaptureBlock)
                                    {
                                        config.CaptureStarted = true;
                                        config.nextCaptureAvailable = DateTime.Now.AddMinutes(config.MinutesBeforeCaptureStarts);
                                      //  Log.Info("Can cap in 10 minutes");
                                        config.capturingNation = capturingNation;
                                        SendChatMessage("Can cap in however many minutes");

                                        try
                                        {
                                            DiscordStuff.SendMessageToDiscord(config.KothName + " Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes.", config);
                                        }
                                        catch (Exception)
                                        {
                                            Log.Error("Cant do discord message for koth.");
                                            SendChatMessage(config.KothName + " Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!contested && capturingNation != Guid.Empty)
                                {
                                  //  Log.Info("Got to the capping check and not contested");
                                    if (DateTime.Now >= config.nextCaptureAvailable && config.CaptureStarted)
                                    {

                                        if (config.capturingNation.Equals(capturingNation) && !config.capturingNation.Equals(""))
                                        {

                                          //  Log.Info("Is the same nation as whats capping");
                                            if (!hasActiveCaptureBlock)
                                            {
                                               // Log.Info("Locking because no active cap block");

                                                config.capturingNation = Guid.Empty;
                                                config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                //broadcast that its locked
                                                config.amountCaptured = 0;
                                                config.CaptureStarted = false;
                                                config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                try
                                                {
                                                    DiscordStuff.SendMessageToDiscord(config.KothName + " Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                }
                                                catch (Exception)
                                                {
                                                    Log.Error("Cant do discord message for koth.");
                                                    SendChatMessage(config.KothName + " Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                }
                                            }
                                            else
                                            {
                                                config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                                if (config.IsDenialPoint)
                                                {
                                                    if (denials.TryGetValue(config.DeniedKoth, out DenialPoint den))
                                                    {
                                                        den.AddCap(config.KothName);
                                                    }
                                                    else
                                                    {
                                                        DenialPoint denial = new DenialPoint();
                                                        denial.AddCap(config.KothName);
                                                        denials.Add(config.DeniedKoth, denial);
                                                    }
                                                    //exit this one because its a denial point and continue to the next config
                                                    continue;
                                                }
                                                config.amountCaptured += config.PointsPerCap;

                                                if (config.amountCaptured >= config.PointsToCap)
                                                {
                                                    //lock
                                            //        Log.Info("Locking because points went over the threshold");

                                                    locked = true;
                                                    config.nextCaptureAvailable = DateTime.Now.AddHours(config.hoursToLockAfterCap);
                                                    config.capturingNation = Guid.Empty;
                                                    config.owner = capturingNation;
                                                    config.amountCaptured = 0;
                                                    config.CaptureStarted = false;
                                                    config.unlockTime = DateTime.Now.AddHours(config.hoursToLockAfterCap);
                                                    Alliance alliance = GetAllianceNoLoading(config.owner);
                                                    if (config.EditTerritoryFile)
                                                    {
                                                        if (File.Exists(config.TerritoryFilePath))
                                                        {
                                                            String[] line;
                                                            line = File.ReadAllLines(config.TerritoryFilePath);

                                                            for (int i = 0; i < line.Length; i++)
                                                            {
                                                                if (line[i].Contains("[Name"))
                                                                {
                                                                    String[] split = line[i].Split(':');
                                                                    line[i] = split[0] + ":" + alliance.name + "]";

                                                                }
                                                            }
                                                            StringBuilder sb = new StringBuilder();
                                                            foreach (string s in line)
                                                            {
                                                                sb.Append(s);
                                                            }
                                                            File.WriteAllText(config.TerritoryFilePath, sb.ToString());
                                                        }
                                                    }
                                                    foreach (JumpGate gate in AllGates.Values)
                                                    {
                                                        if (gate.LinkedKoth.Equals(config.KothName))
                                                        {
                                                            gate.OwnerAlliance = config.owner;
                                                            gate.Save();
                                                        }
                                                    }
                                                    try
                                                    {
                                                        DiscordStuff.SendMessageToDiscord(GetAllianceNoLoading(capturingNation).name + " has captured " + config.KothName + ". It is now locked for " + config.hoursToLockAfterCap + " hours.", config);
                                                    }
                                                    catch (Exception)
                                                    {

                                                        Log.Error("Cant do discord message for koth.");
                                                        SendChatMessage(GetAllianceNoLoading(capturingNation).name + " has captured " + config.KothName + ". It is now locked for " + config.hoursToLockAfterCap + " hours.");
                                                    }
                                                }
                                                else
                                                {
                                                    //       if (DateTime.Now >= config.nextBroadcast)
                                                    //        {


                                                    try
                                                    {
                                                        DiscordStuff.SendMessageToDiscord(config.KothName + " capture progress " + config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAllianceNoLoading(capturingNation).name, config);
                                                    }
                                                    catch (Exception)
                                                    {
                                                        Log.Error("Cant do discord message for koth.");
                                                        SendChatMessage(config.KothName + " capture progress " + config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAllianceNoLoading(capturingNation).name);
                                                    }
                                                    continue;
                                                    //         config.nextBroadcast = DateTime.Now.AddMinutes(config.MinsPerCaptureBroadcast);
                                                    //  }
                                                }
                                            }
                                        }
                                        else
                                        {
                                          //  Log.Info("Locking because the capturing nation changed");
                                            config.capturingNation = Guid.Empty;
                                            config.CaptureStarted = false;
                                            config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                            config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                            //broadcast that its locked
                                            SendChatMessage("Locked because capturing nation has changed.");
                                            config.amountCaptured = 0;
                                            try
                                            {

                                                DiscordStuff.SendMessageToDiscord(config.KothName + " Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                            }
                                            catch (Exception)
                                            {
                                                Log.Error("Cant do discord message for koth.");
                                                SendChatMessage(config.KothName + " Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours");
                                            }
                                        }
                                    }
                                    else
                                    {

                                        SendChatMessage("Waiting to cap");
                                    //    Log.Info("Waiting to cap");
                                    }
                                }
                                else
                                {
                                    if (!hasActiveCaptureBlock && config.CaptureStarted)
                                    {
                                       // Log.Info("Locking because no active cap block");
                                        config.capturingNation = Guid.Empty;
                                        config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                        config.CaptureStarted = false;
                                        //broadcast that its locked

                                        config.amountCaptured = 0;
                                        //   SendChatMessage("Locked because capture blocks are dead");
                                        config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                        try
                                        {
                                            DiscordStuff.SendMessageToDiscord(config.KothName + " Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                        }
                                        catch (Exception)
                                        {
                                            Log.Error("Cant do discord message for koth.");
                                            SendChatMessage(config.KothName + " Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                        }
                                    }
                                    if (contested && config.CaptureStarted)
                                    {
                                     //   Log.Info("Its contested or the fuckers trying to cap have no nation");
                                        //send contested message
                                        //  SendChatMessage("Contested");
                                        try
                                        {
                                            DiscordStuff.SendMessageToDiscord(config.KothName + " Capture point contested!", config);
                                        }
                                        catch (Exception)
                                        {
                                            SendChatMessage(config.KothName + " Capture point contested!");
                                            Log.Error("Cant do discord message for koth.");
                                        }
                                    }
                                }
                            }


                        }
                        else
                        {
                            if (config.CaptureStarted)
                            {
                                DateTime end = config.nextCaptureAvailable;
                                var diff = end.Subtract(DateTime.Now);
                                string time = String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds);
                                SendChatMessage("Capture can begin in " + time);
                            }
                        }
                        if (!locked)
                        {
                            config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                        }
                        SaveKothConfig(config.KothName, config);



                        //if its not locked, check again for capture in a minute

                    }

                    if (DateTime.Now > config.nextCoreSpawn && !config.IsDenialPoint && config.HasReward)
                    {
                        MyCubeGrid lootgrid = GetLootboxGrid(position, config);
                        //spawn the cores
                        Boolean hasCap = false;
                        Boolean hasCapNotOwner = false;
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null)
                            {
                                if (fac.Tag == config.KothBuildingOwner || fac.Tag.Length > 3)
                                {
                                    continue;
                                }
                            }
                            if (fac != null)
                            {
                                if (GetNationTag(fac) != null && GetNationTag(fac).AllianceId.Equals(config.owner))
                                {
                                    if (!hasCap)
                                    {
                                        hasCap = DoesGridHaveCaptureBlock(grid, config);
                                    }
                                }
                                else
                                {
                                    if (!hasCapNotOwner)
                                    {
                                        hasCapNotOwner = DoesGridHaveCaptureBlock(grid, config);
                                    }
                                }

                            }

                        }

                        if (denials.TryGetValue(config.KothName, out DenialPoint den))
                        {
                            if (den.IsDenied())
                                SendChatMessage("Denied point, no core spawn");
                            continue;
                        }
                        if (config.owner != Guid.Empty)
                        {
                            if (config.RequireCaptureBlockForLootGen)
                            {
                                if (!hasCap)
                                {
                                    config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                                    if (hasCapNotOwner)
                                    {
                                        if (lootgrid != null)
                                        {
                                            SpawnCores(lootgrid, config);
                                            SendChatMessage(config.KothName + " Spawning loot!");
                                            Alliance alliance = GetAlliance(config.owner);
                                            if (alliance != null)
                                            {
                                                alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                                if (config.SpaceMoneyReward > 0)
                                                {
                                                    DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                                }
                                                SaveAllianceData(alliance);
                                                SaveKothConfig(config.KothName, config);
                                            }
                                        }
                                    }
                                    continue;

                                }
                            }
                            if (hasCap && config.DoCaptureBlockHalfLootTime)
                            {
                              //  Log.Info("The owner has an active block so reducing time between spawning");
                                if (lootgrid != null)
                                {
                                    SpawnCores(lootgrid, config);

                                }

                                config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn / 2);
                                SendChatMessage(config.KothName + " Spawning loot!");
                                Alliance alliance = GetAlliance(config.owner);
                                if (alliance != null)
                                {
                                    alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                    if (config.SpaceMoneyReward > 0)
                                    {
                                        DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                    }
                                    SaveAllianceData(alliance);

                                }
                                SaveKothConfig(config.KothName, config);
                            }
                            else
                            {
                             //  Log.Info("No block");
                                if (lootgrid != null)
                                {
                                    SpawnCores(lootgrid, config);

                                }
                                SendChatMessage(config.KothName + " Spawning loot!");
                                config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                                Alliance alliance = GetAlliance(config.owner);
                                if (alliance != null)
                                {
                                    alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                    if (config.SpaceMoneyReward > 0)
                                    {
                                        DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                    }
                                    SaveAllianceData(alliance);
                                }
                                SaveKothConfig(config.KothName, config);
                            }
                        }
                        else
                        {
                        //    Log.Info("No owner, normal spawn time");
                            //          if (lootgrid != null)
                            //     {
                            //            SpawnCores(lootgrid, config);

                            //     }
                            config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                            //    SendChatMessage("No owner, normal spawn time");
                            SaveKothConfig(config.KothName, config);
                        }

                    }

                    contested = false;
                    hasActiveCaptureBlock = false;
                }


                catch (Exception ex)
                {
                    Log.Error("koth error " + ex.ToString());
                    config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                }
            }
        }
        public override void Update()
        {
            ticks++;
            if (ticks % 512 == 0)
            {
                try
                {
                    DoTaxStuff();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

            }
            try
            {
                if (config.KothEnabled)
                {
                    DoKothStuff();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            if (DateTime.Now > NextUpdate)
            {
                DiscordStuff.RegisterDiscord();
                Log.Info("Doing alliance tasks");
                DateTime now = DateTime.Now;
                //try
                //{
                //    if (now.Minute == 59 || now.Minute == 60)
                //    {
                //        NextUpdate = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                //    }
                //    else
                //    {
                //        NextUpdate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                //    }
                //}
                //catch (Exception)
                //{
                NextUpdate = now.AddSeconds(60);
                // }
                try
                {
                    MarketCommands.list.RefreshList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                try
                {
                    LoadAllAlliances();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                try
                {
                    LoadAllGates();
                }
                catch (Exception ex)
                {

                    Log.Error(ex);
                }

                try
                {
                    LoadAllJumpZones();

                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                try
                {
                    OrganisePlayers();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);

                }
            }
            if (ticks % 32 == 0)
            {
                if (config.JumpGatesEnabled)
                {
                    try
                    {
                        DoJumpGateStuff();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }



        }
        public static MyCubeGrid GetLootboxGrid(Vector3 position, KothConfig config)
        {
            if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) != null)
            {
                if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) is MyCubeGrid grid)
                    return grid;
            }
            BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre + 5000);
            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if (fac != null && fac.Tag.Equals(config.KothBuildingOwner))
                {

                    Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                    if (block != null)
                    {
                        return grid;
                    }

                }

            }
            return null;
        }

        public static void SpawnCores(MyCubeGrid grid, KothConfig config)
        {
            if (grid != null)
            {
                Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                MyDefinitionId rewardItem = getRewardItem(config);
                Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                if (block != null && rewardItem != null)
                {
                 //   Log.Info("Should spawn item");
                    MyItemType itemType = new MyInventoryItemFilter(rewardItem.TypeId + "/" + rewardItem.SubtypeName).ItemType;
                    block.GetInventory().AddItems((MyFixedPoint)config.RewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(rewardItem));

                }
                else
                {
                  //  Log.Info("Cant spawn item");
                }
                return;
            }



        }


        public static Boolean DoesGridHaveCaptureBlock(MyCubeGrid grid, KothConfig koth, Boolean ignoreOwner = false)
        {
            foreach (MyCubeBlock block in grid.GetFatBlocks())
            {

                if (block.OwnerId > 0 && block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "").Equals(koth.captureBlockType) && block.BlockDefinition.Id.SubtypeName.Equals(koth.captureBlockSubtype))
                {

                    if (block.IsFunctional && block.IsWorking)
                    {
                        if (block is Sandbox.ModAPI.IMyFunctionalBlock bl)
                        {
                            bl.Enabled = true;
                        }

                        if (block is Sandbox.ModAPI.IMyBeacon beacon)
                        {
                            beacon.Radius = koth.captureBlockBroadcastDistance;
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        public static KothConfig SaveKothConfig(String name, KothConfig config)
        {
            FileUtils utils = new FileUtils();
            utils.WriteToXmlFile<KothConfig>(basePath + "//Alliances//KOTH//" + name + ".xml", config);

            return config;
        }
        public static Boolean IsContested(IMyFaction fac, KothConfig koth, Guid capturingNation)
        {

            if (GetNationTag(fac) != null)
            {
                if (capturingNation.Equals(GetNationTag(fac).AllianceId))
                    return false;
                else
                {
                    return true;
                }
            }
            else
            {
                //unaff cant capture
                return true;
            }
            return false;
        }
        public static MyDefinitionId getRewardItem(KothConfig config)
        {
            MyDefinitionId.TryParse("MyObjectBuilder_" + config.RewardTypeId, config.RewardSubTypeId, out MyDefinitionId id);
            return id;
        }
        public static Alliance GetNationTag(IMyFaction fac)
        {
            if (GetAllianceNoLoading(fac as MyFaction) != null)
            {
                return GetAllianceNoLoading(fac as MyFaction);
            }
            return null;
        }
        public static void SendChatMessage(String message, ulong steamID = 0)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = "KOTH";
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = Color.OrangeRed;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId(steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
        public static string GetPlayerName(ulong steamId)
        {
            MyIdentity id = GetIdentityByNameOrId(steamId.ToString());
            if (id != null && id.DisplayName != null)
            {
                return id.DisplayName;
            }
            else
            {
                return steamId.ToString();
            }
        }
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
    }
}
