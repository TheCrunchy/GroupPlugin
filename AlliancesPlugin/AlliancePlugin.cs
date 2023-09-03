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
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using HarmonyLib;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Weapons;
using Sandbox.Game;
using static AlliancesPlugin.Alliances.StorePatchTaxes;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Gates;
using static AlliancesPlugin.JumpGates.JumpGate;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Integrations;
using AlliancesPlugin.JumpZones;
using AlliancesPlugin.WarOptIn;
using AlliancesPlugin.KamikazeTerritories;
using AlliancesPlugin.KOTH;
using AlliancesPlugin.NexusStuff;
using AlliancesPlugin.Territory_Version_2;
using AlliancesPlugin.Territory_Version_2.CapLogics;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using AlliancesPlugin.Territory_Version_2.SecondaryLogics;
using Newtonsoft.Json;
using Sandbox.Definitions;
using SpaceEngineers.Game.EntityComponents.Blocks;
using Torch.Managers.PatchManager;

namespace AlliancesPlugin
{
    public class AlliancePlugin : TorchPluginBase
    {
        public static List<ComponentCost> repairCost = new List<ComponentCost>();
        public static Dictionary<String, ComponentCost> ComponentCosts = new Dictionary<string, ComponentCost>();
        public void AddComponentCost(string subtype, long cost, bool banned)
        {
            if (repairCost.Any(x => x.SubTypeId == subtype))
            {
                return;
            }
            repairCost.Add(new ComponentCost()
            {
                SubTypeId = subtype,
                Cost = cost,
                IsBannedComponent = banned
            });
        }

        public void AddComponentCostToDictionary()
        {
            foreach (ComponentCost comp in repairCost)
            {
                if (!ComponentCosts.ContainsKey(comp.SubTypeId))
                {
                    ComponentCosts.Add(comp.SubTypeId, comp);
                }
            }
        }



        public static Random random = new Random();
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

        public static Dictionary<Guid, JumpGate> AllGates = new Dictionary<Guid, JumpGate>();

        public static FileUtils utils = new FileUtils();
        public static ITorchPlugin GridBackup;
        public static ITorchPlugin MQ;
        public static MethodInfo BackupGrid;

        public static ITorchBase TorchBase;
        public static bool GridBackupInstalled = false;
        public static Dictionary<MyDefinitionId, int> ItemUpkeep = new Dictionary<MyDefinitionId, int>();
        public static MethodInfo SendMessage;

        public static bool MQPluginInstalled = false;

        public static bool InitPlugins = false;
        public static NexusAPI API { get; private set; }
        public static bool NexusInstalled { get; private set; } = false;

        private static readonly Guid NexusGUID = new Guid("28a12184-0422-43ba-a6e6-2e228611cca5");
        public class GridPrintTemp
        {
            public List<MyCubeGrid> gridsToSave = new List<MyCubeGrid>();
            public Guid AllianceId;
            public GridCosts gridCosts;
        }

        public static Stack<GridPrintTemp> GridPrintQueue = new Stack<GridPrintTemp>();

        public static void InitPluginDependencies(PluginManager Plugins, PatchManager Patches)
        {
            InitPlugins = true;
            if (Plugins.Plugins.TryGetValue(Guid.Parse("75e99032-f0eb-4c0d-8710-999808ed970c"), out var GridBackupPlugin))
            {
                BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                GridBackup = GridBackupPlugin;
                GridBackupInstalled = true;
            }

            if (Plugins.Plugins.TryGetValue(Guid.Parse("319afed6-6cf7-4865-81c3-cc207b70811d"), out var MQPlugin))
            {
                SendMessage = MQPlugin.GetType().GetMethod("SendMessage", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(string), typeof(string) }, null);
                MQ = MQPlugin;

                MQPatching.MQPluginPatch.Patch(Patches.AcquireContext());
                Patches.Commit();

                MQPluginInstalled = true;
            }

            if (Plugins.Plugins.TryGetValue(NexusGUID, out ITorchPlugin torchPlugin))
            {
                Type type = torchPlugin.GetType();
                Type type2 = ((type != null) ? type.Assembly.GetType("Nexus.API.PluginAPISync") : null);
                if (type2 != null)
                {
                    type2.GetMethod("ApplyPatching", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
                    {
                        typeof(NexusAPI),
                        "Alliances"
                    });
                    API = new NexusAPI(4398);
                    MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(4398, new Action<ushort, byte[], ulong, bool>(HandleNexusMessage));
                    NexusInstalled = true;
                }
            }
        }

