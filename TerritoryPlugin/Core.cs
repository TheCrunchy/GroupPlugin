using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CrunchGroup.Handlers;
using CrunchGroup.NexusStuff;
using CrunchGroup.NexusStuff.V3;
using CrunchGroup.Territories;
using CrunchGroup.Territories.CapLogics;
using CrunchGroup.Territories.Interfaces;
using CrunchGroup.Territories.Models;
using CrunchGroup.Territories.SecondaryLogics;
using HarmonyLib;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Session;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace CrunchGroup
{
    public class Core : TorchPluginBase
    {
        public static List<Assembly> myAssemblies { get; set; } = new List<Assembly>();
        public static List<ComponentCost> repairCost = new List<ComponentCost>();
        public static Dictionary<String, ComponentCost> ComponentCosts = new Dictionary<string, ComponentCost>();
        public static MethodInfo sendChange;
        public static ITorchSession Session;
        public static Action UpdateCycle;
        public static NexusGlobalAPI NexusGlobalAPI = new NexusGlobalAPI();

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

        public const string PluginName = "Groups";
        public const string PluginCommandPrefix = "group";

        public static Random random = new Random();

        public static TorchSessionState TorchState;
        private TorchSessionManager sessionManager;
        public static Config config;
        public static string path;
        public static string basePath;
        public static Logger Log = LogManager.GetLogger(PluginName);
        public DateTime NextUpdate = DateTime.Now;

        public static FileUtils utils = new FileUtils();
        public static ITorchPlugin GridBackup;
        public static ITorchPlugin MQ;
        public static ITorchPlugin SKO;
        public static MethodInfo BackupGrid;

        public static ITorchBase TorchBase;
        public static bool GridBackupInstalled = false;
        public static MethodInfo SendMessage;

        public static bool MQPluginInstalled = false;

        public static bool InitPlugins = false;
        public static NexusAPI API { get; private set; }
        public static bool NexusInstalled { get; private set; } = false;

        private static readonly Guid NexusGUID = new Guid("28a12184-0422-43ba-a6e6-2e228611cca5");

        public static void InitPluginDependencies(PluginManager Plugins, PatchManager Patches)
        {
            InitPlugins = true;
            if (Plugins.Plugins.TryGetValue(Guid.Parse("75e99032-f0eb-4c0d-8710-999808ed970c"), out var GridBackupPlugin))
            {
                BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                GridBackup = GridBackupPlugin;
                GridBackupInstalled = true;
            }
            if (Plugins.Plugins.TryGetValue(Guid.Parse("34d803f7-42f9-450e-a942-6ed06abdc011"), out var Sko))
            {
                SKO = Sko;
                SkoTweaksPatch.Patch(Patches.AcquireContext());
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
                        $"{PluginName}"
                    });
                    API = new NexusAPI(4398);

                    NexusInstalled = true;
                }
            }

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(4398, new Action<ushort, byte[], ulong, bool>(NexusHandler.HandleNexusMessage));
        }
        public void SetupFriendMethod()
        {
            var FactionCollection = MySession.Static.Factions.GetType().Assembly.GetType("Sandbox.Game.Multiplayer.MyFactionCollection");
            sendChange = FactionCollection?.GetMethod("SendFactionChange", BindingFlags.NonPublic | BindingFlags.Static);
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

            TorchBase = Torch;
        }

        public void SetupConfig()
        {
            var utils = new FileUtils();
            path = StoragePath;
            if (File.Exists(StoragePath + $"\\{PluginName}.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + $"\\{PluginName}.xml");
                utils.WriteToXmlFile<Config>(StoragePath + $"\\{PluginName}.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + $"\\{PluginName}.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + $"\\{PluginName}");
            }
            else
            {
                folder = config.StoragePath;
            }
            var folder2 = "";
            Directory.CreateDirectory(folder);

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
            config = utils.ReadFromXmlFile<Config>(basePath + $"\\{PluginName}.xml");
            return config;

        }

        public static void saveConfig()
        {
            var utils = new FileUtils();

            utils.WriteToXmlFile<Config>(basePath + $"\\{PluginName}.xml", config);

            return;
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

        public static bool CompileFailed = false;
        public static Random rand = new Random();
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            Session = session;

            if (state == TorchSessionState.Unloading)
            {

                // DiscordStuff.DisconnectDiscord();
                foreach (var ter in Core.Territories.Values)
                {
                    Core.utils.WriteToJsonFile<Territories.Models.Territory>(Core.path + "//Territories//" + ter.Name + ".json", ter);
                }

                TorchState = TorchSessionState.Unloading;
            }

            if (state != TorchSessionState.Loaded) return;
            NexusHandler.Setup();
            SetupFriendMethod();
            Storage.SetupStorage();
            Directory.CreateDirectory($"{path}//Territories//");

            if (!File.Exists(path + "//Territories//Example.json"))
            {
                var example = new Territories.Models.Territory();
                var logic = new FactionGridCapLogic();

                logic.SecondaryLogics = new List<ISecondaryLogic>();

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
                var loot = new LootLogic();
                loot.Loot = new List<LootLogic.LootItem>();
                loot.Loot.Add(new LootLogic.LootItem());
                var craft = new LootConverter();
                var item = new CraftedItem();
                item.typeid = "Ore";
                item.subtypeid = "Iron";
                item.amountPerCraft = 500;
                item.chanceToCraft = 1;
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
                logic.SecondaryLogics.Add(new UpkeepLogic()
                {
                    UpkeepItems = new List<UpkeepItem>()
                {
                    new UpkeepItem(){typeid = "Ingot",subtypeid = "Iron", amount = 10000},
                }
                });
                var printer = new GridPrinterLogic();

                logic.SecondaryLogics.Add(printer);
                example.CapturePoints.Add(logic);
                example.Enabled = false;

                utils.WriteToJsonFile<Territories.Models.Territory>(path + "//Territories//Example.json", example, false);


            }
            repairCost = new List<ComponentCost>();
            if (File.Exists($"{path}/ComponentCosts.json"))
            {
                repairCost = utils.ReadFromJsonFile<List<ComponentCost>>($"{path}/ComponentCosts.json");
            }
            AddComponentCostToDictionary();
            AddComponentCost("AdminKit", 5000000, true);
            AddComponentCost("AdminComponent", 5000000, true);
            Directory.CreateDirectory($"{Core.path}/Scripts/");
            try
            {
                Compiler.Compile($"{Core.path}/Scripts/");
                Core.Log.Error("Compile happened");
            }

            catch (Exception e)
            {
                Core.Log.Error($"compile error {e}");
            }

            if (!CompileFailed)
            {
                Core.Log.Error("Apply keen stuff");
                var patchManager = Session.Managers.GetManager<PatchManager>();
                var context = patchManager.AcquireContext();
                KeenScriptManagerPatch.ScriptInit(MySession.Static);
                patchManager.Commit();
            }

            LoadAllTerritories();
        }

        public static DateTime nextRegister = DateTime.Now.AddSeconds(60);

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

        public static List<string> KnownPaths = new List<string>();
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
            if (CompileFailed)
            {
                Core.Log.Info("Compile failed, territories cannot be loaded.");
                return;
            }

            Territories.Clear();
            var jsonStuff = new FileUtils();
            foreach (var s in Directory.GetFiles(path + "//Territories//"))
            {
                try
                {
                    var ter = new Territories.Models.Territory();
                    if (s.EndsWith(".xml"))
                    {
                        continue;
                    }
                    if (s.EndsWith(".json"))
                    {
                        ter = jsonStuff.ReadFromJsonFile<Territories.Models.Territory>(s);
                    }
                    if (!ter.Enabled)
                    {
                        continue;
                    }

                    if (Territories.ContainsKey(ter.Id))
                    {
                        Log.Info($"Duplicate territory ID at {s}");
                        continue;
                    }
                    Territories.Add(ter.Id, ter);
            
                }
                catch (Exception ex)
                {
                    Log.Error("Error reading territory " + s);
                    Log.Info(ex);
                }
            }
            AttachFileWatcher(path + "//Territories//");

        }
        private static Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();
        private static void AttachFileWatcher(string filePath)
        {
            if (watchers.ContainsKey(filePath))
                return;

            var watcher = new FileSystemWatcher(filePath)
            {
                Filter = filePath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            watcher.Changed += (sender, e) => OnFileChanged(filePath);
            watcher.Created += (sender, e) => OnFileChanged(filePath);
            watcher.EnableRaisingEvents = true;
            watchers.Add(filePath, watcher);
        }
        private static void OnFileChanged(string filePath)
        {
            try
            {
                var jsonStuff = new FileUtils();
                var ter = jsonStuff.ReadFromJsonFile<Territory>(filePath);
                if (!ter.Enabled)
                {
                    if (Territories.ContainsKey(ter.Id))
                    {
                        Territories.Remove(ter.Id);
                        Log.Info($"Territory {ter.Id} disabled and removed.");
                    }
                    return;
                }

                Territories[ter.Id] = ter;
                Log.Info($"Territory {ter.Id} updated.");
            }
            catch (Exception ex)
            {
                Log.Error("Error reloading territory " + filePath);
                Log.Info(ex);
            }
        }

        public static string WorldName = "";

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


        public static Dictionary<long, DateTime> messageCooldowns = new Dictionary<long, DateTime>();




        public static bool Paused = false;

        public static Dictionary<Guid, Territories.Models.Territory> Territories = new Dictionary<Guid, Territories.Models.Territory>();

        public static List<Territory> GetAllTerritories()
        {
            return Territories.Values.ToList();
        }


        public static DateTime chat = DateTime.Now;


        private FileUtils jsonStuff = new FileUtils();
        DateTime NextSave = DateTime.Now;
        public override void Update()
        {
            ticks++;

            try
            {
                UpdateCycle?.Invoke();

                if (!InitPlugins)
                {

                    InitPlugins = true;
                    InitPluginDependencies(Torch.Managers.GetManager<PluginManager>(), Torch.Managers.GetManager<PatchManager>());

                }

                if (ticks % 60 == 0)
                {
                    try
                    {
                        GroupHandler.DoGroupLoop();
                    }
                    catch (Exception e)
                    {
                        Core.Log.Error($"Error in group loop { e.ToString()}");
                    }
                    try
                    {
                        CaptureHandler.DoCaps();
                    }
                    catch (Exception e)
                    {
                        Core.Log.Error($"Error in territory cap logic {e.ToString()}");
                    }

                    if (DateTime.Now > NextSave)
                    {
                        NextSave = DateTime.Now.AddMinutes(5);
                        foreach (var ter in Territories)
                        {
                            if (Core.NexusInstalled)
                            {
                                if (NexusAPI.GetServerIDFromPosition(ter.Value.Position) != NexusAPI.GetThisServer().ServerID)
                                {
                                    continue;
                                }
                            }
                            Task.Run(() =>
                            {
                                Core.utils.WriteToJsonFile<Territories.Models.Territory>(
                                    Core.path + "//Territories//" + ter.Value.Name + ".json", ter.Value);
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SendChatMessage(String prefix, String message, ulong steamID = 0, Color color = default)
        {
            if (color == default)
            {
                color = Color.OrangeRed;
            }
            var _chatLog = LogManager.GetLogger("Chat");
            var scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = prefix;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
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
