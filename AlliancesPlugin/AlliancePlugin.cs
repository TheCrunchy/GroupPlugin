using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//Alliances//KOTH");
            }
            else
            {
                folder2 = config.StoragePath + "//KOTH";
            }
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

            if (state == TorchSessionState.Loaded)
            {


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
                SetupFriendMethod();

                LoadAllAlliances();
                LoadAllGates();
                playersInAlliances.Clear();
                AlliancePlugin.Log.Info("Adding players to list");
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {

                    if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                    {
                        Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                        if (temp != null)
                        {
                            if (playersInAlliances.ContainsKey(temp.AllianceId))
                            {
                                playersInAlliances[temp.AllianceId].Add(player.Id.SteamId);
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                            else
                            {
                                List<ulong> bob = new List<ulong>();
                                bob.Add(player.Id.SteamId);
                                playersInAlliances.Add(temp.AllianceId, bob);
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                        }
                    }
                }

                if (!File.Exists(path + "//KOTH//example.xml"))
                {
                    utils.WriteToXmlFile<KothConfig>(path + "//KOTH//example.xml", new KothConfig(), false);
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//");
                }
                if (!Directory.Exists(path + "//HangarUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//HangarUpgrades//");
                }
                if (!Directory.Exists(path + "//JumpGates//"))
                {
                    Directory.CreateDirectory(path + "//JumpGates//");
                }
                if (!File.Exists(path + "//ShipyardUpgrades//SpeedUpgrade1.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("1,Speed,7");
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    File.WriteAllText(path + "//ShipyardUpgrades//SpeedUpgrade1.txt", output.ToString());

                }
                if (!File.Exists(path + "//HangarUnlockCost.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
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
                        loc.y= Double.Parse(split[2]);
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
                    File.WriteAllText(path + "//HangarUpgrades//SlotUpgrade1.txt", output.ToString());

                }
                if (!File.Exists(path + "//ShipyardUnlockCost.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
                    File.WriteAllText(path + "//ShipyardUnlockCost.txt", output.ToString());

                }
                if (!File.Exists(path + "//ShipyardUpgrades//SlotUpgrade1.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("1,Slots,2");
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,5000");
                    output.AppendLine("Money,500000000");
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

                foreach (String s in Directory.GetFiles(path + "//KOTH//"))
                {


                    KothConfig koth = utils.ReadFromXmlFile<KothConfig>(s);

                    KOTHs.Add(koth);
                }

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
        public void LoadAllAlliances()
        {
            if (TorchState == TorchSessionState.Loaded)
            {
                FileUtils jsonStuff = new FileUtils();
                try
                {
                    AllAlliances.Clear();
                    foreach (String s in Directory.GetFiles(path + "//AllianceData//"))
                    {

                        Alliance alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                        if (AllAlliances.ContainsKey(alliance.name))
                        {
                            AllAlliances[alliance.name] = alliance;
                            FactionsInAlliances.Clear();
                            foreach (long id in alliance.AllianceMembers)
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }
                        else
                        {
                            AllAlliances.Add(alliance.name, alliance);
                            FactionsInAlliances.Clear();
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
                foreach (Alliance alliance in AllAlliances.Values)
                {
                    alliance.ForceFriendlies();
                    alliance.ForceEnemies();
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
                            temp = LoadAllianceData(alliance.AllianceId);
                           temp.GateFee(gate.fee, player.Id.SteamId, gate.GateName);
                           
                          
                        }
                    }
                    if (temp != null)
                    {
                        SaveAllianceData(temp);
                    }
                    EconUtils.takeMoney(player.Identity.IdentityId, gate.fee);
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
                        messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(500));
                        return true;
                    }
                }
            }
            return false;
        }
        public static Dictionary<long, DateTime> messageCooldowns = new Dictionary<long, DateTime>();
        public override void Update()
        {
            ticks++;
            if (ticks % 32 == 0)
            {

                if (DateTime.Now > NextUpdate)
                {

                    NextUpdate = DateTime.Now.AddMinutes(1);
                    LoadAllAlliances();
                    LoadAllGates();
                    playersInAlliances.Clear();
                    playersAllianceId.Clear();
                    foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                    {
                        if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                        {
                            Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                            if (temp != null)
                            {
                                if (playersInAlliances.ContainsKey(temp.AllianceId))
                                {
                                    playersInAlliances[temp.AllianceId].Add(player.Id.SteamId);
                                    playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                                }
                                else
                                {
                                    List<ulong> bob = new List<ulong>();
                                    bob.Add(player.Id.SteamId);
                                    playersInAlliances.Add(temp.AllianceId, bob);
                       
                                    playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                                }
                            }
                        }
                    }
                }
                if (config.JumpGatesEnabled)
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
                                    MatrixD worldMatrix = MatrixD.CreateWorld(target.Position + offset, controller.CubeGrid.WorldMatrix.Forward, controller.CubeGrid.WorldMatrix.Up);
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
                try
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
                        if (!config.enabled)
                            continue;

                        bool contested = false;
                        Boolean hasActiveCaptureBlock = false;
                        // Log.Info("We capping?");
                        Vector3 position = new Vector3(config.x, config.y, config.z);
                        BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre);

                        if (DateTime.Now >= config.nextCaptureInterval)
                        {

                            //setup a time check for capture time
                            String capturingNation = "";

                            Boolean locked = false;

                            Log.Info("Yeah we capping");



                            int entitiesInCapPoint = 0;
                            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                            {
                                entitiesInCapPoint++;
                                if (!contested)
                                {
                                    IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                    if (fac != null && !fac.Tag.Equals(config.KothBuildingOwner))
                                    {

                                        if (IsContested(fac, config, capturingNation))
                                        {
                                            contested = true;
                                            break;
                                        }
                                        else
                                        {
                                            capturingNation = GetNationTag(fac);
                                        }

                                    }
                                    hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, config);
                                }
                            }

                            if (!contested)
                            {
                                //now check characters
                                foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                                {
                                    entitiesInCapPoint++;
                                    IMyFaction fac = FacUtils.GetPlayersFaction(character.GetPlayerIdentityId());
                                    if (fac != null)
                                    {
                                        float distance = Vector3.Distance(position, character.PositionComp.GetPosition());
                                        if (IsContested(fac, config, capturingNation))
                                        {
                                            contested = true;
                                            break;
                                        }
                                        else
                                        {
                                            capturingNation = GetNationTag(fac);
                                        }
                                    }
                                    else
                                    {
                                        contested = true;
                                    }
                                }
                            }

                            if (entitiesInCapPoint == 0 && config.IsDenialPoint)
                            {
                                if (denials.TryGetValue(config.DeniedKoth, out DenialPoint den))
                                {
                                    den.RemoveCap(config.KothName);
                                    SaveKothConfig(config.KothName, config);
                                }
                            }
                            if (!contested && hasActiveCaptureBlock && !config.CaptureStarted && !capturingNation.Equals(""))
                            {
                                config.CaptureStarted = true;
                                config.nextCaptureAvailable = DateTime.Now.AddMinutes(config.MinutesBeforeCaptureStarts);
                                Log.Info("Can cap in 10 minutes");
                                config.capturingNation = capturingNation;
                                SendChatMessage("Can cap in however many minutes");
                            }
                            else
                            {
                                if (!contested && !capturingNation.Equals(""))
                                {
                                    Log.Info("Got to the capping check and not contested");
                                    if (DateTime.Now >= config.nextCaptureAvailable && config.CaptureStarted)
                                    {
                                        if (config.capturingNation.Equals(capturingNation) && !config.capturingNation.Equals(""))
                                        {
                                            Log.Info("Is the same nation as whats capping");
                                            if (!hasActiveCaptureBlock)
                                            {
                                                Log.Info("Locking because no active cap block");
                                                config.capturingNation = config.owner;
                                                config.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                                //broadcast that its locked
                                                config.capturingNation = "";
                                                config.amountCaptured = 0;
                                                SendChatMessage("Locked because capture blocks are dead");
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
                                                    Log.Info("Locking because points went over the threshold");
                                                    locked = true;
                                                    config.nextCaptureInterval = DateTime.Now.AddHours(config.hoursToLockAfterCap);
                                                    config.capturingNation = capturingNation;
                                                    config.owner = capturingNation;
                                                    config.amountCaptured = 0;
                                                    SendChatMessage(config.captureMessage.Replace("%NATION%", config.owner));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Log.Info("Locking because the capturing nation changed");
                                            config.capturingNation = config.owner;
                                            config.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                            //broadcast that its locked
                                            SendChatMessage("Locked because capturing nation has changed.");
                                            config.amountCaptured = 0;

                                        }
                                    }
                                    else
                                    {
                                        SendChatMessage("Waiting to cap");
                                        Log.Info("Waiting to cap");
                                    }
                                }
                                else
                                {
                                    Log.Info("Its contested or the fuckers trying to cap have no nation");
                                    //send contested message
                                    SendChatMessage("Contested or unaff trying to cap");
                                }


                            }

                            if (!locked)
                            {
                                config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                            }
                            SaveKothConfig(config.KothName, config);
                        }


                        //if its not locked, check again for capture in a minute



                        if (DateTime.Now > config.nextCoreSpawn && !config.IsDenialPoint)
                        {
                            MyCubeGrid lootgrid = GetLootboxGrid(position, config);
                            //spawn the cores
                            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                            {
                                IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                if (fac != null)
                                {
                                    if (GetNationTag(fac) != null && GetNationTag(fac).Equals(config.owner))
                                    {
                                        hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, config);
                                    }

                                }

                            }
                            if (denials.TryGetValue(config.KothName, out DenialPoint den))
                            {
                                if (den.IsDenied())
                                    SendChatMessage("Denied point, no core spawn");
                                continue;
                            }
                            if (!config.owner.Equals("NOBODY"))
                            {

                                if (hasActiveCaptureBlock)
                                {
                                    Log.Info("The owner has an active block so reducing time between spawning");
                                    SpawnCores(lootgrid, config);
                                    config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn / 2);
                                    SendChatMessage("Capture block and owned, half spawn time");
                                }
                                else
                                {
                                    Log.Info("No block");
                                    SpawnCores(lootgrid, config);
                                    SendChatMessage("No capture block and owned, normal spawn time");
                                    config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                                }
                            }
                            else
                            {
                                Log.Info("No owner, normal spawn time");
                                SpawnCores(lootgrid, config);
                                config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                                SendChatMessage("No owner, normal spawn time");
                            }

                        }

                        contested = false;
                        hasActiveCaptureBlock = false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("koth error " + ex.ToString());
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
                    Log.Info("Should spawn item");
                    MyItemType itemType = new MyInventoryItemFilter(rewardItem.TypeId + "/" + rewardItem.SubtypeName).ItemType;
                    block.GetInventory().AddItems((MyFixedPoint)config.RewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(rewardItem));

                }
                else
                {
                    Log.Info("Cant spawn item");
                }
                return;
            }



        }


        public static Boolean DoesGridHaveCaptureBlock(MyCubeGrid grid, KothConfig koth)
        {
            foreach (MyCubeBlock block in grid.GetFatBlocks())
            {
                if (block != null && block.BlockDefinition != null)
                {
                    Log.Info(block.BlockDefinition.Id.TypeId + " " + block.BlockDefinition.Id.SubtypeName);

                }
                else
                {
                    Log.Info("Null id for capture block");
                }

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
            utils.WriteToXmlFile<KothConfig>(path + "//KOTH" + name + ".xml", config);

            return config;
        }
        public static Boolean IsContested(IMyFaction fac, KothConfig koth, string capturingNation)
        {

            if (GetNationTag(fac) != null)
            {
                if (capturingNation.Equals(GetNationTag(fac)) || capturingNation.Equals(""))
                    capturingNation = GetNationTag(fac);
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
        public static string GetNationTag(IMyFaction fac)
        {
            if (GetAllianceNoLoading(fac as MyFaction) != null)
            {
                return GetAllianceNoLoading(fac as MyFaction).name;
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