        private static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<AllianceChatMessage>(data);
            AllianceChat.ReceiveChatMessage(message);
        }

        public static void TestMethod(string MessageType, string MessageBody)
        {
            Log.Info("It worked?");
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
            var identityId = AlliancePlugin.GetIdentityByNameOrId(SteamId.ToString());
            Alliance alliance = null;
            var fac = FacUtils.GetPlayersFaction(identityId.IdentityId);
            if (fac != null)
            {
                alliance = GetAllianceNoLoading(fac as MyFaction);
                if (alliance != null)
                {
                    // alliance.reputation
                    //amount = Convert.ToInt64(amount * 1.05f);
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
            sessionManager.AddOverrideMod(2861556200L);
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            Directory.CreateDirectory($"{AlliancePlugin.path}//ShipClassLimits//");


            Directory.CreateDirectory(path + "//JumpGates//");

            Directory.CreateDirectory(path + "//Vaults//");

            Directory.CreateDirectory(path + "//Territories//");

            Directory.CreateDirectory(path + "//PlayerData//");



            TorchBase = Torch;
            LoadAllAlliances();
        }

        public void SetupConfig()
        {
            var utils = new FileUtils();
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
            watcher = new FileSystemWatcher($"{folder}//AllianceData");
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;
            return folder;
        }
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            KnownPaths.Add(e.FullPath);
        }
        public static FileSystemWatcher watcher;

        public static Config LoadConfig()
        {
            var utils = new FileUtils();
            config = utils.ReadFromXmlFile<Config>(basePath + "\\Alliances.xml");
            return config;

        }

        public static void saveConfig()
        {
            var utils = new FileUtils();

            utils.WriteToXmlFile<Config>(basePath + "\\Alliances.xml", config);

            return;
        }
        public static void SaveAllianceData(Alliance alliance)
        {
            var jsonStuff = new FileUtils();

            jsonStuff.WriteToJsonFile<Alliance>(path + "//AllianceData//" + alliance.AllianceId + ".json", alliance);
            AlliancePlugin.AllAlliances[alliance.name] = alliance;
        }
        public static Alliance LoadAllianceData(Guid id)
        {
            var jsonStuff = new FileUtils();
            try
            {
                var alliance2 = jsonStuff.ReadFromJsonFile<Alliance>(path + "//AllianceData//" + id + ".json");
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
            foreach (var alliance in AllAlliances)
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
            foreach (var alliance in AllAlliances)
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
            foreach (var alliance in AllAlliances)
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

            foreach (var alliance in AllAlliances)
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
            var FactionCollection = MySession.Static.Factions.GetType().Assembly.GetType("Sandbox.Game.Multiplayer.MyFactionCollection");
            sendChange = FactionCollection?.GetMethod("SendFactionChange", BindingFlags.NonPublic | BindingFlags.Static);
        }
        private static List<Vector3> StationLocations = new List<Vector3>();

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

            if (entity is MyFunctionalBlock block)
            {

                return block.OwnerId;
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
            var attackerId = GetAttacker(info.AttackerId);

            if (target is MyCubeBlock cube)
            {
                NewTerritoryHandler.AddToBlockLog(cube, attackerId);
            }

            if (!config.DisablePvP) return;
            if (!(target is MySlimBlock block)) return;
            //  Log.Info("is an entity");


            if (FacUtils.GetOwner(block.CubeGrid) == 0L)
            {
                //    Log.Info("no owner");
                return;
            }
            if (block.OwnerId == attackerId)
            {
                //    Log.Info("owner is attacker");
                return;
            }
            var loc = block.CubeGrid.PositionComp.GetPosition();
            if ((from territory in KamikazeTerritories.MessageHandler.Territories.Where(x => x.ForcesPvP) let distance = Vector3.Distance(loc, territory.Position) where distance <= territory.Radius select territory).Any())
            {
                return;
            }
            var attacker = MySession.Static.Factions.GetPlayerFaction(attackerId) as MyFaction;
            var defender = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner(block.CubeGrid));

            // Log.Info(info.Type.ToString().Trim());
            if (!info.Type.ToString().Trim().Equals("Grind") &&
                !info.Type.ToString().Trim().Equals("Explosion")) return;
            //   Log.Info("Grind damage");
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
                if (defender == null)
                {
                    info.Amount = 0.0f;
                    return;
                }
                if (!AlliancePlugin.config.EnableOptionalWar)
                {
                    SlimBlockPatch.SendPvEMessage(attackerId);
                    info.Amount = 0.0f;
                    return;
                }
                if (MySession.Static.Factions.AreFactionsEnemies(attacker.FactionId, defender.FactionId)) return;
                //  AlliancePlugin.Log.Info("not 4");
                SlimBlockPatch.SendPvEMessage(attackerId);
                info.Amount = 0.0f;
                return;

            }

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

            SlimBlockPatch.SendPvEMessage(attackerId);
            info.Amount = 0.0f;
            return;

        }

        public static void BalanceChangedMethod2(
     MyAccountInfo oldAccountInfo,
     MyAccountInfo newAccountInfo)
        {



            if (Sync.Players.TryGetPlayerId(newAccountInfo.OwnerIdentifier, out var player))
            {
                if (MySession.Static.Players.TryGetPlayerById(player, out var pp))
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

        public static bool SendToMQ(string Type, Object SendThis)
        {
            if (NexusInstalled && AlliancePlugin.config.UsingNexusChat && SendThis is AllianceChatMessage chatMessage)
            {
                var message = MyAPIGateway.Utilities.SerializeToBinary<AllianceChatMessage>(chatMessage);
                API.SendMessageToAllServers(message);
                AllianceChat.ReceiveChatMessage(chatMessage);
                return true;
            }
            if (!MQPluginInstalled)
            {
                return false;
            }
            var input = JsonConvert.SerializeObject(SendThis);
            var methodInput = new object[] { Type, input };
            AlliancePlugin.SendMessage?.Invoke(AlliancePlugin.MQ, methodInput);
            return true;
        }

        public static OptinCore warcore = new OptinCore();

        public static Random rand = new Random();
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {


            if (state == TorchSessionState.Unloading)
            {

                // DiscordStuff.DisconnectDiscord();
                foreach (var ter in AlliancePlugin.Territories.Values)
                {
                    AlliancePlugin.utils.WriteToJsonFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".json", ter);
                }

                foreach (var item in GridPrintQueue)
                {
                    try
                    {
                        Task.Run(async () =>
                                {
                                    GridManager.SaveGrid(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + item.AllianceId + "\\") + item.gridCosts.getGridName() + ".xml", item.gridCosts.getGridName(), false, true, item.gridsToSave);
                                });
                    }
                    catch (Exception e)
                    {
                        AlliancePlugin.Log.Error("shipyard error " + e);
                    }
                }

                TorchState = TorchSessionState.Unloading;
            }

            if (state != TorchSessionState.Loaded) return;
            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (def is MySearchEnemyComponentDefinition search)
                {
                    search.SearchRadius = 5000;
                    Log.Info("Changing Range");
                }
            }
            if (!File.Exists(path + "//Territories//Example.json"))
            {
                var example = new Territory();
                var logic = new AllianceGridCapLogic();
                var logic2 = new BlockOwnershipLogic();
                logic.SecondaryLogics = new List<ISecondaryLogic>();
                logic2.SecondaryLogics = new List<ISecondaryLogic>();
                var block = new BlockDisablerLogic();
                block.TargetedSubtypes = new List<string>()
                {
                    "ShipWelder"
                };
                var block2 = new GridDamagerLogic();
                block2.TargetedSubtypes = new List<string>()
                {
                    "LargeBlockBeacon"
                };
                var thrust = new ThrustDisablerLogic();
                logic2.SecondaryLogics.Add(block);
                logic2.SecondaryLogics.Add(block2);
                logic2.SecondaryLogics.Add(thrust);
                var loot = new LootLogic();
                loot.Loot = new List<LootLogic.LootItem>();
                loot.Loot.Add(new LootLogic.LootItem());
                var craft = new LootConverter();
                var item = new CraftedItem();
                item.typeid = "Ore";
                item.subtypeid = "Iron";
                item.amountPerCraft = 500;
                item.chanceToCraft = 1;
                logic2.SecondaryLogics.Add(new SafezoneLogic());
                var recipe = new RecipeItem();
                recipe.typeid = "Ore";
                recipe.subtypeid = "Stone";
                recipe.amount = 500;
                var radar = new RadarLogic();
                radar.Distance = 50000;
                radar.Enabled = true;
                radar.Priority = 1;
                radar.RequireOwner = true;
                logic.SecondaryLogics.Add(radar);
                item.RequriedItems.Add(recipe);
                craft.CraftableItems.Add(item);
                logic.SecondaryLogics.Add(loot);
                logic.SecondaryLogics.Add(craft);
                var printer = new GridPrinterLogic();
                var paster = new GridPasterLogic();

                logic.SecondaryLogics.Add(printer);
                logic.SecondaryLogics.Add(paster);
                example.CapturePoints.Add(logic);
                example.CapturePoints.Add(logic2);
                example.Enabled = false;

                utils.WriteToJsonFile<Territory>(path + "//Territories//Example.json", example, false);
            }
            MyAPIGateway.Multiplayer.RegisterMessageHandler(8544, Integrations.AllianceIntegrationCore.ReceiveModMessage);
            KamikazeTerritories.MessageHandler.LoadFile();
            Directory.CreateDirectory(AlliancePlugin.path + "//OptionalWar//");
            if (!File.Exists(AlliancePlugin.path + "//OptionalWar//WarConfig.json"))
            {
                AlliancePlugin.utils.WriteToJsonFile<WarConfig>(AlliancePlugin.path + "//OptionalWar//WarConfig.json", new WarConfig());

            }
            warcore.config = AlliancePlugin.utils.ReadFromJsonFile<WarConfig>(AlliancePlugin.path + "//OptionalWar//WarConfig.json");
            if (!AlliancePlugin.config.ConvertedFromOldWarFile)
            {
                AlliancePlugin.config.EnableOptionalWar = warcore.config.EnableOptionalWar;
                AlliancePlugin.config.ConvertedFromOldWarFile = true;
                AlliancePlugin.saveConfig();
            }

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, new BeforeDamageApplied(DamageHandler));
            //      MyEntities.OnEntityAdd += NEWSUIT;
            if (config != null && config.AllowDiscord)
            {
                var SendingMessage = new AllianceChatMessage();

                SendingMessage.SenderPrefix = "Init";
                SendingMessage.MessageText = "Init Message";
                SendingMessage.AllianceId = Guid.Empty;
                SendingMessage.ChannelId = config.DiscordChannelId;
                SendingMessage.BotToken = config.DiscordBotToken;

                SendToMQ("AllianceMessage", SendingMessage);
            }
            nextRegister = DateTime.Now;
            //    rand.Next(1, 60);

            LoadAllGates();

            MyBankingSystem.Static.OnAccountBalanceChanged += BalanceChangedMethod2;

            AllianceChat.ApplyLogging();
            TorchState = TorchSessionState.Loaded;
            repairCost = new List<ComponentCost>();
            if (File.Exists($"{path}/ComponentCosts.json"))
            {
                repairCost = utils.ReadFromJsonFile<List<ComponentCost>>($"{path}/ComponentCosts.json");
            }
            AddComponentCost("AdminKit", 5000000, true);
            AddComponentCost("AdminComponent", 5000000, true);

            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if ((def as MyComponentDefinition) == null) continue;
                var min = (def as MyComponentDefinition).MinimalPricePerUnit;
                this.AddComponentCost(def.Id.SubtypeName, 1000, false);
            }

            utils.WriteToJsonFile($"{path}/ComponentCosts.json", repairCost);

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
            Directory.CreateDirectory(path + "//JumpZones//");

            Directory.CreateDirectory(path + "//EditorBackups//");

            Directory.CreateDirectory(path + "//UpkeepBackups//");
            if (!Directory.Exists(basePath + "//Alliances"))
            {
                Directory.CreateDirectory(basePath + "//Alliances//");
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
            if (!Directory.Exists(path + "//HangarUpgrades//"))
            {
                Directory.CreateDirectory(path + "//HangarUpgrades//");
            }
            if (!File.Exists(path + "//ItemUpkeep.txt"))
            {

                var output = new StringBuilder();
                output.AppendLine("TypeId,SubtypeId,Amount");
                output.AppendLine("MyObjectBuilder_Ingot,Uranium,1");
                File.WriteAllText(path + "//ItemUpkeep.txt", output.ToString());

            }

            //convert this to new format 
            if (!File.Exists(path + "//ShipyardUpgrades//Speed//SpeedUpgrade_0.xml"))
            {

                var upg = new ShipyardSpeedUpgrade();
                var req = new ItemRequirement();
                upg.items.Add(req);
                utils.WriteToXmlFile<ShipyardSpeedUpgrade>(path + "//ShipyardUpgrades//Speed//SpeedUpgrade_0.xml", upg);

            }
            if (!File.Exists(path + "//ShipyardUpgrades//Slot//SlotUpgrade_0.xml"))
            {

                var upg = new ShipyardSlotUpgrade();
                var req = new ItemRequirement();
                upg.items.Add(req);
                utils.WriteToXmlFile<ShipyardSlotUpgrade>(path + "//ShipyardUpgrades//Slot//SlotUpgrade_0.xml", upg);

            }

            if (!File.Exists(path + "//HangarDeniedLocations.txt"))
            {

                var output = new StringBuilder();
                output.AppendLine("Name,X,Y,Z,Radius");
                output.AppendLine("Fred,0,0,0,50000");
                File.WriteAllText(path + "//HangarDeniedLocations.txt", output.ToString());

            }
            else
            {
                String[] line;
                line = File.ReadAllLines(path + "//HangarDeniedLocations.txt");
                for (var i = 1; i < line.Length; i++)
                {
                    var loc = new DeniedLocation();
                    var split = line[i].Split(',');
                    foreach (var s in split)
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

                var upg = new HangarUpgrade();
                var req = new ItemRequirement();
                upg.items.Add(req);
                utils.WriteToXmlFile<HangarUpgrade>(path + "//HangarUpgrades//SlotUpgrade_0.xml", upg);

            }


            if (!Directory.Exists(path + "//ShipyardBlocks//"))
            {
                Directory.CreateDirectory(path + "//ShipyardBlocks//");
            }

            if (!File.Exists(path + "//ShipyardBlocks//LargeProjector.xml"))
            {
                var config33 = new ShipyardBlockConfig();
                config33.SetShipyardBlockConfig("LargeProjector");
                utils.WriteToXmlFile<ShipyardBlockConfig>(path + "//ShipyardBlocks//LargeProjector.xml", config33, false);

            }
            if (!File.Exists(path + "//ShipyardBlocks//SmallProjector.xml"))
            {
                var config33 = new ShipyardBlockConfig();
                config33.SetShipyardBlockConfig("SmallProjector");
                utils.WriteToXmlFile<ShipyardBlockConfig>(path + "//ShipyardBlocks//SmallProjector.xml", config33, false);

            }
            if (!File.Exists(path + "//ShipyardConfig.xml"))
            {
                utils.WriteToXmlFile<ShipyardConfig>(path + "//ShipyardConfig.xml", new ShipyardConfig(), false);

            }

            ReloadShipyard();
            SetupFriendMethod();

            LoadAllAlliances();
            LoadAllGates();
            LoadAllRefineryUpgrades();
            LoadAllTerritories();

            LoadItemUpkeep();


            foreach (var alliance in AllAlliances.Values)
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
                        if (Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length < 59)
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
        public static void LoadItemUpkeep()
        {
            String[] line;
            line = File.ReadAllLines(path + "//ItemUpkeep.txt");
            for (var i = 1; i < line.Length; i++)
            {

                var split = line[i].Split(',');
                foreach (var s in split)
                {
                    s.Replace(" ", "");
                }

                if (MyDefinitionId.TryParse(split[0], split[1], out var id))
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
            foreach (var s in Directory.GetFiles(path + "//ShipyardBlocks//"))
            {


                var conf = utils.ReadFromXmlFile<ShipyardBlockConfig>(s);

                shipyardConfig.AddToBlockConfig(conf);
            }
            foreach (var s in Directory.GetFiles(path + "//ShipyardUpgrades//"))
            {
                ShipyardCommands.ConvertUpgradeCost(s);
            }
            foreach (var s in Directory.GetFiles(path + "//ShipyardUpgrades//Slot//"))
            {
                ShipyardCommands.LoadShipyardSlotCost(s);

            }
            foreach (var s in Directory.GetFiles(path + "//ShipyardUpgrades//Speed//"))
            {
                ShipyardCommands.LoadShipyardSpeedCost(s);

            }
            foreach (var s in Directory.GetFiles(path + "//HangarUpgrades//"))
            {
                HangarCommands.LoadHangarUpgrade(s);
            }
        }
        public void LoadAllRefineryUpgrades()
        {
            MyProductionPatch.upgrades.Clear();
            foreach (var s in Directory.GetFiles(path + "//RefineryUpgrades//"))
            {
                var upgrade = utils.ReadFromXmlFile<RefineryUpgrade>(s);
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
            foreach (var s in Directory.GetFiles(path + "//AssemblerUpgrades//"))
            {
                var upgrade = utils.ReadFromXmlFile<AssemblerUpgrade>(s);
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
                if (ulong.TryParse(playerNameOrSteamId, out var steamId))
                {
                    var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
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
            //FileUtils jsonStuff = new FileUtils();
            //try
            //{
            //  //  JumpPatch.Zones.Clear();
            //    foreach (String s in Directory.GetFiles(path + "//JumpZones//"))
            //    {

            //        JumpPatch.Zones.Add(jsonStuff.ReadFromXmlFile<JumpZone>(s));


            //    }
            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex);
            //}

        }
        public static void LoadAllAlliancesForUpkeep()
        {
            var jsonStuff = new FileUtils();
            try
            {
                AllAlliances.Clear();
                FactionsInAlliances.Clear();
                foreach (var s in Directory.GetFiles(path + "//AllianceData//"))
                {

                    var alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                    if (AllAlliances.ContainsKey(alliance.name))
                    {
                        alliance.name += " DUPLICATE";
                        AllAlliances.Add(alliance.name, alliance);

                        foreach (var id in alliance.AllianceMembers)
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
                        foreach (var id in alliance.AllianceMembers)
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

        public static void TriggerModUpdate()
        {
            AllianceIntegrationCore.SendAllAllianceMemberDataToMods();
        }

        public static List<string> LoadThisShit = new List<string>();
        public static List<string> KnownPaths = new List<string>();

        private DateTime NextSendToClient = DateTime.Now;
        public static void LoadAllAlliances()
        {

            if (TorchState == TorchSessionState.Loaded)
            {
                var jsonStuff = new FileUtils();
                try
                {
                    if (!Loading)
                    {
                        AllAlliances.Clear();
                        FactionsInAlliances.Clear();
                        Parallel.Invoke();
                        Task.Run(async () =>
                        {
                            foreach (var s in Directory.EnumerateFiles(path + "//AllianceData//"))
                            {
                                KnownPaths.Add(s);
                            }

                            foreach (var s in KnownPaths)
                            {
                                try
                                {
                                    if (!File.Exists(s))
                                    {
                                        KnownPaths.Remove(s);
                                        continue;
                                    }
                                    var alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                                    if (alliance != null)
                                    {
                                        if (AllAlliances.ContainsKey(alliance.name))
                                        {
                                            AllAlliances[alliance.name] = alliance;

                                            foreach (var id in alliance.AllianceMembers)
                                            {
                                                if (!FactionsInAlliances.ContainsKey(id))
                                                {
                                                    FactionsInAlliances.Add(id, alliance.name);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AllAlliances.Add(alliance.name, alliance);
                                            foreach (var id in alliance.AllianceMembers)
                                            {
                                                if (!FactionsInAlliances.ContainsKey(id))
                                                {
                                                    FactionsInAlliances.Add(id, alliance.name);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    AlliancePlugin.Log.Error(e);
                                }
                            }
                        });
                    }
                    else
                    {
                        foreach (var s in KnownPaths)
                        {
                            try
                            {
                                if (!File.Exists(s))
                                {
                                    KnownPaths.Remove(s);
                                    continue;
                                }
                                var alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                                if (alliance != null)
                                {
                                    if (AllAlliances.ContainsKey(alliance.name))
                                    {
                                        AllAlliances[alliance.name] = alliance;

                                        foreach (var id in alliance.AllianceMembers)
                                        {
                                            if (!FactionsInAlliances.ContainsKey(id))
                                            {
                                                FactionsInAlliances.Add(id, alliance.name);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        AllAlliances.Add(alliance.name, alliance);
                                        foreach (var id in alliance.AllianceMembers)
                                        {
                                            if (!FactionsInAlliances.ContainsKey(id))
                                            {
                                                FactionsInAlliances.Add(id, alliance.name);
                                            }
                                        }
                                    }
                                }

                                LoadThisShit.Remove(s);

                            }
                            catch (Exception e)
                            {
                                AlliancePlugin.Log.Error(e);
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

        public static List<IMyIdentity> GetAllIdentitiesByNameOrId(string playerNameOrSteamId)
        {
            var ids = new List<IMyIdentity>();
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                {
                    if (!ids.Contains(identity))
                    {
                        ids.Add(identity);
                    }
                }
                if (ulong.TryParse(playerNameOrSteamId, out var steamId))
                {
                    var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
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
            KamikazeTerritories.MessageHandler.LoadFile();
            Territories.Clear();
            var jsonStuff = new FileUtils();

            foreach (var s in Directory.GetFiles(path + "//Territories//"))
            {
                try
                {
                    var ter = new Territory();
                    if (s.EndsWith(".xml"))
                    {
                        continue;
                    }
                    if (s.EndsWith(".json"))
                    {
                        ter = jsonStuff.ReadFromJsonFile<Territory>(s);
                    }
                    if (!ter.Enabled)
                    {
                        continue;
                    }

                    if (ter.WorldName != MyMultiplayer.Static.HostName)
                    {
                        Log.Info($"Doesnt match world name {ter.WorldName} expected {MyMultiplayer.Static.HostName}");
                        continue;
                    }
                    if (Territories.ContainsKey(ter.Id))
                    {
                        Log.Info($"Duplicate territory ID at {s}");
                        continue;
                    }
                    Territories.Add(ter.Id, ter);
                    //foreach (var city in ter.ActiveCities.Where(x => !CityHandler.ActiveCities.Any(z => z.CityId == x.CityId)))
                    //{
                    //    CityHandler.ActiveCities.Add(city);
                    //}

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
            if (MyMultiplayer.Static.HostName.ToLower().Contains("drac"))
            {
                return;
            }
            var FilesToDelete = new List<string>();
            if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
            {
                WorldName = MyMultiplayer.Static.HostName;
            }
            var jsonStuff = new FileUtils();
            try
            {
                AllGates.Clear();
                foreach (var s in Directory.GetFiles(path + "//JumpGates//"))
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
            var deleted = false;
            foreach (var s in FilesToDelete)
            {
                File.Delete(s);
                deleted = true;
            }
            if (deleted)
            {
                foreach (var gate in AllGates.Values)
                {
                    gate.Save();
                }
            }
        }
        public static Boolean SendPlayerNotify(MyPlayer player, int milliseconds, string message, string color)
        {
            var message2 = new NotificationMessage();
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

        public static bool ConsumeComponentsGateFee(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            var toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (var c in components)
            {
                var needed = ShipyardCommands.CountComponents(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    ShipyardCommands.SendMessage("[Gate Fee]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)steamid);

                    return false;
                }
            }

            foreach (var item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }
        public static Boolean DoFeeStuff(MyPlayer player, JumpGate gate, MyCubeGrid Controller)
        {
            if (gate.RequireDrive && Controller != null)
            {
                var drives = new List<MyJumpDrive>();
                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Controller).GetBlocksOfType<MyJumpDrive>(drives);
                var enabled = false;
                if (drives.Count == 0)
                {
                    enabled = false;
                }

                foreach (var drive in drives)
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


            if (gate.itemCostsForUse && Controller != null)
            {
                var items = new Dictionary<MyDefinitionId, int>();
                foreach (var item in gate.itemCostsList)
                {

                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out var id))
                    {
                        decimal multiplier = 1;
                        multiplier += Controller.BlocksCount / item.BlockCountDivision;
                        var amount = item.BaseItemAmount * multiplier;
                        items.Add(id, (int)amount);
                    }
                }
                if (ConsumeComponentsGateFee(ShipyardCommands.GetInventories(Controller), items, player.Client.SteamUserId))
                {

                    SendPlayerNotify(player, 1000, $"Item cost taken from cargo.", "Green");
                    return true;

                }
                else
                {
                    SendPlayerNotify(player, 1000, "Gate is disabled. Alliance failed upkeep.", "Red");
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
            if (gate.itemCostsForUse)
            {
                SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
                foreach (var item in gate.itemCostsList)
                {
                    SendPlayerNotify(player, 1000, $"It costs {item.SubTypeId} {item.TypeId} SC to jump.", "Green");
                    return true;
                }
            }

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
            NewGateLogic.DoGateLogic();
        }
        public void OrganisePlayers()
        {
            Task.Run(async () =>
            {
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    var fac = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId);
                    if (fac == null) continue;
                    var temp = GetAllianceNoLoading(fac);
                    if (temp == null) continue;
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
                        var bob = new List<ulong>();
                        bob.Add(player.Id.SteamId);
                        playersInAlliances.Add(temp.AllianceId, bob);

                        if (!playersAllianceId.ContainsKey(player.Id.SteamId))
                        {
                            playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                        }
                    }
                }
            });
        }
        public static List<long> TaxingId = new List<long>();
        public static List<long> OtherTaxingId = new List<long>();
        public void DoTaxStuff()
        {
            var Processed = new List<long>();
            var Territory = new Dictionary<Guid, Dictionary<long, float>>();


            var taxes = new Dictionary<Guid, Dictionary<long, float>>();

            foreach (var id in TaxesToBeProcessed.Keys)
            {

                if (MySession.Static.Factions.TryGetPlayerFaction(id) != null)
                {
                    bool Paid = false;
                    if (AlliancePlugin.config.TerritoryTaxes)
                    {

                    }

                    if (!Paid)
                    {
                        var alliance = GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(id) as MyFaction);
                        if (alliance != null)
                        {

                            alliance = GetAlliance(alliance.name);

                            if (alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id)) > 0)
                            {

                                var tax = TaxesToBeProcessed[id] * alliance.GetTaxRate(MySession.Static.Players.TryGetSteamId(id));
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
                                        var temp = new Dictionary<long, float>();
                                        temp.Add(id, tax);
                                        taxes.Add(alliance.AllianceId, temp);
                                    }
                                }
                                Processed.Add(id);
                            }
                        }
                    }
                }
            }

            DatabaseForBank.Taxes(taxes);
            TerritoryTaxes.Clear();
            foreach (var id in Processed)
            {
                TaxesToBeProcessed.Remove(id);
            }
        }

        public static bool Paused = false;


        public static Dictionary<ulong, String> InCapRadius = new Dictionary<ulong, String>();
        public static Dictionary<ulong, Guid> TerritoryInside = new Dictionary<ulong, Guid>();
        public static List<JumpThing> jumpies = new List<JumpThing>();
        public static Dictionary<Guid, Territory> Territories = new Dictionary<Guid, Territory>();
        public static Dictionary<long, DateTime> InTerritory = new Dictionary<long, DateTime>();
        public static void SendEnterMessage(MyPlayer player, Territory ter)
        {
            Alliance alliance = null;

            //  alliance = AlliancePlugin.GetAllianceNoLoading(ter.Alliance);

            var message2 = new NotificationMessage();


            if (InTerritory.ContainsKey(player.Identity.IdentityId))
            {

                if (DateTime.Now < InTerritory[player.Identity.IdentityId])
                    return;
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
                    var message3 = new NotificationMessage(ter.ControlledMessage.Replace("{alliance}", alliance.name), 5000, "Red");
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
                    var message3 = new NotificationMessage(ter.ControlledMessage.Replace("{alliance}", alliance.name), 5000, "Red");
                    ModCommunication.SendMessageTo(message3, player.Id.SteamId);
                }
                return;
            }
        }

        public static void SendLeaveMessage(MyPlayer player, Territory ter)
        {
            if (TerritoryInside.TryGetValue(player.Id.SteamId, out var ter2))
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
            var message2 = new NotificationMessage(ter.ExitMessage.Replace("{name}", ter.Name), 10000, "White");

            ModCommunication.SendMessageTo(message2, player.Id.SteamId);
            InTerritory.Remove(player.Identity.IdentityId);
            TerritoryInside.Remove(player.Id.SteamId);
        }


        public static DateTime chat = DateTime.Now;

        public static Dictionary<ulong, DateTime> UpdateThese = new Dictionary<ulong, DateTime>();
        public static DateTime RegisterMainBot = DateTime.Now;
        public static Dictionary<ulong, Boolean> statusUpdate = new Dictionary<ulong, bool>();
        public static List<ulong> PlayersNeedPvPAreas = new List<ulong>();
        public static Dictionary<ulong, Guid> otherAllianceShit = new Dictionary<ulong, Guid>();
        public DateTime InitDiscord = DateTime.Now;
        public DateTime NextTerritoryUpdate = DateTime.Now;

        public static bool Loading = false;
        private FileUtils jsonStuff = new FileUtils();
        DateTime NextSave = DateTime.Now;
        public override async void Update()
        {
            try
            {
                if (!InitPlugins)
                {
                    InitPluginDependencies(Torch.Managers.GetManager<PluginManager>(), Torch.Managers.GetManager<PatchManager>());
                    InitPlugins = true;

                    if (AlliancePlugin.config.EnableOptionalWar)
                    {
                        MySession.Static.Factions.FactionStateChanged += warcore.StateChange;
                        MySession.Static.Factions.FactionCreated += warcore.ProcessNewFaction;
                        warcore.config.EnableOptionalWar = true;
                        foreach (var fac in MySession.Static.Factions.GetAllFactions())
                        {
                            if (fac.Tag.Length > 3)
                                continue;

                            var alliance = AlliancePlugin.GetAlliance(fac);
                            foreach (var fac2 in MySession.Static.Factions.GetAllFactions())
                            {
                                if (fac2.Tag.Length > 3)
                                    continue;
                                if (alliance.AllianceMembers.Contains(fac2.FactionId))
                                {
                                    continue;
                                }
                                if (fac == fac2) continue;

                                if (warcore.GetStatus(fac.FactionId) is "Disabled." || warcore.GetStatus(fac2.FactionId) is "Disabled.")
                                {
                                    AlliancePlugin.warcore.DoNeutralUpdate(fac.FactionId, fac2.FactionId);
                                }
                            }
                        }
                    }
                    else
                    {
                        warcore.config.EnableOptionalWar = false;
                    }
                }

                if (ticks % 128 == 0)
                {
                    try
                    {
                        Task.Run(async () => { CaptureHandler.DoCaps(); });
                    }
                    catch (Exception e)
                    {
                        AlliancePlugin.Log.Error("Error in territory cap logic", e.ToString());
                    }

                    if (DateTime.Now > NextSave)
                    {
                        NextSave = DateTime.Now.AddMinutes(5);
                        foreach (var ter in Territories)
                        {
                            AlliancePlugin.utils.WriteToJsonFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Value.Name + ".json", ter.Value);
                        }
                    }
                }

                if (ticks % 16 == 0)
                {
                    try
                    {
                        if (GridPrintQueue.Any() && TorchState == TorchSessionState.Loaded)
                        {
                            var item = GridPrintQueue.Pop();
                            if (item != null)
                            {
                                Task.Run(async () =>
                            {
                                GridManager.SaveGrid(System.IO.Path.Combine(AlliancePlugin.path + "\\ShipyardData\\" + item.AllianceId + "\\") + item.gridCosts.getGridName() + ".xml", item.gridCosts.getGridName(), false, true, item.gridsToSave);

                            });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AlliancePlugin.Log.Error("shipyard error " + e);
                    }
                }
                if (ticks % 512 == 0)
                {
                    if (DateTime.Now >= NextSendToClient && TorchState == TorchSessionState.Loaded)
                    {
                        AllianceIntegrationCore.SendAllAllianceMemberDataToMods();
                        NextSendToClient = DateTime.Now.AddMinutes(5);
                    }

                    var YEET = new Dictionary<ulong, DateTime>();
                    var oof = new List<ulong>();
                    var OtherYeet = new List<ulong>();

                    foreach (var pair in UpdateThese.Where(pair => DateTime.Now >= pair.Value))
                    {
                        oof.Add(pair.Key);
                        if (!YEET.ContainsKey(pair.Key))
                        {
                            YEET.Add(pair.Key, DateTime.Now.AddMinutes(1));

                        }
                        if (statusUpdate.TryGetValue(pair.Key, out var status))
                        {
                            statusUpdate.Remove(pair.Key);
                        }

                        AllianceCommands.SendStatusToClient(AllianceChat.PeopleInAllianceChat.ContainsKey(pair.Key),
                            pair.Key);
                    }
                    foreach (var id in oof)
                    {
                        if (UpdateThese.TryGetValue(id, out var time))
                        {
                            UpdateThese[id] = time.AddSeconds(5);
                        }
                    }
                    oof.Clear();
                    foreach (var pair in YEET.Where(pair => DateTime.Now > pair.Value))
                    {
                        OtherYeet.Add(pair.Key);
                        UpdateThese.Remove(pair.Key);
                    }
                    foreach (var id in OtherYeet)
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
                    if (config.AllowDiscord && DateTime.Now >= InitDiscord)
                    {
                        InitDiscord = InitDiscord.AddMinutes(1);
                        foreach (var alliance in from keys in registerThese where DateTime.Now > keys.Value select GetAlliance(keys.Key) into alliance where alliance != null select alliance)
                        {
                            var SendingMessage = new AllianceChatMessage();

                            SendingMessage.SenderPrefix = "Init";
                            SendingMessage.MessageText = "Init Message";
                            SendingMessage.AllianceId = alliance.AllianceId;
                            SendingMessage.ChannelId = alliance.DiscordChannelId;
                            SendingMessage.BotToken = alliance.DiscordToken;

                            SendToMQ("AllianceMessage", SendingMessage);
                        }
                    }
                }
                if (DateTime.Now > chat)
                {
                    try
                    {
                        foreach (var id in AllianceChat.PeopleInAllianceChat.Keys)
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
                    if (DateTime.Now > NextUpdate)
                    {
                        NextUpdate = DateTime.Now.AddMinutes(8);

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
                            LoadAllAlliances();

                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    }
                }
                if (ticks % 32 == 0 && TorchState == TorchSessionState.Loaded)
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
                if (ticks % 128 == 0)
                {

                    try
                    {
                        foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                        {

                            if (player.GetPosition() != null)
                            {
                                try
                                {
                                    foreach (var ter in Territories.Values)
                                    {
                                        if (ter.Enabled)
                                        {
                                            if (Vector3.Distance(player.GetPosition(), ter.Position) <= ter.Radius)
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
            catch (Exception)
            {
            }


        }

        public static void SendChatMessage(String prefix, String message, ulong steamID = 0)
        {
            var _chatLog = LogManager.GetLogger("Chat");
            var scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = prefix;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = Color.OrangeRed;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId(steamID);
            var scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
        public static string GetPlayerName(ulong steamId)
        {
            var id = GetIdentityByNameOrId(steamId.ToString());
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
                if (ulong.TryParse(playerNameOrSteamId, out var steamId))
                {
                    var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
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
