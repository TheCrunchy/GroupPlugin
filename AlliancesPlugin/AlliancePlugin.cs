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
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;
using Sandbox.Game.Screens.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using DSharpPlus;
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using HarmonyLib;
using AlliancesPlugin.NewCaptureSite;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Weapons;
using Sandbox.Game;
using static AlliancesPlugin.Alliances.StorePatchTaxes;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class AlliancePlugin : TorchPluginBase
    {
        public static MethodInfo sendChange;
        public static TorchSessionState TorchState;
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
        public DateTime NextMining = DateTime.Now;

        public static FileUtils utils = new FileUtils();
        public static Dictionary<string, DenialPoint> denials = new Dictionary<string, DenialPoint>();
        public static ITorchPlugin GridBackup;
        public static MethodInfo BackupGrid;

        public static ITorchBase TorchBase;
        public static bool GridBackupInstalled = false;
        public static Dictionary<MyDefinitionId, int> ItemUpkeep = new Dictionary<MyDefinitionId, int>();
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


        public static long AddToTaxes(ulong SteamId, long amount, string type, Vector3D Location)
        {
            MyIdentity identityId = AlliancePlugin.GetIdentityByNameOrId(SteamId.ToString());
            Alliance alliance = null;
            IMyFaction fac = FacUtils.GetPlayersFaction(identityId.IdentityId);
            if (fac != null)
            {
                alliance = GetAllianceNoLoading(fac as MyFaction);
                if (alliance != null)
                {
                    // alliance.reputation
                    //amount = Convert.ToInt64(amount * 1.05f);
                }
            }

            foreach (Territory ter in AlliancePlugin.Territories.Values)
            {
                if (alliance != null)
                {
                    if (ter.Alliance != Guid.Empty && ter.Alliance == alliance.AllianceId)
                    {
                        continue;
                    }
                }
                if (ter.TaxesForStationsInTerritory && ter.Alliance != Guid.Empty)
                {
                    float distance = Vector3.Distance(new Vector3(ter.x, ter.y, ter.z), Location);
                    if (distance <= ter.Radius)
                    {
                        TaxItem item = new TaxItem();
                        item.playerId = identityId.IdentityId;
                        item.price = amount;
                        item.territory = ter.Id;

                        AlliancePlugin.TerritoryTaxes.Add(item);
                        return amount;
                    }
                }
            }

            if (AlliancePlugin.TaxesToBeProcessed.ContainsKey(identityId.IdentityId))
            {

                AlliancePlugin.TaxesToBeProcessed[identityId.IdentityId] += amount;
            }
            else
            {
                AlliancePlugin.TaxesToBeProcessed.Add(identityId.IdentityId, amount);

            }
            return amount;
        }

        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.AddOverrideMod(758597413L);
            sessionManager.AddOverrideMod(2612907530L);
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            if (!Directory.Exists(path + "//CaptureSites//"))
            {
                Directory.CreateDirectory(path + "//CaptureSites//");

            }
            if (!Directory.Exists(path + "//JumpGates//"))
            {
                Directory.CreateDirectory(path + "//JumpGates//");
            }
            if (!Directory.Exists(path + "//Vaults//"))
            {
                Directory.CreateDirectory(path + "//Vaults//");
            }
            if (!Directory.Exists(path + "//Territories//"))
            {
                Directory.CreateDirectory(path + "//Territories//");
            }
            if (!File.Exists(path + "//Territories//Example.xml"))
            {
                Territory example = new Territory();
                example.enabled = false;
                utils.WriteToXmlFile<Territory>(path + "//Territories//Example.xml", example, false);
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
            LoadItemUpkeep();
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
        public static Boolean YEETED = false;
        public static int seconds = 15;
        public static void LoadAllCaptureSites()
        {
            sites.Clear();

            foreach (String s in Directory.GetFiles(path + "//CaptureSites//"))
            {

                try
                {
                    CaptureSite koth = utils.ReadFromXmlFile<CaptureSite>(s);
                    koth.LoadCapProgress();
                    koth.caplog.LoadSorted();
                    if (!YEETED)
                    {
                        koth.nextCaptureAvailable = koth.nextCaptureAvailable.AddSeconds(seconds);
                        koth.nextCaptureInterval = koth.nextCaptureInterval.AddSeconds(seconds);
                        seconds += 15;
                    }
                    //  DateTime now = DateTime.Now;
                    //if (now.Minute == 59 || now.Minute == 60)
                    //{
                    //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                    //}
                    //else
                    //{
                    //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                    //}

                    sites.Add(koth);
                }
                catch (Exception ex)
                {

                    Log.Error("Delete this file and generate a new one " + s);
                }
                YEETED = true;

            }
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
        private static List<Vector3> StationLocations = new List<Vector3>();
        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }
        public static ChatManagerServer _chatmanager;

        public static long GetAttacker(long attackerId)
        {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return 0L;

            if (entity is MyPlanet)
            {

                return 0L;
            }

            if (entity is MyCharacter character)
            {

                return character.GetPlayerIdentityId();
            }

            if (entity is IMyEngineerToolBase toolbase)
            {

                return toolbase.OwnerIdentityId;

            }

            if (entity is MyLargeTurretBase turret)
            {

                return turret.OwnerId;

            }

            if (entity is MyShipToolBase shipTool)
            {

                return shipTool.OwnerId;
            }

            if (entity is IMyGunBaseUser gunUser)
            {

                return gunUser.OwnerId;

            }



            if (entity is MyCubeGrid grid)
            {

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                return gridOwner;

            }

            return 0L;
        }
        public static Boolean Debug = false;

        private void DamageHandler(object target, ref MyDamageInformation info)
        {

            if (config == null)
            {
                return;
            }
         //   Log.Info(info.Type.String);

            if (config.DisablePvP)
            {
                long attackerId = GetAttacker(info.AttackerId);
                //    Log.Info(attackerId);
                //  Log.Info(info.Type.ToString());
                //check if in zone
                MyFaction attacker = MySession.Static.Factions.GetPlayerFaction(attackerId) as MyFaction;


                if (target is MyCharacter character)
                {
                 //   Log.Info(info.Type.ToString());
                    if (info.Type.String.Equals("Environment") || info.Type.String.Equals("Asphyxia") || info.Type.String.Equals("LowPressure") || info.Type.String.Equals("Spider") || info.Type.String.Equals("Wolf") || info.Type.String.Equals("Fall") || info.Type.String.Equals("Suicide"))
                    {
                        return;
                    }
                    if (attacker != null)
                    {
                        if (attacker.Tag.Length > 3)
                        {
                            return;
                        }
                        else
                        {
                            info.Amount = 0.0f;
                            return;
                        }
                    }
                    else
                    {
                        info.Amount = 0.0f;
                        return;
                    }

                }
                //  Log.Info(target.GetType().ToString());

                if (target is MySlimBlock block)
                {
                    //   Log.Info("is an entity");
                    if (FacUtils.GetOwner(block.CubeGrid) == 0L)
                    {
                        return;
                    }
                    if (block.OwnerId == attackerId)
                    {
                        return;
                    }
                    MyFaction defender = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner(block.CubeGrid));
                    //    Log.Info("in distance");
                    if (info.Type.String.Equals("Grind") || info.Type.String.Equals("Explosion"))
                    {
                        //  Log.Info("Grind damage");
                        if (attacker != null)
                        {
                            if (Debug)
                            {
                                AlliancePlugin.Log.Info("attacker has faction");
                            }
                            if (attacker.Tag.Length > 3)
                            {
                                if (Debug)
                                {
                                    AlliancePlugin.Log.Info("NPC fac, allowing");
                                }
                                return;
                            }
                            if (defender != null)
                            {
                                if (Debug)
                                {
                                    AlliancePlugin.Log.Info("defender isnt null");
                                }
                                if (defender.Tag.Length > 3)
                                {
                                    if (Debug)
                                    {
                                        AlliancePlugin.Log.Info("defender is NPC");
                                    }
                                    return;
                                }
                                if (attacker.FactionId == defender.FactionId)
                                {
                                    if (Debug)
                                    {
                                        AlliancePlugin.Log.Info("attacker is defender");
                                    }
                                    return;
                                }
                            }
                            SlimBlockPatch.SendPvEMessage(attackerId);
                            info.Amount = 0.0f;
                            return;
                        }
                        else
                        {
                            if (Debug)
                            {
                                AlliancePlugin.Log.Info("attacker has no faction");
                            }
                            if (defender != null)
                            {
                                if (defender.Tag.Length > 3)
                                {
                                    if (Debug)
                                    {
                                        AlliancePlugin.Log.Info("defender is npc");
                                    }
                                    return;
                                }
                            }
                            
                            SlimBlockPatch.SendPvEMessage(attackerId, true);
                            info.Amount = 0.0f;
                            return;
                      
                        }
                 
                    }

                   
                }
            }

        }

        public static void BalanceChangedMethod2(
     MyAccountInfo oldAccountInfo,
     MyAccountInfo newAccountInfo)
        {



            if (Sync.Players.TryGetPlayerId(newAccountInfo.OwnerIdentifier, out MyPlayer.PlayerId player))
            {
                if (MySession.Static.Players.TryGetPlayerById(player, out MyPlayer pp))
                {

                    MySession.Static.Players.TryGetSteamId(newAccountInfo.OwnerIdentifier);
                    //  foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                    //    {
                    //     if (player.Identity.IdentityId == identifierId)
                    //    {
                    long change;
                    if (oldAccountInfo.Balance > newAccountInfo.Balance)
                    {
                        change = oldAccountInfo.Balance - newAccountInfo.Balance;

                        if (TaxingId.Contains(pp.Identity.IdentityId))
                        {
                            ShipyardCommands.SendMessage("CrunchEcon", "Alliances Taxes: " + String.Format("{0:n0}", change) + " SC", Color.HotPink, (long)pp.Id.SteamId);
                            TaxingId.Remove(pp.Identity.IdentityId);
                            return;
                        }
                        if (OtherTaxingId.Contains(pp.Identity.IdentityId))
                        {
                            ShipyardCommands.SendMessage("CrunchEcon", "Territory Taxes: " + String.Format("{0:n0}", change) + " SC", Color.Red, (long)pp.Id.SteamId);
                            OtherTaxingId.Remove(pp.Identity.IdentityId);
                            return;
                        }

                        ShipyardCommands.SendMessage("CrunchEcon", "Balance decreased by: " + String.Format("{0:n0}", change) + " SC", Color.Red, (long)pp.Id.SteamId);
                        return;
                    }

                }
            }


        }


        public static void NEWSUIT(MyEntity entity)
        {
            if (entity is MyCharacter character)
            {
                //     AlliancePlugin.Log.Info("ITS A SUIT BITCH!");


            }
        }
        public static Random rand = new Random();
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (state == TorchSessionState.Unloading)
            {

                // DiscordStuff.DisconnectDiscord();
                TorchState = TorchSessionState.Unloading;
            }
            if (state == TorchSessionState.Loaded)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, new BeforeDamageApplied(DamageHandler));
                //   MyEntities.OnEntityAdd += NEWSUIT;
                if (config != null && config.AllowDiscord && !DiscordStuff.Ready)
                {
                    DiscordStuff.RegisterDiscord();
                }
                nextRegister = DateTime.Now;
                //    rand.Next(1, 60);

                LoadAllGates();

                MyBankingSystem.Static.OnAccountBalanceChanged += BalanceChangedMethod2;

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
                if (!Directory.Exists(path + "//UpkeepBackups//"))
                {
                    Directory.CreateDirectory(path + "//UpkeepBackups//");
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


                if (!File.Exists(path + "//CaptureSites//Example.xml"))
                {
                    CaptureSite site = new CaptureSite();
                    //  site.HoursToUnlockAfter.Add(5);
                    //site.HoursToUnlockAfter.Add(7);
                    Location loc = new Location();
                    site.locations.Add(loc);
                    LootLocation loot = new LootLocation();
                    RewardItem reward = new RewardItem();
                    reward.CreditReward = 5000000;
                    loc.LinkedLootLocation = 1;
                    loot.loot.Add(reward);
                    reward = new RewardItem();
                    reward.ItemMaxAmount = 5;
                    reward.ItemMinAmount = 1;
                    reward.SubTypeId = "Uranium";
                    reward.TypeId = "Ingot";
                    loot.loot.Add(reward);
                    reward = new RewardItem();

                    reward.MetaPoint = 50;
                    loot.loot.Add(reward);
                    site.loot.Add(loot);
                    utils.WriteToXmlFile<CaptureSite>(path + "//CaptureSites//example.xml", site, false);
                    loc.X = 2;
                    loc.Y = 2;
                    loc.LinkedLootLocation = 1;
                    loc.Z = 2;
                    loc.Num = 2;
                    loc.Name = "Example 2";
                    site.locations.Add(loc);
                    utils.WriteToXmlFile<CaptureSite>(path + "//CaptureSites//example2.xml", site, false);
                }
                if (!Directory.Exists(path + "//WaystationRepairUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//WaystationRepairUpgrades//");
                }
                if (!File.Exists(path + "//WaystationRepairUpgrades//Base.xml"))
                {
                    GridRepairUpgrades upgrade = new GridRepairUpgrades();

                    ItemRequirement req = new ItemRequirement();
                    upgrade.items.Add(req);
                    GridRepairUpgrades.ComponentCostForRepair cost = new GridRepairUpgrades.ComponentCostForRepair();
                    cost.SubTypeId = "SteelPlate";
                    cost.Cost = 100;
                    upgrade.repairCost.Add(cost);
                    upgrade.BannedComponents.Add("AdminKit");
                    upgrade.BannedComponents.Add("ShitminKit");
                    utils.WriteToXmlFile<GridRepairUpgrades>(path + "//WaystationRepairUpgrades//Base.xml", upgrade);

                }
                if (!File.Exists(basePath + "//Alliances//KOTH//example.xml"))
                {
                    utils.WriteToXmlFile<KothConfig>(basePath + "//Alliances//KOTH//example.xml", new KothConfig(), false);
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//");
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//Slot//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//Slot//");
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//Speed//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//Speed//");
                }
                if (!Directory.Exists(path + "//ShipyardUpgrades//OldFiles//"))
                {
                    Directory.CreateDirectory(path + "//ShipyardUpgrades//OldFiles//");
                }
                if (!Directory.Exists(path + "//RefineryUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//RefineryUpgrades//");
                }
                if (!Directory.Exists(path + "//AssemblerUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//AssemblerUpgrades//");
                }
                if (!File.Exists(path + "//AssemblerUpgrades//Example.xml"))
                {
                    AssemblerUpgrade upgrade = new AssemblerUpgrade();
                    AssemblerUpgrade.AssemblerBuffList list = new AssemblerUpgrade.AssemblerBuffList();
                    list.buffs.Add(new AssemblerUpgrade.AssemblerBuff());
                    upgrade.buffedRefineries.Add(list);
                    ItemRequirement req = new ItemRequirement();
                    upgrade.items.Add(req);
                    utils.WriteToXmlFile<AssemblerUpgrade>(path + "//AssemblerUpgrades//Example.xml", upgrade);
                    upgrade.UpgradeId = 2;
                    list.buffs.Add(new AssemblerUpgrade.AssemblerBuff());
                    upgrade.buffedRefineries.Add(list);
                    upgrade.items.Add(req);
                    utils.WriteToXmlFile<AssemblerUpgrade>(path + "//AssemblerUpgrades//Example2.xml", upgrade);

                }
                if (!File.Exists(path + "//RefineryUpgrades//Example.xml"))
                {
                    RefineryUpgrade upgrade = new RefineryUpgrade();
                    RefineryUpgrade.RefineryBuffList list = new RefineryUpgrade.RefineryBuffList();
                    list.buffs.Add(new RefineryUpgrade.RefineryBuff());
                    upgrade.buffedRefineries.Add(list);
                    ItemRequirement req = new ItemRequirement();
                    upgrade.items.Add(req);
                    utils.WriteToXmlFile<RefineryUpgrade>(path + "//RefineryUpgrades//Example.xml", upgrade);
                    upgrade.UpgradeId = 2;
                    list.buffs.Add(new RefineryUpgrade.RefineryBuff());
                    upgrade.buffedRefineries.Add(list);
                    upgrade.items.Add(req);
                    utils.WriteToXmlFile<RefineryUpgrade>(path + "//RefineryUpgrades//Example2.xml", upgrade);

                }
                if (!Directory.Exists(path + "//HangarUpgrades//"))
                {
                    Directory.CreateDirectory(path + "//HangarUpgrades//");
                }
                if (!File.Exists(path + "//ItemUpkeep.txt"))
                {

                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId,SubtypeId,Amount");
                    output.AppendLine("MyObjectBuilder_Ingot,Uranium,1");
                    File.WriteAllText(path + "//ItemUpkeep.txt", output.ToString());

                }

                //convert this to new format 
                if (!File.Exists(path + "//ShipyardUpgrades//Speed//SpeedUpgrade_0.xml"))
                {

                    ShipyardSpeedUpgrade upg = new ShipyardSpeedUpgrade();
                    ItemRequirement req = new ItemRequirement();
                    upg.items.Add(req);
                    utils.WriteToXmlFile<ShipyardSpeedUpgrade>(path + "//ShipyardUpgrades//Speed//SpeedUpgrade_0.xml", upg);

                }
                if (!File.Exists(path + "//ShipyardUpgrades//Slot//SlotUpgrade_0.xml"))
                {

                    ShipyardSlotUpgrade upg = new ShipyardSlotUpgrade();
                    ItemRequirement req = new ItemRequirement();
                    upg.items.Add(req);
                    utils.WriteToXmlFile<ShipyardSlotUpgrade>(path + "//ShipyardUpgrades//Slot//SlotUpgrade_0.xml", upg);

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
                if (!File.Exists(path + "//HangarUpgrades//SlotUpgrade_0.xml"))
                {

                    HangarUpgrade upg = new HangarUpgrade();
                    ItemRequirement req = new ItemRequirement();
                    upg.items.Add(req);
                    utils.WriteToXmlFile<HangarUpgrade>(path + "//HangarUpgrades//SlotUpgrade_0.xml", upg);

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

                ReloadShipyard();

                foreach (String s in Directory.GetFiles(basePath + "//Alliances//KOTH//"))
                {


                    KothConfig koth = utils.ReadFromXmlFile<KothConfig>(s);

                    KOTHs.Add(koth);
                }
                SetupFriendMethod();

                LoadAllAlliances();
                LoadAllGates();
                LoadAllRefineryUpgrades();


                LoadItemUpkeep();


                foreach (Alliance alliance in AllAlliances.Values)
                {
                    try
                    {
                        alliance.ForceFriendlies();
                        alliance.ForceEnemies();
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
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
                        //    if (!botsTried.Contains(alliance.AllianceId))
                        //    {
                        //   botsTried.Add(alliance.AllianceId);
                        Log.Info("Registering bot for " + alliance.AllianceId);
                        registerThese.Add(alliance.AllianceId, nextRegister.AddSeconds(15));





                    }
                }
                //        DatabaseForBank bank = new DatabaseForBank();
                //    bank.CreateTable(bank.CreateConnection());
            }
        }
        public static void LoadItemUpkeep()
        {
            String[] line;
            line = File.ReadAllLines(path + "//ItemUpkeep.txt");
            for (int i = 1; i < line.Length; i++)
            {

                String[] split = line[i].Split(',');
                foreach (String s in split)
                {
                    s.Replace(" ", "");
                }

                if (MyDefinitionId.TryParse(split[0], split[1], out MyDefinitionId id))
                {
                    if (ItemUpkeep.ContainsKey(id))
                    {
                        ItemUpkeep[id] += int.Parse(split[2]);
                    }
                    else
                    {
                        ItemUpkeep.Add(id, int.Parse(split[2]));
                    }

                }

            }
        }
        public static DateTime nextRegister = DateTime.Now.AddSeconds(60);
        public static Dictionary<Guid, DateTime> registerThese = new Dictionary<Guid, DateTime>();

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
                ShipyardCommands.ConvertUpgradeCost(s);
            }
            foreach (String s in Directory.GetFiles(path + "//ShipyardUpgrades//Slot//"))
            {
                ShipyardCommands.LoadShipyardSlotCost(s);

            }
            foreach (String s in Directory.GetFiles(path + "//ShipyardUpgrades//Speed//"))
            {
                ShipyardCommands.LoadShipyardSpeedCost(s);

            }
            foreach (String s in Directory.GetFiles(path + "//HangarUpgrades//"))
            {
                HangarCommands.LoadHangarUpgrade(s);
            }
        }
        public void LoadAllRefineryUpgrades()
        {
            GridRepair.upgrades.Clear();
            foreach (String s in Directory.GetFiles(path + "//WaystationRepairUpgrades//"))
            {
                GridRepairUpgrades upgrade = utils.ReadFromXmlFile<GridRepairUpgrades>(s);
                if (upgrade.Enabled)
                {
                    upgrade.AddComponentCostToDictionary();
                    if (!GridRepair.upgrades.ContainsKey(upgrade.UpgradeId))
                    {
                        GridRepair.upgrades.Add(upgrade.UpgradeId, upgrade);
                    }
                    else
                    {
                        Log.Error("Duplicate ID for upgrades " + s);
                    }
                }
            }
            MyProductionPatch.upgrades.Clear();
            foreach (String s in Directory.GetFiles(path + "//RefineryUpgrades//"))
            {
                RefineryUpgrade upgrade = utils.ReadFromXmlFile<RefineryUpgrade>(s);
                if (upgrade.Enabled)
                {
                    upgrade.PutBuffedInDictionary();
                    if (!MyProductionPatch.upgrades.ContainsKey(upgrade.UpgradeId))
                    {
                        MyProductionPatch.upgrades.Add(upgrade.UpgradeId, upgrade);
                    }
                    else
                    {
                        Log.Error("Duplicate ID for upgrades " + s);
                    }
                }
            }
            MyProductionPatch.assemblerupgrades.Clear();
            foreach (String s in Directory.GetFiles(path + "//AssemblerUpgrades//"))
            {
                AssemblerUpgrade upgrade = utils.ReadFromXmlFile<AssemblerUpgrade>(s);
                if (upgrade.Enabled)
                {
                    upgrade.PutBuffedInDictionary();
                    if (!MyProductionPatch.assemblerupgrades.ContainsKey(upgrade.UpgradeId))
                    {
                        MyProductionPatch.assemblerupgrades.Add(upgrade.UpgradeId, upgrade);
                    }
                    else
                    {
                        Log.Error("Duplicate ID for upgrades " + s);
                    }
                }
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
        public static void LoadAllAlliancesForUpkeep()
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
                            if (!FactionsInAlliances.ContainsKey(id))
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }
                        SaveAllianceData(alliance);
                    }
                    else
                    {
                        AllAlliances.Add(alliance.name, alliance);
                        foreach (long id in alliance.AllianceMembers)
                        {
                            if (!FactionsInAlliances.ContainsKey(id))
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
        public static Boolean HasFailedUpkeep(Alliance alliance)
        {
            if (alliance.failedUpkeep > 0)
            {
                return true;
            }
            return false;
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
                                if (!FactionsInAlliances.ContainsKey(id))
                                {
                                    FactionsInAlliances.Add(id, alliance.name);
                                }
                            }
                            SaveAllianceData(alliance);
                        }
                        else
                        {
                            AllAlliances.Add(alliance.name, alliance);
                            foreach (long id in alliance.AllianceMembers)
                            {
                                if (!FactionsInAlliances.ContainsKey(id))
                                {
                                    FactionsInAlliances.Add(id, alliance.name);
                                }
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
                    if (DiscordStuff.allianceBots.TryGetValue(alliance.AllianceId, out DiscordClient bot))
                    {
                        //   bot.MessageCreated -= DiscordStuff.Discord_AllianceMessage;
                        //  bot.MessageCreated += DiscordStuff.Discord_AllianceMessage;

                    }
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

                        //    if (!botsTried.Contains(alliance.AllianceId))
                        //    {
                        //   botsTried.Add(alliance.AllianceId);
                        //    DiscordStuff.RegisterAllianceBot(alliance, alliance.DiscordChannelId);




                    }
                }

            }
        }
        public static List<IMyIdentity> GetAllIdentitiesByNameOrId(string playerNameOrSteamId)
        {
            List<IMyIdentity> ids = new List<IMyIdentity>();
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                {
                    if (!ids.Contains(identity))
                    {
                        ids.Add(identity);
                    }
                }
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                    {
                        if (!ids.Contains(identity))
                        {
                            ids.Add(identity);
                        }

                    }
                    if (identity.IdentityId == (long)steamId)
                    {
                        if (!ids.Contains(identity))
                        {
                            ids.Add(identity);
                        }
                    }
                }

            }
            return ids;
        }


        public static void LoadAllTerritories()
        {
            Territories.Clear();
            FileUtils jsonStuff = new FileUtils();

            foreach (String s in Directory.GetFiles(path + "//Territories//"))
            {
                try
                {
                    Territory ter = jsonStuff.ReadFromXmlFile<Territory>(s);
                    if (!ter.enabled)
                    {
                        continue;
                    }

                    Territories.Add(ter.Id, ter);

                    //     Log.Info(ter.Name);
                    if (DateTime.Now >= ter.DisableZoneAt && ter.ZoneIsEnabled)
                    {
                        //CONSUME CHIPS
                        Vector3 Position = new Vector3(ter.stationX, ter.stationY, ter.stationZ);
                        BoundingSphereD sphere = new BoundingSphereD(Position, 1000);
                        Alliance alliance = AlliancePlugin.GetAlliance(ter.Alliance);
                        if (alliance == null)
                        {
                            //  Log.Info("null alliance");
                            continue;
                        }
                        List<VRage.Game.ModAPI.IMyInventory> zoneInvent = new List<VRage.Game.ModAPI.IMyInventory>();

                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            //   Log.Info("Grid? " + grid.DisplayNameText);

                            if (fac != null)
                            {
                                // Log.Info("Fac isnt null");
                                if (!fac.Tag.Equals(ter.FactionTagForStationOwner))
                                {
                                    //    Log.Info("Fac isnt the configured owner");
                                    continue;
                                }
                            }
                            List<long> blockIds = new List<long>();
                            foreach (MySafeZoneBlock block in grid.GetFatBlocks().OfType<MySafeZoneBlock>())
                            {
                                blockIds.Add(block.EntityId);
                                for (int i = 0; i < block.InventoryCount; i++)
                                {

                                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                                    zoneInvent.Add(inv);

                                }
                            }
                            Dictionary<MyDefinitionId, int> chips = new Dictionary<MyDefinitionId, int>();
                            MyDefinitionId.TryParse("MyObjectBuilder_Component/ZoneChip", out MyDefinitionId id);
                            chips.Add(id, ter.ZoneChipUse);
                            if (ShipyardCommands.ConsumeComponents(zoneInvent, chips, 0l))
                            {
                                ter.DisableZoneAt = DateTime.Now.AddHours(ter.HoursPerChip);
                            }
                            else
                            {
                                ter.ZoneIsEnabled = false;
                                FunctionalBlockPatch.DisableThese.AddRange(blockIds);
                            }

                            utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                            Territories[ter.Id] = ter;
                        }
                    }
                    if (DateTime.Now >= ter.transferTime)
                    {
                        //  Log.Info("Transferring? " + ter.Name);
                        if (ter.transferTo == ter.previousOwner)
                        {
                            //   Log.Info("Same owner, not transferring.");
                            continue;
                        }
                        Vector3 Position = new Vector3(ter.stationX, ter.stationY, ter.stationZ);
                        BoundingSphereD sphere = new BoundingSphereD(Position, 1000);
                        Alliance alliance = AlliancePlugin.GetAlliance(ter.transferTo);
                        if (alliance == null)
                        {
                            //  Log.Info("null alliance");
                            continue;
                        }
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            //   Log.Info("Grid? " + grid.DisplayNameText);

                            if (fac != null)
                            {
                                // Log.Info("Fac isnt null");
                                if (!fac.Tag.Equals(ter.FactionTagForStationOwner))
                                {
                                    //    Log.Info("Fac isnt the configured owner");
                                    continue;
                                }

                                Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                                List<Sandbox.ModAPI.IMyTerminalBlock> blocks = new List<Sandbox.ModAPI.IMyTerminalBlock>();




                                gridTerminalSys.GetBlocksOfType<MyStoreBlock>(blocks);
                                bool transferred = false;
                                foreach (MySafeZoneBlock block in grid.GetFatBlocks().OfType<MySafeZoneBlock>())
                                {
                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);

                                    block.Enabled = false;
                                    foreach (MySafeZone zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                                    {
                                        zone.Factions.Clear();
                                        zone.Players.Clear();
                                        zone.Entities.Clear();
                                        zone.AccessTypeFactions = MySafeZoneAccess.Blacklist;
                                        zone.AccessTypePlayers = MySafeZoneAccess.Blacklist;
                                        zone.AccessTypeGrids = MySafeZoneAccess.Blacklist;
                                        zone.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
                                    }
                                    DiscordStuff.SendMessageToDiscord("Transfer of " + grid.DisplayName + " complete.");
                                }
                                foreach (MyBeacon Beacon in grid.GetFatBlocks().OfType<MyBeacon>())
                                {

                                    // Beacon.DisplayNameText = "Beacon - Owned by " + alliance.name;
                                    try
                                    {

                                        StringBuilder sb = new StringBuilder();
                                        sb.Append("Owned by " + alliance.name);
                                        MethodInfo methodInfo = typeof(MyBeacon).GetMethod("SetHudText", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(String) }, null);
                                        object[] MethodInput = new object[] { sb.ToString() };
                                        //  parameters.AddItem
                                        methodInfo.Invoke(Beacon, MethodInput);
                                    }
                                    catch (Exception ex)
                                    {

                                        Log.Error(ex);
                                    }
                                }

                                foreach (MyStoreBlock block in grid.GetFatBlocks().OfType<MyStoreBlock>())
                                {

                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);
                                }

                                foreach (MyShipConnector block in grid.GetFatBlocks().OfType<MyShipConnector>())
                                {

                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);
                                }


                                foreach (MyRefinery block in grid.GetFatBlocks().OfType<MyRefinery>())
                                {

                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);
                                }

                                foreach (MyAssembler block in grid.GetFatBlocks().OfType<MyAssembler>())
                                {

                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);
                                }

                                foreach (MyCargoContainer block in grid.GetFatBlocks().OfType<MyCargoContainer>())
                                {

                                    FunctionalBlockPatch.transferList.Add(block.EntityId, alliance.SupremeLeader);
                                }

                                ter.previousOwner = ter.transferTo;
                                ter.transferTo = Guid.Empty;
                                ter.transferTime = DateTime.Now.AddYears(1);
                                utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                                Territories[ter.Id] = ter;
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error reading territory " + s);
                    Log.Info(ex);
                }
            }

        }
        public static string WorldName = "";
        public static void LoadAllGates()
        {
            if (TorchState != TorchSessionState.Loaded)
            {
                return;
            }
            List<string> FilesToDelete = new List<string>();
            if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
            {
                WorldName = MyMultiplayer.Static.HostName;
            }
            FileUtils jsonStuff = new FileUtils();
            try
            {
                AllGates.Clear();
                foreach (String s in Directory.GetFiles(path + "//JumpGates//"))
                {
                    JumpGate gate;
                    if (s.EndsWith(".json"))
                    {
                        FilesToDelete.Add(s);
                        gate = jsonStuff.ReadFromJsonFile<JumpGate>(s);
                    }
                    else
                    {
                        gate = jsonStuff.ReadFromXmlFile<JumpGate>(s);
                    }


                    if (gate.CanBeRented && DateTime.Now >= gate.NextRentAvailable)
                    {
                        gate.OwnerAlliance = Guid.Empty;
                        gate.fee = 0;
                    }




                    AllGates.Add(gate.GateId, gate);

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            bool deleted = false;
            foreach (String s in FilesToDelete)
            {
                File.Delete(s);
                deleted = true;
            }
            if (deleted)
            {
                foreach (JumpGate gate in AllGates.Values)
                {
                    gate.Save();
                }
            }
        }
        public static Boolean SendPlayerNotify(MyPlayer player, int milliseconds, string message, string color)
        {
            NotificationMessage message2 = new NotificationMessage();
            if (messageCooldowns.ContainsKey(player.Identity.IdentityId))
            {
                if (DateTime.Now < messageCooldowns[player.Identity.IdentityId])
                    return false;

                message2 = new NotificationMessage(message, milliseconds, color);
                //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                messageCooldowns[player.Identity.IdentityId] = DateTime.Now.AddMilliseconds(milliseconds / 2);
                return false;
            }
            else
            {

                message2 = new NotificationMessage(message, milliseconds, color);
                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(milliseconds / 2));
                return false;
            }
        }
        public static Boolean DoFeeStuff(MyPlayer player, JumpGate gate, MyCockpit Controller)
        {
            if (gate.RequireDrive)
            {
                List<MyJumpDrive> drives = new List<MyJumpDrive>();
                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Controller.CubeGrid).GetBlocksOfType<MyJumpDrive>(drives);
                bool enabled = false;
                if (drives.Count == 0)
                {
                    enabled = false;
                }

                foreach (MyJumpDrive drive in drives)
                {
                    if (drive.Enabled && drive.IsFunctional)
                    {
                        enabled = true;
                    }
                }

                if (!enabled)
                {
                    SendPlayerNotify(player, 1000, "Functional Enabled Jump Drive is required!", "Red");
                    return false;
                }
            }
            if (gate.fee > 0)
            {
                if (EconUtils.getBalance(player.Identity.IdentityId) >= gate.fee)
                {
                    Alliance temp = null;
                    if (gate.OwnerAlliance != Guid.Empty)
                    {
                        temp = AlliancePlugin.GetAlliance(gate.OwnerAlliance);
                        if (temp == null)
                        {
                            return true;
                        }
                        if (AlliancePlugin.HasFailedUpkeep(temp))
                        {

                            SendPlayerNotify(player, 1000, "Gate is disabled. Alliance failed upkeep.", "Red");
                            return false;
                        }
                    }
                    else
                    {
                        // EconUtils.takeMoney(player.Identity.IdentityId, gate.fee);
                        return true;
                    }
                    if (DatabaseForBank.AddToBalance(temp, gate.fee))
                    {

                        if (temp != null)
                        {
                            temp.GateFee(gate.fee, player.Id.SteamId, gate.GateName);
                            EconUtils.takeMoney(player.Identity.IdentityId, gate.fee);
                            SaveAllianceData(temp);
                        }

                    }

                    return true;
                }
                else
                {
                    SendPlayerNotify(player, 1000, "It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", "Red");
                    return false;
                }
            }

            return true;
        }

        public static Boolean DoFeeMessage(MyPlayer player, JumpGate gate, float Distance)
        {
            if (gate.fee > 0)
            {
                if (EconUtils.getBalance(player.Identity.IdentityId) >= gate.fee)
                {
                    SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
                    SendPlayerNotify(player, 1000, "It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", "Green");
                    return true;
                }
            }
            return false;
        }
        public static Dictionary<long, long> TaxesToBeProcessed = new Dictionary<long, long>();
        public static Dictionary<long, DateTime> messageCooldowns = new Dictionary<long, DateTime>();
        public static List<TaxItem> TerritoryTaxes = new List<TaxItem>();
        public static Dictionary<ulong, DateTime> yeet = new Dictionary<ulong, DateTime>();
        public void DoJumpGateStuff()
        {
            List<MyPlayer> players = new List<MyPlayer>();
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                //if (yeet.TryGetValue(player.Id.SteamId, out DateTime time))
                //{
                //    if (DateTime.Now >= time)
                //    {
                //        yeet[player.Id.SteamId] = DateTime.Now.AddSeconds(20);
                //        if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                //        {
                //            Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                //            if (temp != null)
                //            {
                //                if (AllianceChat.PeopleInAllianceChat.ContainsKey(player.Id.SteamId))
                //                {
                //                    AllianceCommands.SendStatusToClient(true, player.Id.SteamId);
                //                }
                //                else
                //                {
                //                    AllianceCommands.SendStatusToClient(false, player.Id.SteamId);
                //                }
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    yeet.Add(player.Id.SteamId, DateTime.Now.AddSeconds(20));
                //    if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                //    {
                //        Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                //        if (temp != null)
                //        {
                //            if (AllianceChat.PeopleInAllianceChat.ContainsKey(player.Id.SteamId))
                //            {
                //                AllianceCommands.SendStatusToClient(true, player.Id.SteamId);
                //            }
                //            else
                //            {
                //                AllianceCommands.SendStatusToClient(false, player.Id.SteamId);
                //            }
                //        }
                //    }
                //}

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

                if (!gate.WorldName.Equals(MyMultiplayer.Static.HostName))
                    continue;


                foreach (MyPlayer player in players)
                {
                    if (player?.Controller?.ControlledEntity is MyCockpit controller)
                    {

                        float Distance = Vector3.Distance(gate.Position, controller.PositionComp.GetPosition());
                        if (Distance <= gate.RadiusToJump)
                        {
                            if (!DoFeeStuff(player, gate, controller))
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
                            AlliancePlugin.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in " + controller.CubeGrid.DisplayName);
                        }
                        else
                        {
                            if (gate.fee > 0 && Distance <= 500)
                            {
                                DoFeeMessage(player, gate, Distance);
                            }
                            else
                            {


                                if (Distance <= 500)
                                {
                                    SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
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
                        //if (AllianceChat.PeopleInAllianceChat.ContainsKey(player.Id.SteamId))
                        //{
                        //    AllianceCommands.SendStatusToClient(true, player.Id.SteamId);
                        //}
                        //else
                        //{
                        //    AllianceCommands.SendStatusToClient(false, player.Id.SteamId);
                        //}
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
        public static List<long> TaxingId = new List<long>();
        public static List<long> OtherTaxingId = new List<long>();
        public void DoTaxStuff()
        {
            List<long> Processed = new List<long>();
            Dictionary<Guid, Dictionary<long, float>> Territory = new Dictionary<Guid, Dictionary<long, float>>();


            Dictionary<Guid, Dictionary<long, float>> taxes = new Dictionary<Guid, Dictionary<long, float>>();


            //this is broken 

            //foreach (TaxItem item in TerritoryTaxes)
            //{
            //    float tax;
            //    Territory ter = Territories[item.territory];
            //    Alliance alliance1 = GetAllianceNoLoading(ter.Alliance);
            //    if (alliance1 != null)
            //    {
            //        if (MySession.Static.Factions.TryGetPlayerFaction(item.playerId) != null)
            //        {

            //            Alliance alliance = GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(item.playerId) as MyFaction);
            //            if (alliance != null)
            //            {
            //                if (alliance.AllianceId == ter.Id)
            //                {
            //                    if (AlliancePlugin.TaxesToBeProcessed.ContainsKey(item.playerId))
            //                    {
            //                        AlliancePlugin.TaxesToBeProcessed[item.playerId] += item.price;
            //                    }
            //                    else
            //                    {
            //                        AlliancePlugin.TaxesToBeProcessed.Add(item.playerId, item.price);
            //                    }
            //                    continue;
            //                }
            //                else
            //                {
            //                    tax = item.price * ter.GetTaxRate(alliance.AllianceId);
            //                    if (AlliancePlugin.TaxesToBeProcessed.ContainsKey(item.playerId))
            //                    {
            //                        AlliancePlugin.TaxesToBeProcessed[item.playerId] += Convert.ToInt64(item.price - tax);
            //                    }
            //                    else
            //                    {
            //                        AlliancePlugin.TaxesToBeProcessed.Add(item.playerId, Convert.ToInt64(item.price - tax));
            //                    }
            //                }
            //            }

            //        }
            //        tax = item.price * ter.TaxPercent;
            //        if (EconUtils.getBalance(item.playerId) >= tax)
            //        {
            //            //add taxes to the dictionary
            //            if (taxes.ContainsKey(ter.Id))
            //            {
            //                Territory[ter.Id].Remove(item.playerId);
            //                taxes[ter.Id].Add(item.playerId, tax);
            //            }
            //            else
            //            {
            //                Dictionary<long, float> temp = new Dictionary<long, float>();
            //                temp.Add(item.playerId, tax);
            //                taxes.Add(ter.Id, temp);
            //            }
            //        }
            //    }
            //}
            ////dont do an else, far too much effort 

            //DatabaseForBank.TerritoryTaxes(taxes);


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
            TerritoryTaxes.Clear();
            foreach (long id in Processed)
            {
                TaxesToBeProcessed.Remove(id);
            }
        }
        public static bool yeeted = false;

        public static bool Paused = false;
        public void DoCaptureSiteStuff()
        {
            if (TorchState != TorchSessionState.Loaded)
            {
                return;
            }
            if (!config.KothEnabled)
            {
                return;
            }
            if (Paused)
            {
                return;
            }
            foreach (CaptureSite config in sites)
            {

                try
                {
                    Location loc = config.GetCurrentLocation();
                    if (loc == null)
                    {
                        continue;
                    }
                    if (!loc.WorldName.Equals(MyMultiplayer.Static.HostName) || !loc.Enabled)
                    {
                        continue;
                    }
                    bool unlocked = false;
                    //if (config.UnlockAtTheseTimes && config.Locked)
                    //{
                    //    if (config.HoursToUnlockAfter.Contains(DateTime.Now.Hour))
                    //    {
                    //        unlocked = true;
                    //        config.Locked = false;
                    //        config.unlockTime = DateTime.Now.AddYears(1);
                    //    }
                    //}
                    //else
                    // {
                    if (DateTime.Now >= config.unlockTime)
                    {
                        unlocked = true;
                        config.unlockTime = DateTime.Now.AddYears(1);
                        config.AllianceOwner = Guid.Empty;
                        config.Locked = false;

                        config.FactionOwner = 0;
                    }
                    //  }
                    if (DateTime.Now >= config.nextCaptureInterval)
                    {
                        config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);

                        if (config.CaptureStarted && DateTime.Now < config.nextCaptureAvailable)
                        {
                            DateTime end = config.nextCaptureAvailable;
                            var diff = end.Subtract(DateTime.Now);
                            string time = String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds);
                            SendChatMessage(loc.Name, "Capture can begin in " + time);
                            SaveCaptureConfig(config.Name, config);
                            continue;
                        }
                        Vector3 position = new Vector3(loc.X, loc.Y, loc.Z);
                        BoundingSphereD sphere = new BoundingSphereD(position, loc.CaptureRadiusInMetre * 2);


                        if (DateTime.Now >= config.nextCaptureAvailable)
                        {
                            config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                            if (unlocked)
                            {
                                if (config.PickNewSiteOnUnlock)
                                {



                                    config.PickNewSiteOnUnlock = false;
                                    Location newloc = config.GetNewCapSite(loc);
                                    if (newloc != null)
                                    {
                                        Log.Error("Failed to change capture site");
                                    }
                                }
                                //do unlock message
                                try
                                {
                                    MyGps gps = new MyGps();
                                    gps.Coords = new Vector3D(loc.X, loc.Y, loc.Z);
                                    gps.Name = loc.Name;
                                    DiscordStuff.SendMessageToDiscord(loc.Name, "Is now unlocked! Ownership reset to nobody. Find it here " + gps.ToString(), config, true);

                                }
                                catch (Exception e)
                                {
                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                    SendChatMessage(loc.Name, "Is now unlocked! Ownership reset to nobody.");
                                }


                                foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                                {


                                    IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                    if (fac != null && !fac.Tag.Equals(loc.KothBuildingOwner) && fac.Tag.Length == 3)
                                    {
                                        Vector3 playerPos = new Vector3();
                                        Boolean yeet = false;
                                        try
                                        {
                                            playerPos = MySession.Static.Players.TryGetPlayerBySteamId(MySession.Static.Players.TryGetSteamId(FacUtils.GetOwner(grid))).GetPosition();
                                        }
                                        catch (Exception)
                                        {

                                            yeet = true;
                                        }

                                        if (playerPos != null)
                                        {
                                            if (Vector3.Distance(playerPos, position) > loc.CaptureRadiusInMetre * 2)
                                            {
                                                yeet = true;
                                            }
                                        }
                                        else
                                        {
                                            yeet = true;
                                        }
                                        if (yeet)
                                        {
                                            if (grid.IsPowered)
                                            {
                                                grid.SwitchPower();
                                            }
                                            foreach (MyBeacon beacon in grid.GetFatBlocks().OfType<MyBeacon>())
                                            {
                                                beacon.Enabled = false;
                                            }
                                            foreach (MyTimerBlock beacon in grid.GetFatBlocks().OfType<MyTimerBlock>())
                                            {
                                                beacon.Enabled = false;
                                            }
                                            foreach (MyProgrammableBlock beacon in grid.GetFatBlocks().OfType<MyProgrammableBlock>())
                                            {
                                                beacon.Enabled = false;
                                            }
                                        }

                                    }
                                }
                            }




                            bool contested = false;
                            Boolean hasActiveCaptureBlock = false;

                            //do time check for unlocks first
                            bool locked = false;

                            //Yeah split the logic, easier to do this than work both into the same method
                            if (config.AllianceSite)
                            {
                                Guid CapturingAlliance = Guid.Empty;
                                if (config.CapturingAlliance != Guid.Empty)
                                {
                                    CapturingAlliance = config.CapturingAlliance;
                                }
                                Boolean CanCapWithSuit = false;
                                if (config.DoSuitCaps)
                                {
                                    foreach (MyPlayer Player in MySession.Static.Players.GetOnlinePlayers())
                                    {

                                        if (CanCapWithSuit)
                                        {
                                            continue;
                                        }
                                        if (Player == null || Player.Character.MarkedForClose)
                                            continue;

                                        long PlayerID = Player.Identity.IdentityId;
                                        if (PlayerID == 0L)
                                            continue;

                                        MyFaction PlayersFaction = MySession.Static.Factions.GetPlayerFaction(PlayerID);
                                        if (PlayersFaction == null)
                                        {
                                            continue;
                                        }
                                        if (Vector3D.Distance(position, Player.GetPosition()) <= loc.CaptureRadiusInMetre * 2)
                                        {


                                            if (PlayersFaction != null)
                                            {
                                                Alliance yeet = AlliancePlugin.GetAllianceNoLoading(PlayersFaction);
                                                if (yeet != null)
                                                {
                                                    if (CapturingAlliance != Guid.Empty)
                                                    {
                                                        CapturingAlliance = yeet.AllianceId;
                                                        CanCapWithSuit = true;
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        if (CapturingAlliance == yeet.AllianceId)
                                                        {
                                                            CanCapWithSuit = true;
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                if (CanCapWithSuit)
                                {
                                    hasActiveCaptureBlock = true;
                                    //    continue;
                                }
                                if (!config.DoSuitCaps)
                                {
                                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                                    {
                                        if (grid.Projector != null)
                                            continue;

                                        if (CanCapWithSuit)
                                        {
                                            hasActiveCaptureBlock = true;
                                            continue;
                                        }

                                        IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                        if (fac != null && !fac.Tag.Equals(loc.KothBuildingOwner) && fac.Tag.Length == 3)
                                        {
                                            //do contested checks
                                            if (GetNationTag(fac) != null)
                                            {
                                                if (CapturingAlliance != Guid.Empty)
                                                {
                                                    if (!CapturingAlliance.Equals(GetNationTag(fac).AllianceId))
                                                    {

                                                        //if its not the same alliance, contest it if they have a capture block
                                                        if (DoesGridHaveCaptureBlock(grid, loc))
                                                        {
                                                            contested = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!hasActiveCaptureBlock)
                                                        {
                                                            if (DoesGridHaveCaptureBlock(grid, loc))
                                                            {
                                                                hasActiveCaptureBlock = true;
                                                                CapturingAlliance = GetNationTag(fac).AllianceId;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (!hasActiveCaptureBlock)
                                                    {
                                                        if (DoesGridHaveCaptureBlock(grid, loc))
                                                        {
                                                            hasActiveCaptureBlock = true;
                                                            CapturingAlliance = GetNationTag(fac).AllianceId;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                contested = true;
                                            }
                                        }
                                    }
                                }

                                if (!contested && !config.CaptureStarted && CapturingAlliance != Guid.Empty)
                                {
                                    if (!CapturingAlliance.Equals(config.AllianceOwner))
                                    {
                                        if (hasActiveCaptureBlock)
                                        {
                                            config.CaptureStarted = true;
                                            config.nextCaptureAvailable = DateTime.Now.AddMinutes(config.MinutesBeforeCaptureStarts);
                                            //  Log.Info("Can cap in 10 minutes");
                                            config.CapturingAlliance = CapturingAlliance;


                                            try
                                            {
                                                DiscordStuff.SendMessageToDiscord(loc.Name, "Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + GetAllianceNoLoading(CapturingAlliance).name, config, true);
                                            }
                                            catch (Exception e)
                                            {
                                                Log.Error("Cant do discord message for koth. " + e.ToString());
                                                SendChatMessage(loc.Name, "Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + GetAllianceNoLoading(CapturingAlliance).name);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!contested && CapturingAlliance != Guid.Empty)
                                    {
                                        //  Log.Info("Got to the capping check and not contested");
                                        if (DateTime.Now >= config.nextCaptureAvailable && config.CaptureStarted)
                                        {

                                            if (config.CapturingAlliance.Equals(CapturingAlliance) && !config.CapturingAlliance.Equals(""))
                                            {

                                                //  Log.Info("Is the same nation as whats capping");
                                                if (!hasActiveCaptureBlock)
                                                {
                                                    // Log.Info("Locking because no active cap block");

                                                    if (config.LockOnFail)
                                                    {
                                                        config.CapturingAlliance = Guid.Empty;
                                                        config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                        //broadcast that its locked
                                                        config.amountCaptured = 0;
                                                        config.CaptureStarted = false;
                                                        config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                        try
                                                        {
                                                            DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Log.Error("Cant do discord message for koth. " + e.ToString());
                                                            SendChatMessage(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            DiscordStuff.SendMessageToDiscord(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.", config);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Log.Error("Cant do discord message for koth. " + e.ToString());
                                                            SendChatMessage(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.");
                                                        }
                                                        config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                                        config.CapturingAlliance = Guid.Empty;
                                                        config.amountCaptured = config.amountCaptured / 2;
                                                        config.CaptureStarted = false;
                                                    }
                                                }
                                                else
                                                {
                                                    config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);

                                                    config.amountCaptured += config.PointsPerCap;

                                                    if (config.amountCaptured >= config.PointsToCap)
                                                    {
                                                        //lock
                                                        //        Log.Info("Locking because points went over the threshold");
                                                        if (config.ChangeCapSiteOnUnlock)
                                                        {
                                                            config.PickNewSiteOnUnlock = true;
                                                        }
                                                        locked = true;
                                                        //  
                                                        config.CapturingAlliance = Guid.Empty;
                                                        config.AllianceOwner = CapturingAlliance;
                                                        config.amountCaptured = 0;
                                                        config.CaptureStarted = false;

                                                        Alliance alliance = GetAllianceNoLoading(config.AllianceOwner);
                                                        config.caplog.AddToCap(alliance.name);
                                                        if (loc.HasTerritory)
                                                        {
                                                            if (config.ChangeLocationAfterTerritoryCap)
                                                            {
                                                                config.PickNewSiteOnUnlock = true;
                                                            }
                                                            config.AddCapProgress(alliance.AllianceId, 1);
                                                            if (config.GetCapProgress(alliance.AllianceId) >= config.CapturesRequiredForTerritory)
                                                            {
                                                                config.nextCaptureAvailable = DateTime.Now.AddHours(config.hoursToLockAfterTerritoryCap);
                                                                config.unlockTime = DateTime.Now.AddHours(config.hoursToLockAfterTerritoryCap);
                                                                config.ClearCapProgress();
                                                                try
                                                                {
                                                                    DiscordStuff.SendMessageToDiscord(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterTerritoryCap + " hours.", config, true);
                                                                }
                                                                catch (Exception e)
                                                                {

                                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                    SendChatMessage(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterTerritoryCap + " hours.");
                                                                }
                                                                if (File.Exists(AlliancePlugin.path + "//Territories//" + loc.LinkedTerritory + ".xml"))
                                                                {
                                                                    Territory ter = utils.ReadFromXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + loc.LinkedTerritory + ".xml");
                                                                    if (ter.HasStation)
                                                                    {
                                                                        ter.previousOwner = ter.Alliance;
                                                                        ter.transferTime = DateTime.Now.AddHours(48);
                                                                        ter.transferTo = alliance.AllianceId;
                                                                        try
                                                                        {
                                                                            DiscordStuff.SendMessageToDiscord(ter.Name, "Waystation will be transferred to " + GetAllianceNoLoading(CapturingAlliance).name, config, true);
                                                                        }
                                                                        catch (Exception ex)
                                                                        {
                                                                            SendChatMessage(loc.Name, "Waystation will be transferred to " + GetAllianceNoLoading(CapturingAlliance).name + " in 48 hours if territory is not recaptured.");

                                                                        }
                                                                    }
                                                                    ter.Alliance = alliance.AllianceId;
                                                                    utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + loc.LinkedTerritory + ".xml", ter);

                                                                    if (Territories.ContainsKey(ter.Id))
                                                                    {
                                                                        Territories[ter.Id] = ter;
                                                                    }
                                                                    else
                                                                    {
                                                                        Territories.Add(ter.Id, ter);
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    Log.Error("TERRITORY FILE DID NOT EXIST.");
                                                                    Territory ter = new Territory();
                                                                    ter.Name = loc.LinkedTerritory;
                                                                    ter.x = loc.X;
                                                                    ter.y = loc.Y;
                                                                    ter.z = loc.Y;
                                                                    ter.Alliance = alliance.AllianceId;

                                                                    utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + loc.LinkedTerritory + ".xml", ter);
                                                                    Territories.Add(ter.Id, ter);

                                                                }
                                                            }
                                                            else
                                                            {
                                                                config.nextCaptureAvailable = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                                config.unlockTime = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                                try
                                                                {
                                                                    DiscordStuff.SendMessageToDiscord(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours. Territory will be captured in " + (config.CapturesRequiredForTerritory - config.GetCapProgress(alliance.AllianceId)) + " more captures", config, true);
                                                                }
                                                                catch (Exception e)
                                                                {

                                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                    SendChatMessage(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours. Territory will be captured in " + (config.CapturesRequiredForTerritory - config.GetCapProgress(alliance.AllianceId)) + " more captures");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            config.nextCaptureAvailable = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                            config.unlockTime = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                            if (config.OnlyDoLootOnceAfterCap)
                                                            {
                                                                LootLocation l = config.GetLootSite();
                                                                if (l != null)
                                                                {
                                                                    DiscordStuff.SendMessageToDiscord(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours. Loot will spawn in " + config.SecondsForOneLootSpawnAfterCap + " seconds.", config);
                                                                    l.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsForOneLootSpawnAfterCap);
                                                                    SendChatMessage(loc.Name, "Loot will spawn in " + config.SecondsForOneLootSpawnAfterCap + " seconds.");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                try
                                                                {
                                                                    DiscordStuff.SendMessageToDiscord(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours.", config, true);
                                                                }
                                                                catch (Exception)
                                                                {

                                                                    Log.Error("Cant do discord message for koth.");
                                                                    SendChatMessage(loc.Name, GetAllianceNoLoading(CapturingAlliance).name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours.");
                                                                }
                                                            }
                                                        }


                                                        foreach (JumpGate gate in AllGates.Values)
                                                        {
                                                            if (gate.LinkedKoth.Equals(loc.Name))
                                                            {
                                                                gate.OwnerAlliance = config.AllianceOwner;
                                                                gate.Save();
                                                            }
                                                        }
                                                        SaveCaptureConfig(config.Name, config);
                                                    }
                                                    else
                                                    {
                                                        //       if (DateTime.Now >= config.nextBroadcast)
                                                        //        {


                                                        try
                                                        {
                                                            DiscordStuff.SendMessageToDiscord(loc.Name, config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAlliance(CapturingAlliance).name, config);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Log.Error("Cant do discord message for koth. " + e.ToString());
                                                            SendChatMessage(loc.Name, config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAlliance(CapturingAlliance).name);
                                                        }
                                                        SaveCaptureConfig(config.Name, config);
                                                        continue;
                                                        //         config.nextBroadcast = DateTime.Now.AddMinutes(config.MinsPerCaptureBroadcast);
                                                        //  }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (config.LockOnFail)
                                                {
                                                    //  Log.Info("Locking because the capturing nation changed");
                                                    config.CapturingAlliance = Guid.Empty;
                                                    config.CaptureStarted = false;
                                                    config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                    config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                    //broadcast that its locked


                                                    config.amountCaptured = 0;
                                                    try
                                                    {

                                                        DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("Cant do discord message for koth. " + e.ToString());
                                                        SendChatMessage(loc.Name, "Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                    }
                                                }
                                                else
                                                {
                                                    config.CapturingAlliance = Guid.Empty;
                                                    config.CaptureStarted = false;
                                                    config.amountCaptured = config.amountCaptured / 2;
                                                    try
                                                    {

                                                        DiscordStuff.SendMessageToDiscord(loc.Name, "Capturing alliance has changed. Resetting capture site and halfing progress.", config);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("Cant do discord message for koth. " + e.ToString());
                                                        SendChatMessage(loc.Name, " Capturing alliance has changed. Resetting capture site and halfing progress.");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {

                                            SendChatMessage(loc.Name, "Waiting to cap");
                                            //    Log.Info("Waiting to cap");
                                        }
                                    }
                                    else
                                    {
                                        if (!hasActiveCaptureBlock && config.CaptureStarted)
                                        {
                                            // Log.Info("Locking because no active cap block");
                                            if (config.LockOnFail)
                                            {
                                                config.CapturingAlliance = Guid.Empty;
                                                config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                //broadcast that its locked
                                                config.amountCaptured = 0;
                                                config.CaptureStarted = false;
                                                config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                try
                                                {
                                                    DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                    SendChatMessage(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    DiscordStuff.SendMessageToDiscord(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.", config);
                                                }
                                                catch (Exception)
                                                {
                                                    Log.Error("Cant do discord message for koth.");
                                                    SendChatMessage(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.");
                                                }
                                                config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                                config.CapturingAlliance = Guid.Empty;
                                                config.amountCaptured = config.amountCaptured / 2;
                                                config.CaptureStarted = false;
                                            }
                                        }
                                        if (contested && config.CaptureStarted)
                                        {
                                            //   Log.Info("Its contested or the fuckers trying to cap have no nation");
                                            //send contested message
                                            //  SendChatMessage("Contested");
                                            try
                                            {
                                                DiscordStuff.SendMessageToDiscord(loc.Name, "Capture point contested!", config);
                                            }
                                            catch (Exception e)
                                            {
                                                SendChatMessage(loc.Name, "Capture point contested!");
                                                Log.Error("Cant do discord message for koth. " + e.ToString());
                                            }
                                        }
                                        else
                                        {
                                            if (contested && !config.CaptureStarted)
                                            {
                                                SendChatMessage(loc.Name, " Capture point contested, Capture cannot begin!");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    //  Log.Info("faction cap site");
                                    long CapturingFaction = 0;
                                    MyFaction capture = null;
                                    if (config.CapturingFaction > 0)
                                    {
                                        // Log.Info("found a faction");
                                        CapturingFaction = config.CapturingFaction;
                                        capture = MySession.Static.Factions.TryGetFactionById(config.CapturingFaction) as MyFaction;
                                    }
                                    Boolean CanCapWithSuit = false;
                                    if (config.DoSuitCaps)
                                    {
                                        foreach (MyPlayer Player in MySession.Static.Players.GetOnlinePlayers())
                                        {

                                            if (CanCapWithSuit)
                                            {
                                                continue;
                                            }
                                            if (Player == null || Player.Character.MarkedForClose)
                                                continue;

                                            long PlayerID = Player.Identity.IdentityId;
                                            if (PlayerID == 0L)
                                                continue;

                                            MyFaction PlayersFaction = MySession.Static.Factions.GetPlayerFaction(PlayerID);
                                            if (PlayersFaction == null)
                                            {
                                                continue;
                                            }
                                            if (Vector3D.Distance(position, Player.GetPosition()) <= loc.CaptureRadiusInMetre * 2)
                                            {


                                                if (PlayersFaction != null)
                                                {

                                                    if (yeet != null)
                                                    {
                                                        if (CapturingFaction == 0)
                                                        {
                                                            CapturingFaction = PlayersFaction.FactionId;
                                                            CanCapWithSuit = true;
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            if (!MySession.Static.Factions.AreFactionsEnemies(CapturingFaction, PlayersFaction.FactionId))
                                                            {
                                                                CanCapWithSuit = true;
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                contested = true;
                                                                CanCapWithSuit = false;
                                                                continue;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (CanCapWithSuit)
                                    {
                                        hasActiveCaptureBlock = true;
                                        //    continue;
                                    }
                                    if (!config.DoSuitCaps)
                                    {
                                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                                        {
                                            if (grid.Projector != null)
                                                continue;




                                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                            if (fac != null && !fac.Tag.Equals(loc.KothBuildingOwner) && fac.Tag.Length == 3)
                                            {

                                                //do contested checks
                                                if (CapturingFaction > 0)
                                                {
                                                    //  Log.Info("check if contested");
                                                    if (!CapturingFaction.Equals(fac.FactionId) && MySession.Static.Factions.AreFactionsEnemies(CapturingFaction, fac.FactionId))
                                                    {
                                                        //   Log.Info("not the capping fac, and enemies");
                                                        //if its not the same alliance, contest it if they have a capture block
                                                        if (DoesGridHaveCaptureBlock(grid, loc))
                                                        {
                                                            //     Log.Info("contested and has cap block");
                                                            contested = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Log.Info("friends");
                                                        if (!hasActiveCaptureBlock)
                                                        {
                                                            if (DoesGridHaveCaptureBlock(grid, loc))
                                                            {
                                                                // Log.Info("has cap block");
                                                                hasActiveCaptureBlock = true;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // Log.Info("grab the first faction");
                                                    if (!hasActiveCaptureBlock)
                                                    {
                                                        //   Log.Info("doesnt have a cap block, checking");
                                                        if (DoesGridHaveCaptureBlock(grid, loc))
                                                        {
                                                            //     Log.Info("has cap block");
                                                            hasActiveCaptureBlock = true;
                                                            CapturingFaction = fac.FactionId;
                                                            capture = fac as MyFaction;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }


                                    if (!contested && !config.CaptureStarted && CapturingFaction > 0)
                                    {
                                        if (!CapturingFaction.Equals(config.FactionOwner))
                                        {
                                            if (hasActiveCaptureBlock)
                                            {
                                                config.CaptureStarted = true;
                                                config.nextCaptureAvailable = DateTime.Now.AddMinutes(config.MinutesBeforeCaptureStarts);
                                                //  Log.Info("Can cap in 10 minutes");
                                                config.CapturingFaction = CapturingFaction;
                                                SaveCaptureConfig(config.Name, config);
                                                //    Log.Info(CapturingFaction);
                                                //    Log.Info(config.CapturingFaction);
                                                try
                                                {
                                                    DiscordStuff.SendMessageToDiscord(loc.Name, "Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + capture.Name, config);
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                    SendChatMessage(loc.Name, "Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + capture.Name);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!contested && CapturingFaction > 0)
                                        {
                                            //  Log.Info("Got to the capping check and not contested");
                                            if (DateTime.Now >= config.nextCaptureAvailable && config.CaptureStarted)
                                            {

                                                if (config.CapturingFaction == CapturingFaction)
                                                {

                                                    //  Log.Info("Is the same nation as whats capping");
                                                    if (!hasActiveCaptureBlock)
                                                    {
                                                        // Log.Info("Locking because no active cap block");
                                                        if (config.LockOnFail)
                                                        {
                                                            config.CapturingFaction = 0;
                                                            config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                            //broadcast that its locked
                                                            config.amountCaptured = 0;
                                                            config.CaptureStarted = false;

                                                            config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                            try
                                                            {
                                                                DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                SendChatMessage(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            try
                                                            {
                                                                DiscordStuff.SendMessageToDiscord(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.", config);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                SendChatMessage(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.");
                                                            }
                                                            config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                                            config.CapturingFaction = 0;
                                                            config.amountCaptured = config.amountCaptured / 2;
                                                            config.CaptureStarted = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);

                                                        config.amountCaptured += config.PointsPerCap;

                                                        if (config.amountCaptured >= config.PointsToCap)
                                                        {
                                                            //lock
                                                            //        Log.Info("Locking because points went over the threshold");

                                                            locked = true;
                                                            //  
                                                            config.CapturingFaction = 0;
                                                            config.FactionOwner = CapturingFaction;
                                                            config.amountCaptured = 0;
                                                            config.CaptureStarted = false;
                                                            config.unlockTime = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                            config.caplog.AddToCap(capture.Name);
                                                            config.nextCaptureAvailable = DateTime.Now.AddHours(config.hoursToLockAfterNormalCap);
                                                            if (config.ChangeCapSiteOnUnlock)
                                                            {
                                                                config.PickNewSiteOnUnlock = true;
                                                            }
                                                            if (config.OnlyDoLootOnceAfterCap)
                                                            {
                                                                LootLocation l = config.GetLootSite();
                                                                if (l != null)
                                                                {
                                                                    try
                                                                    {
                                                                        DiscordStuff.SendMessageToDiscord(loc.Name, capture.Name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours. Loot will spawn in " + config.SecondsForOneLootSpawnAfterCap + " seconds.", config);
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        SendChatMessage(loc.Name, capture.Name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours. Loot will spawn in " + config.SecondsForOneLootSpawnAfterCap + " seconds.");
                                                                        Log.Error("Cant do discord message for koth. " + ex.ToString());
                                                                    }
                                                                    l.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsForOneLootSpawnAfterCap);

                                                                }
                                                            }
                                                            else
                                                            {
                                                                try
                                                                {
                                                                    DiscordStuff.SendMessageToDiscord(loc.Name, capture.Name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours.", config);
                                                                }
                                                                catch (Exception e)
                                                                {
                                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                    SendChatMessage(loc.Name, capture.Name + " has captured " + loc.Name + ". It is now locked for " + config.hoursToLockAfterNormalCap + " hours.");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //       if (DateTime.Now >= config.nextBroadcast)
                                                            //        {


                                                            try
                                                            {
                                                                DiscordStuff.SendMessageToDiscord(loc.Name, config.amountCaptured + " out of " + config.PointsToCap + " by " + capture.Tag, config);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Log.Error("Cant do discord message for koth. " + e.ToString());
                                                                SendChatMessage(loc.Name, " " + config.amountCaptured + " out of " + config.PointsToCap + " by " + capture.Tag);
                                                            }
                                                            SaveCaptureConfig(config.Name, config);
                                                            continue;
                                                            //         config.nextBroadcast = DateTime.Now.AddMinutes(config.MinsPerCaptureBroadcast);
                                                            //  }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //  Log.Info("Locking because the capturing nation changed");
                                                    if (config.LockOnFail)
                                                    {
                                                        config.CapturingFaction = 0;
                                                        config.CaptureStarted = false;
                                                        config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                        config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                        //broadcast that its locked

                                                        config.amountCaptured = 0;
                                                        try
                                                        {

                                                            DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capturing faction has changed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                        }
                                                        catch (Exception)
                                                        {
                                                            Log.Error("Cant do discord message for koth.");
                                                            SendChatMessage(loc.Name, "Locked, Capturing faction has changed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        config.CapturingFaction = 0;
                                                        config.CaptureStarted = false;
                                                        config.amountCaptured = config.amountCaptured / 2;
                                                        try
                                                        {

                                                            DiscordStuff.SendMessageToDiscord(loc.Name, "Capturing faction has changed. Resetting capture site.", config);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Log.Error("Cant do discord message for koth. " + e.ToString());
                                                            SendChatMessage(loc.Name, "Capturing faction has changed. Resetting capture site.");
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {

                                                SendChatMessage(loc.Name, "Waiting to cap");
                                                //    Log.Info("Waiting to cap");
                                            }
                                        }
                                        else
                                        {
                                            if (!hasActiveCaptureBlock && config.CaptureStarted)
                                            {
                                                // Log.Info("Locking because no active cap block");
                                                if (config.LockOnFail)
                                                {
                                                    config.CapturingFaction = 0;
                                                    config.nextCaptureAvailable = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                    //broadcast that its locked
                                                    config.amountCaptured = 0;
                                                    config.CaptureStarted = false;
                                                    config.unlockTime = DateTime.Now.AddHours(config.hourCooldownAfterFail);
                                                    try
                                                    {
                                                        DiscordStuff.SendMessageToDiscord(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("Cant do discord message for koth. " + e.ToString());
                                                        SendChatMessage(loc.Name, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        DiscordStuff.SendMessageToDiscord(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.", config);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("Cant do discord message for koth. " + e.ToString());
                                                        SendChatMessage(loc.Name, "Capture blocks are missing or destroyed. Resetting site capture.");
                                                    }
                                                    config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                                    config.CapturingFaction = 0;
                                                    config.amountCaptured = config.amountCaptured / 2;
                                                    config.CaptureStarted = false;
                                                }
                                            }
                                            if (contested && config.CaptureStarted)
                                            {
                                                //   Log.Info("Its contested or the fuckers trying to cap have no nation");
                                                //send contested message
                                                //  SendChatMessage("Contested");
                                                try
                                                {
                                                    DiscordStuff.SendMessageToDiscord(loc.Name, "Capture point contested!", config);
                                                }
                                                catch (Exception e)
                                                {
                                                    SendChatMessage(loc.Name, "Capture point contested!");
                                                    Log.Error("Cant do discord message for koth. " + e.ToString());
                                                }
                                            }
                                            else
                                            {
                                                if (contested && !config.CaptureStarted)
                                                {
                                                    SendChatMessage(loc.Name, " Capture point contested, Capture cannot begin!");
                                                }
                                            }
                                        }
                                    }


                                }
                                catch (Exception ex)
                                {
                                    Log.Error("Faction koth error for " + loc.Name + " " + ex.ToString());
                                    SaveCaptureConfig(config.Name, config);
                                    continue;
                                }

                            }

                            if (!locked)
                            {
                                config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                            }
                            SaveCaptureConfig(config.Name, config);
                        }
                        //  else
                        // {
                        //if (config.CaptureStarted)
                        //{
                        //    DateTime end = config.nextCaptureAvailable;
                        //    var diff = end.Subtract(DateTime.Now);
                        //    string time = String.Format("{0} Hours {1} Minutes {2} Seconds", diff.Hours, diff.Minutes, diff.Seconds);
                        //    SendChatMessage(config.Name, "Capture can begin in " + time);
                        //}
                        // }

                    }
                    LootLocation loot = config.GetLootSite();
                    if (loot == null)
                    {
                        continue;
                    }

                    if (DateTime.Now >= loot.nextCoreSpawn)
                    {
                        Vector3 position = new Vector3(loc.X, loc.Y, loc.Z);
                        BoundingSphereD sphere = new BoundingSphereD(position, loc.CaptureRadiusInMetre * 2);
                        loot.nextCoreSpawn = DateTime.Now.AddSeconds(loot.SecondsBetweenCoreSpawn);
                        if (config.OnlyDoLootOnceAfterCap)
                        {

                            loot.nextCoreSpawn = DateTime.Now.AddYears(5);

                            try
                            {
                                DiscordStuff.SendMessageToDiscord(loc.Name, "Doing one time loot spawn.", config);
                            }
                            catch (Exception e)
                            {

                                Log.Error("Cant do discord message for koth. " + e.ToString());
                            }

                        }
                        SaveCaptureConfig(config.Name, config);
                        Vector3 lootGrid = new Vector3(loot.X, loot.Y, loot.Z);
                        MyCubeGrid lootgrid = GetLootboxGrid(lootGrid, loot);
                        //spawn the cores
                        Boolean hasCap = false;
                        Boolean hasCapNotOwner = false;
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null)
                            {
                                if (fac.Tag == loot.KothBuildingOwner || fac.Tag.Length > 3)
                                {
                                    continue;
                                }
                            }
                            if (fac != null)
                            {
                                if (GetNationTag(fac) != null && GetNationTag(fac).AllianceId.Equals(config.AllianceOwner))
                                {
                                    if (!hasCap)
                                    {

                                        hasCap = DoesGridHaveCaptureBlock(grid, loc);
                                    }
                                }
                                else
                                {
                                    if (!hasCapNotOwner)
                                    {

                                        hasCapNotOwner = DoesGridHaveCaptureBlock(grid, loc);
                                    }
                                }

                            }

                        }

                        if (config.AllianceSite)
                        {
                            if (config.AllianceOwner != Guid.Empty)
                            {
                                Alliance alliance = GetAlliance(config.AllianceOwner);
                                if (loc.RequireCaptureBlockForLootGen)
                                {
                                    if (!hasCap)
                                    {

                                        if (hasCapNotOwner)
                                        {
                                            if (lootgrid != null)
                                            {

                                                SpawnCores(lootgrid, loot, loc, alliance);
                                                SendChatMessage(loc.Name, "Spawning loot!");

                                                //   SaveCaptureConfig(config.Name, config);


                                            }
                                            continue;
                                        }

                                        SendChatMessage(loc.Name, "No loot spawn, No functional capture block");

                                        continue;

                                    }
                                    else
                                    {
                                        if (lootgrid != null)
                                        {

                                            SpawnCores(lootgrid, loot, loc, alliance);
                                            SendChatMessage(loc.Name, "Spawning loot!");

                                            // SaveCaptureConfig(config.Name, config);


                                        }
                                        continue;
                                    }
                                }
                                else
                                {
                                    //  Log.Info("No block");
                                    if (lootgrid != null)
                                    {
                                        SpawnCores(lootgrid, loot, loc, alliance);

                                    }
                                    SendChatMessage(loc.Name, "Spawning loot!");


                                    //  SaveCaptureConfig(config.Name, config);
                                }
                            }
                        }
                        else
                        {
                            if (config.FactionOwner > 0)
                            {
                                MyFaction capture = null;

                                capture = MySession.Static.Factions.TryGetFactionById(config.FactionOwner) as MyFaction;
                                if (capture == null)
                                {
                                    SaveCaptureConfig(config.Name, config);
                                    continue;
                                }
                                if (loc.RequireCaptureBlockForLootGen)
                                {
                                    if (!hasCap)
                                    {

                                        if (hasCapNotOwner)
                                        {
                                            if (lootgrid != null)
                                            {

                                                SpawnCores(lootgrid, loot, loc, capture);
                                                SendChatMessage(loc.Name, "Spawning loot!");




                                            }
                                            continue;
                                        }

                                        SendChatMessage(loc.Name, "No loot spawn, No functional capture block");

                                        continue;

                                    }
                                    else
                                    {
                                        if (lootgrid != null)
                                        {

                                            SpawnCores(lootgrid, loot, loc, capture);
                                            SendChatMessage(loc.Name, "Spawning loot!");




                                        }
                                        continue;
                                    }
                                }
                                else
                                {
                                    //  Log.Info("No block");
                                    if (lootgrid != null)
                                    {
                                        SpawnCores(lootgrid, loot, loc, capture);

                                    }
                                    SendChatMessage(loc.Name, "Spawning loot!");



                                }
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    Log.Error("New Capture Site Error " + e.ToString());
                }

            }
        }

        public static List<CaptureSite> sites = new List<CaptureSite>();
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
                    BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre * 2);
                    if (DateTime.Now >= config.unlockTime)
                    {
                        config.unlockTime = DateTime.Now.AddYears(1);
                        config.owner = Guid.Empty;
                        SaveKothConfig(config.KothName, config);
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {

                            if (grid.Projector != null)
                                continue;

                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null && !fac.Tag.Equals(config.KothBuildingOwner) && fac.Tag.Length == 3)
                            {
                                Vector3 playerPos = new Vector3();
                                try
                                {
                                    playerPos = MySession.Static.Players.TryGetPlayerBySteamId(MySession.Static.Players.TryGetSteamId(FacUtils.GetOwner(grid))).GetPosition();
                                }
                                catch (Exception)
                                {


                                }
                                Boolean yeet = false;
                                if (playerPos != null)
                                {
                                    if (Vector3.Distance(playerPos, position) > config.CaptureRadiusInMetre * 2)
                                    {
                                        yeet = true;
                                    }
                                }
                                else
                                {
                                    yeet = true;
                                }
                                if (yeet)
                                {


                                    foreach (MyBeacon beacon in grid.GetFatBlocks().OfType<MyBeacon>())
                                    {
                                        beacon.Enabled = false;

                                    }
                                    foreach (MyTimerBlock beacon in grid.GetFatBlocks().OfType<MyTimerBlock>())
                                    {
                                        beacon.Enabled = false;
                                    }
                                    foreach (MyProgrammableBlock beacon in grid.GetFatBlocks().OfType<MyProgrammableBlock>())
                                    {
                                        beacon.Enabled = false;
                                    }
                                }

                            }
                        }
                        try
                        {
                            DiscordStuff.SendMessageToDiscord(config.KothName + " Is now unlocked! Ownership reset to nobody", config);

                        }
                        catch (Exception)
                        {
                            Log.Error("Cant do discord message for koth.");
                            SendChatMessage(config.KothName, "Is now unlocked! Ownership reset to nobody");
                        }

                    }

                    if (DateTime.Now >= config.nextCaptureInterval)
                    {
                        config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);

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
                                    if (DoesGridHaveCaptureBlock(grid, config))
                                    {
                                        contested = true;
                                    }
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
                                // contested = false;
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


                                        try
                                        {
                                            DiscordStuff.SendMessageToDiscord(config.KothName + " Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + GetAllianceNoLoading(capturingNation).name, config);
                                        }
                                        catch (Exception)
                                        {
                                            Log.Error("Cant do discord message for koth.");
                                            SendChatMessage(config.KothName, "Capture can begin in " + config.MinutesBeforeCaptureStarts + " minutes. By " + GetAllianceNoLoading(capturingNation).name);
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
                                                    SendChatMessage(config.KothName, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
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
                                                    if (config.HasTerritory)
                                                    {
                                                        if (File.Exists(AlliancePlugin.path + "//Territories//" + config.LinkedTerritory + ".xml"))
                                                        {
                                                            Territory ter = utils.ReadFromXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + config.LinkedTerritory + ".xml");
                                                            if (ter.HasStation)
                                                            {
                                                                ter.previousOwner = ter.Alliance;
                                                                ter.transferTime = DateTime.Now.AddHours(48);
                                                                ter.transferTo = alliance.AllianceId;
                                                            }
                                                            ter.Alliance = alliance.AllianceId;
                                                            utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + config.LinkedTerritory + ".xml", ter);

                                                            if (Territories.ContainsKey(ter.Id))
                                                            {
                                                                Territories[ter.Id] = ter;
                                                            }
                                                            else
                                                            {
                                                                Territories.Add(ter.Id, ter);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Territory ter = new Territory();
                                                            ter.Name = config.LinkedTerritory;
                                                            ter.x = config.x;
                                                            ter.y = config.y;
                                                            ter.z = config.z;
                                                            ter.Alliance = alliance.AllianceId;

                                                            utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + config.LinkedTerritory + ".xml", ter);
                                                            Territories.Add(ter.Id, ter);
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
                                                        SendChatMessage(config.KothName, GetAllianceNoLoading(capturingNation).name + " has captured " + config.KothName + ". It is now locked for " + config.hoursToLockAfterCap + " hours.");
                                                    }
                                                }
                                                else
                                                {
                                                    //       if (DateTime.Now >= config.nextBroadcast)
                                                    //        {


                                                    try
                                                    {
                                                        DiscordStuff.SendMessageToDiscord(config.KothName + " capture progress " + config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAlliance(capturingNation).name, config);
                                                    }
                                                    catch (Exception)
                                                    {
                                                        Log.Error("Cant do discord message for koth.");
                                                        SendChatMessage(config.KothName, "capture progress " + config.amountCaptured + " out of " + config.PointsToCap + " by " + GetAlliance(capturingNation).name);
                                                    }
                                                    SaveKothConfig(config.KothName, config);
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
                                            SendChatMessage(config.KothName, "Locked because capturing nation has changed.");
                                            config.amountCaptured = 0;
                                            try
                                            {

                                                DiscordStuff.SendMessageToDiscord(config.KothName + " Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours", config);
                                            }
                                            catch (Exception)
                                            {
                                                Log.Error("Cant do discord message for koth.");
                                                SendChatMessage(config.KothName, "Locked, Capturing alliance has changed. Locked for " + config.hourCooldownAfterFail + " hours");
                                            }
                                        }
                                    }
                                    else
                                    {

                                        SendChatMessage(config.KothName, "Waiting to cap");
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
                                            SendChatMessage(config.KothName, "Locked, Capture blocks are missing or destroyed. Locked for " + config.hourCooldownAfterFail + " hours");
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
                                            SendChatMessage(config.KothName, "Capture point contested!");
                                            Log.Error("Cant do discord message for koth.");
                                        }
                                    }
                                    else
                                    {
                                        if (contested && !config.CaptureStarted)
                                        {
                                            SendChatMessage(config.KothName, " Capture point contested, Capture cannot begin!");
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
                                SendChatMessage(config.KothName, "Capture can begin in " + time);
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
                                SendChatMessage(config.KothName, "Denied point, no core spawn");
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
                                            SendChatMessage(config.KothName, "Spawning loot!");
                                            Alliance alliance = GetAlliance(config.owner);
                                            if (alliance != null)
                                            {
                                                alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                                if (config.SpaceMoneyReward > 0)
                                                {
                                                    DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                                    alliance.DepositKOTH(config.SpaceMoneyReward, 1);
                                                }
                                                SaveAllianceData(alliance);
                                                SaveKothConfig(config.KothName, config);
                                            }

                                        }
                                        return;
                                    }

                                    SendChatMessage(config.KothName, "No loot spawn, No functional capture block");

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
                                SendChatMessage(config.KothName, "Spawning loot!");
                                Alliance alliance = GetAlliance(config.owner);
                                if (alliance != null)
                                {
                                    alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                    if (config.SpaceMoneyReward > 0)
                                    {
                                        DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                        alliance.DepositKOTH(config.SpaceMoneyReward, 1);
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
                                SendChatMessage(config.KothName, "Spawning loot!");
                                config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                                Alliance alliance = GetAlliance(config.owner);
                                if (alliance != null)
                                {
                                    alliance.CurrentMetaPoints += config.MetaPointsPerCapWithBonus;
                                    if (config.SpaceMoneyReward > 0)
                                    {
                                        DatabaseForBank.AddToBalance(alliance, config.SpaceMoneyReward);
                                        alliance.DepositKOTH(config.SpaceMoneyReward, 1);
                                    }
                                    SaveAllianceData(alliance);
                                }
                                SaveKothConfig(config.KothName, config);
                            }
                        }
                        else
                        {
                            config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
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
        public static Dictionary<ulong, String> InCapRadius = new Dictionary<ulong, String>();
        public static Dictionary<ulong, Guid> TerritoryInside = new Dictionary<ulong, Guid>();
        public static List<JumpThing> jumpies = new List<JumpThing>();
        public static Dictionary<Guid, Territory> Territories = new Dictionary<Guid, Territory>();
        public static Dictionary<long, DateTime> InTerritory = new Dictionary<long, DateTime>();
        public static List<ulong> InSafeZone = new List<ulong>();
        public static void SendEnterMessage(MyPlayer player, Territory ter)
        {

            Alliance alliance = null;

            alliance = AlliancePlugin.GetAllianceNoLoading(ter.Alliance);


            NotificationMessage message2 = new NotificationMessage();


            if (InTerritory.ContainsKey(player.Identity.IdentityId))
            {

                if (DateTime.Now < InTerritory[player.Identity.IdentityId])
                    return;
                if (ter.HasBigSafeZone)
                {
                    float distance = Vector3.Distance(player.GetPosition(), new Vector3(ter.stationX, ter.stationY, ter.stationZ));
                    if (distance <= ter.SafeZoneRadiusFromStationCoords)
                    {
                        if (ter.ZoneIsEnabled)
                        {
                            ShipyardCommands.SendMessage(ter.MessagePrefix, "Safezone is enabled, combat is disabled.", Color.LimeGreen, (long)player.Id.SteamId);
                            InSafeZone.Remove(player.Id.SteamId);
                            InSafeZone.Add(player.Id.SteamId);
                            return;
                        }
                        else
                        {
                            ShipyardCommands.SendMessage(ter.MessagePrefix, "Safezone is NOT enabled.", Color.DarkRed, (long)player.Id.SteamId);
                            InSafeZone.Remove(player.Id.SteamId);
                        }
                    }
                }
                message2 = new NotificationMessage(ter.EntryMessage.Replace("{name}", ter.Name), 10000, "Green");
                //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible


                if (!TerritoryInside.ContainsKey(player.Id.SteamId))
                {
                    TerritoryInside.Add(player.Id.SteamId, ter.Id);
                }
                else
                {
                    TerritoryInside[player.Id.SteamId] = ter.Id;
                }
                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                InTerritory[player.Identity.IdentityId] = DateTime.Now.AddMinutes(10);
                if (alliance != null)
                {
                    NotificationMessage message3 = new NotificationMessage(ter.ControlledMessage.Replace("{alliance}", alliance.name), 5000, "Red");
                    ModCommunication.SendMessageTo(message3, player.Id.SteamId);

                }
                return;
            }
            else
            {
                message2 = new NotificationMessage(ter.EntryMessage.Replace("{name}", ter.Name), 5000, "Green");
                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                InTerritory.Add(player.Identity.IdentityId, DateTime.Now.AddMinutes(10));
                if (!TerritoryInside.ContainsKey(player.Id.SteamId))
                {
                    TerritoryInside.Add(player.Id.SteamId, ter.Id);
                }
                else
                {
                    TerritoryInside[player.Id.SteamId] = ter.Id;
                }
                if (alliance != null)
                {
                    NotificationMessage message3 = new NotificationMessage(ter.ControlledMessage.Replace("{alliance}", alliance.name), 5000, "Red");
                    ModCommunication.SendMessageTo(message3, player.Id.SteamId);
                }
                return;
            }
        }

        public static void SendLeaveMessage(MyPlayer player, Territory ter)
        {
            if (TerritoryInside.TryGetValue(player.Id.SteamId, out Guid ter2))
            {
                if (!ter.Id.Equals(ter2))
                {
                    return;
                }
            }
            else
            {
                InTerritory.Remove(player.Identity.IdentityId);
                TerritoryInside.Remove(player.Id.SteamId);
                return;
            }
            if (ter.HasBigSafeZone && InSafeZone.Contains(player.Id.SteamId))
            {
                float distance = Vector3.Distance(player.GetPosition(), new Vector3(ter.stationX, ter.stationY, ter.stationZ));
                if (distance > ter.SafeZoneRadiusFromStationCoords)
                {

                    ShipyardCommands.SendMessage(ter.MessagePrefix, "You have left the safezone, combat is enabled.", Color.DarkRed, (long)player.Id.SteamId);
                    InSafeZone.Remove(player.Id.SteamId);
                    return;
                }
            }
            NotificationMessage message2 = new NotificationMessage(ter.ExitMessage.Replace("{name}", ter.Name), 10000, "White");

            ModCommunication.SendMessageTo(message2, player.Id.SteamId);
            InTerritory.Remove(player.Identity.IdentityId);
            TerritoryInside.Remove(player.Id.SteamId);
        }


        public static DateTime chat = DateTime.Now;

        public static Dictionary<ulong, DateTime> UpdateThese = new Dictionary<ulong, DateTime>();
        public static DateTime RegisterMainBot = DateTime.Now;
        public static Dictionary<ulong, Boolean> statusUpdate = new Dictionary<ulong, bool>();
        public static Dictionary<ulong, Guid> otherAllianceShit = new Dictionary<ulong, Guid>();
        public override void Update()
        {

            if (ticks % 512 == 0)
            {
                Dictionary<ulong, DateTime> YEET = new Dictionary<ulong, DateTime>();
                List<ulong> oof = new List<ulong>();
                List<ulong> OtherYeet = new List<ulong>();
                foreach (KeyValuePair<ulong, DateTime> pair in UpdateThese)
                {
                    if (DateTime.Now >= pair.Value)
                    {
                        oof.Add(pair.Key);
                        if (!YEET.ContainsKey(pair.Key))
                        {
                            YEET.Add(pair.Key, DateTime.Now.AddMinutes(1));

                        }
                        if (statusUpdate.TryGetValue(pair.Key, out Boolean status))
                        {
                            AlliancePlugin.SendChatMessage("AllianceChatStatus", "true", pair.Key);
                            statusUpdate.Remove(pair.Key);
                        }
                        if (otherAllianceShit.TryGetValue(pair.Key, out Guid allianceId))
                        {
                            Alliance alliance = GetAlliance(allianceId);
                            if (alliance != null)
                            {
                                AlliancePlugin.SendChatMessage("AllianceColorConfig", alliance.r + " " + alliance.g + " " + alliance.b, pair.Key);
                                AlliancePlugin.SendChatMessage("AllianceTitleConfig", alliance.GetTitle(pair.Key) + " ", pair.Key);
                                otherAllianceShit.Remove(pair.Key);
                            }
                        }
                        if (AllianceChat.PeopleInAllianceChat.ContainsKey(pair.Key))
                        {
                            AllianceCommands.SendStatusToClient(true, pair.Key);
                        }
                        else
                        {
                            AllianceCommands.SendStatusToClient(false, pair.Key);
                        }

                    }
                }
                foreach (ulong id in oof)
                {
                    if (UpdateThese.TryGetValue(id, out DateTime time))
                    {
                        UpdateThese[id] = time.AddSeconds(5);
                    }
                }
                oof.Clear();
                foreach (KeyValuePair<ulong, DateTime> pair in YEET)
                {
                    if (DateTime.Now > pair.Value)
                    {
                        OtherYeet.Add(pair.Key);
                        UpdateThese.Remove(pair.Key);
                    }
                }
                foreach (ulong id in OtherYeet)
                {
                    YEET.Remove(id);
                }
                OtherYeet.Clear();
            }
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
                if (config.AllowDiscord)
                {
                    Dictionary<Guid, DateTime> temp = new Dictionary<Guid, DateTime>();
                    foreach (KeyValuePair<Guid, DateTime> keys in registerThese)
                    {
                        if (DateTime.Now > keys.Value)
                        {
                            Alliance alliance = GetAlliance(keys.Key);
                            if (alliance != null)
                            {

                                DiscordStuff.RegisterAllianceBot(alliance, alliance.DiscordChannelId);

                                temp.Add(alliance.AllianceId, DateTime.Now.AddMinutes(10));
                                Log.Info("Connecting bot.");
                            }

                        }
                    }
                    foreach (KeyValuePair<Guid, DateTime> keys in temp)
                    {
                        registerThese[keys.Key] = keys.Value;
                    }
                }
            }
            try
            {
                if (config.KothEnabled)
                {
                    DoCaptureSiteStuff();
                    DoKothStuff();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            if (DateTime.Now > chat)
            {
                try
                {
                    foreach (ulong id in AllianceChat.PeopleInAllianceChat.Keys)
                    {
                        ShipyardCommands.SendMessage("Alliance chat", "You are in alliance chat, to leave use !alliance chat", Color.Green, (long)id);
                    }
                }
                catch (Exception)
                {
                }
                chat = chat.AddMinutes(10);
            }

            if (TorchState == TorchSessionState.Loaded)
            {
                //Log.Info("Doing alliance tasks");

                //     DateTime now = DateTime.Now;
                //   if (config != null && config.AllowDiscord && !DiscordStuff.Ready && now >= RegisterMainBot)
                //    {
                //         DiscordStuff.RegisterDiscord();
                //    }

                if (DateTime.Now > NextUpdate)
                {
                    NextUpdate = DateTime.Now.AddSeconds(60);
                    try
                    {
                        OrganisePlayers();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);

                    }
                    try
                    {
                        LoadAllCaptureSites();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }

                }

                if (DateTime.Now > NextUpdate.AddSeconds(5))
                {
                    try
                    {
                        LoadAllAlliances();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                if (DateTime.Now > NextUpdate.AddSeconds(10))
                {
                    try
                    {

                        LoadAllRefineryUpgrades();
                    }
                    catch (Exception ex)
                    {

                        Log.Error(ex);
                    }
                }
                if (DateTime.Now > NextUpdate.AddSeconds(15))
                {
                    try
                    {
                        LoadAllGates();

                    }
                    catch (Exception ex)
                    {

                        Log.Error(ex);
                    }
                }
                if (DateTime.Now > NextUpdate.AddSeconds(20))
                {
                    try
                    {
                        Log.Info("Loading territories");
                        LoadAllTerritories();
                    }
                    catch (Exception ex)
                    {

                        Log.Error(ex);
                    }
                }
                if (DateTime.Now > NextUpdate.AddSeconds(25))
                {
                    try
                    {
                        LoadAllJumpZones();

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }

            if (ticks % 32 == 0 && TorchState == TorchSessionState.Loaded)
            {
                try
                {
                    GridRepair.DoRepairCycle();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);

                }
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
            if (ticks % 128 == 0)
            {

                try
                {


                    //i should really split this into multiple methods so i dont have one huge method for everything
                    foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                    {

                        if (player.GetPosition() != null)
                        {
                            try
                            {
                                if (config.KothEnabled)
                                {
                                    foreach (KothConfig koth in KOTHs)
                                    {
                                        if (Vector3.Distance(player.GetPosition(), new Vector3(koth.x, koth.y, koth.z)) <= koth.CaptureRadiusInMetre)
                                        {
                                            if (!InCapRadius.ContainsKey(player.Id.SteamId))
                                            {
                                                InCapRadius.Add(player.Id.SteamId, koth.KothName);
                                                NotificationMessage message2 = new NotificationMessage("You are inside the capture radius.", 10000, "Green");
                                                //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                                            }
                                        }
                                        else
                                        {
                                            if (InCapRadius.TryGetValue(player.Id.SteamId, out string name))
                                            {
                                                if (koth.KothName.Equals(name))
                                                {
                                                    InCapRadius.Remove(player.Id.SteamId);
                                                    NotificationMessage message2 = new NotificationMessage("You are outside the capture radius.", 10000, "Red");
                                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                                    ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                                                }
                                            }
                                        }
                                    }
                                    foreach (CaptureSite site in sites)
                                    {
                                        Location loc = site.GetCurrentLocation();
                                        if (loc == null || !loc.WorldName.Equals(MyMultiplayer.Static.HostName) || !loc.Enabled)
                                        {
                                            continue;
                                        }
                                        if (Vector3.Distance(player.GetPosition(), new Vector3(loc.X, loc.Y, loc.Z)) <= loc.CaptureRadiusInMetre)
                                        {
                                            if (!InCapRadius.ContainsKey(player.Id.SteamId))
                                            {
                                                InCapRadius.Add(player.Id.SteamId, loc.Name);
                                                NotificationMessage message2 = new NotificationMessage("You are inside the capture radius.", 10000, "Green");
                                                //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                                            }
                                        }
                                        else
                                        {
                                            if (InCapRadius.TryGetValue(player.Id.SteamId, out string name))
                                            {
                                                if (loc.Name.Equals(name))
                                                {
                                                    InCapRadius.Remove(player.Id.SteamId);
                                                    NotificationMessage message2 = new NotificationMessage("You are outside the capture radius.", 10000, "Red");
                                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                                                    ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                Log.Error(ex);
                            }
                            try
                            {
                                foreach (Territory ter in Territories.Values)
                                {
                                    if (ter.enabled)
                                    {
                                        if (Vector3.Distance(player.GetPosition(), new Vector3(ter.x, ter.y, ter.z)) <= ter.Radius)
                                        {
                                            SendEnterMessage(player, ter);
                                        }
                                        else
                                        {
                                            if (InTerritory.ContainsKey(player.Identity.IdentityId))
                                            {
                                                SendLeaveMessage(player, ter);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }
                    }

                }

                catch (Exception ex)
                {
                    AlliancePlugin.Log.Error(ex);
                }
            }



        }
        public static MyCubeGrid GetLootboxGrid(Vector3 position, LootLocation config)
        {
            if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) != null)
            {
                if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) is MyCubeGrid grid)
                    return grid;
            }

            BoundingSphereD sphere = new BoundingSphereD(position, config.RadiusToCheck + 5000);
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
        public static void SpawnCores(MyCubeGrid grid, LootLocation config, Location loc, MyFaction fac)
        {
            if (grid != null)
            {
                int max = loc.MaxLootAmount;
                int loot = 0;
                Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                foreach (RewardItem item in config.loot)
                {
                    Random random = new Random();
                    if (random.NextDouble() <= item.chance)
                    {
                        if (loot >= max)
                        {
                            continue;
                        }
                        loot++;
                        if (item.CreditReward > 0)
                        {
                            EconUtils.addMoney(fac.FactionId, item.CreditReward);
                        }
                        if (item.TypeId != null && item.TypeId != string.Empty)
                        {
                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out MyDefinitionId id))
                            {
                                if (block != null)
                                {
                                    //   Log.Info("Should spawn item");

                                    MyItemType itemType = new MyInventoryItemFilter(item.TypeId + "/" + item.SubTypeId).ItemType;
                                    int amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                                    block.GetInventory().CanItemsBeAdded((MyFixedPoint)amount, itemType);
                                    block.GetInventory().AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                                }
                            }
                        }

                    }

                }
                return;
            }
        }
        public static void SpawnCores(MyCubeGrid grid, LootLocation config, Location loc, Alliance alliance)
        {
            if (grid != null)
            {
                int max = loc.MaxLootAmount;
                int loot = 0;
                Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                foreach (RewardItem item in config.loot)
                {
                    Random random = new Random();
                    if (random.NextDouble() <= item.chance)
                    {
                        if (loot >= max)
                        {
                            continue;
                        }
                        loot++;
                        if (item.CreditReward > 0)
                        {
                            DatabaseForBank.AddToBalance(alliance, item.CreditReward);
                            alliance.DepositKOTH(item.CreditReward, 1);
                        }
                        if (item.MetaPoint > 0)
                        {

                            alliance.CurrentMetaPoints += item.MetaPoint;
                        }

                        if (item.TypeId != null && item.TypeId != string.Empty)
                        {


                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out MyDefinitionId id))
                            {
                                int amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);

                                if (block != null)
                                {
                                    //   Log.Info("Should spawn item");

                                    MyItemType itemType = new MyInventoryItemFilter(item.TypeId + "/" + item.SubTypeId).ItemType;

                                    block.GetInventory().CanItemsBeAdded((MyFixedPoint)amount, itemType);
                                    block.GetInventory().AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                                }

                            }
                        }

                    }

                }
                SaveAllianceData(alliance);
                return;
            }
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
                    block.GetInventory().CanItemsBeAdded((MyFixedPoint)config.RewardAmount, itemType);
                    block.GetInventory().AddItems((MyFixedPoint)config.RewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(rewardItem));
                }
                else
                {
                    //  Log.Info("Cant spawn item");
                }
                return;
            }
        }
        public static Boolean DoesGridHaveCaptureBlock(MyCubeGrid grid, Location loc)
        {
            foreach (MyCubeBlock block in grid.GetFatBlocks())
            {

                if (block.OwnerId > 0 && block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "").Equals(loc.captureBlockType) && block.BlockDefinition.Id.SubtypeName.Equals(loc.captureBlockSubtype))
                {
                    //  MyRadioAntenna antenna;

                    if (block is Sandbox.ModAPI.IMyBeacon beacon)
                    {
                        // Log.Info(beacon.Radius);

                        if (beacon.IsFunctional && beacon.IsWorking)
                        {
                            if (beacon.Radius >= loc.CaptureBlockRange - 1000)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (block is MyRadioAntenna antenna)
                    {
                        // Log.Info(beacon.Radius);

                        if (antenna.IsFunctional && antenna.IsWorking)
                        {
                            if (antenna.GetRadius() >= loc.CaptureBlockRange - 1000)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (block.IsFunctional && block.IsWorking)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Boolean DoesGridHaveCaptureBlock(MyCubeGrid grid, KothConfig koth, Boolean ignoreOwner = false)
        {
            foreach (MyCubeBlock block in grid.GetFatBlocks())
            {

                if (block.OwnerId > 0 && block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "").Equals(koth.captureBlockType) && block.BlockDefinition.Id.SubtypeName.Equals(koth.captureBlockSubtype))
                {

                    if (block.IsFunctional && block.IsWorking)
                    {
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
        public static CaptureSite SaveCaptureConfig(String name, CaptureSite config)
        {
            FileUtils utils = new FileUtils();
            config.SaveCapProgress();
            config.caplog.SaveSorted();
            utils.WriteToXmlFile<CaptureSite>(path + "//CaptureSites//" + name + ".xml", config);

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
        public static void SendChatMessage(String prefix, String message, ulong steamID = 0)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = prefix;
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
