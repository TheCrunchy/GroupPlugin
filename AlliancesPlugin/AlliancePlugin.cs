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

namespace AlliancesPlugin
{
    public class AlliancePlugin : TorchPluginBase
    {
        public static MethodInfo sendChange;
        TorchSessionState TorchState;
        private TorchSessionManager sessionManager;
        public static Config config;
        public static string path;
        Logger log = LogManager.GetLogger("Alliances");
        public DateTime NextUpdate = DateTime.Now;
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            SetupConfig();
            path = CreatePath();
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
            string fileName = "Alliances";
            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + "//Alliances//");
            }
            else
            {
                folder = config.StoragePath;
            }


            Directory.CreateDirectory(folder);

            return folder;
        }
        public static Config LoadConfig()
        {
            FileUtils utils = new FileUtils();
            config = utils.ReadFromXmlFile<Config>(path + "\\Alliances.xml");

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
            jsonStuff.WriteToJsonFile<Alliance>(path, alliance);
        }
        public static Alliance LoadAllianceData(String name)
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                Alliance alliance = jsonStuff.ReadFromJsonFile<Alliance>(path + "//" + name + ".json");
                return alliance;
            }
            catch
            {
                return null;
            }
        }
        public static Alliance GetAlliance(string name)
        {
            //fuck it lets just return something that might be null
            Alliance temp = LoadAllianceData(name);
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
                return LoadAllianceData(FactionsInAlliances[fac.FactionId]);
            }

          foreach (KeyValuePair<String,Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceMembers.Contains(fac.FactionId))
                {
                    
                    return LoadAllianceData(alliance.Value.name);
                }
            }
            return null;
        }
        public void SetupFriendMethod()
        {
            Type FactionCollection = MySession.Static.Factions.GetType().Assembly.GetType("Sandbox.Game.Multiplayer.MyFactionCollection");
            sendChange = FactionCollection?.GetMethod("SendFactionChange", BindingFlags.NonPublic | BindingFlags.Static);
        }
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            if (state == TorchSessionState.Loaded)
            {


                TorchState = TorchSessionState.Loaded;

                SetupFriendMethod();

                LoadAllAlliances();
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
                    foreach (String s in Directory.GetFiles(path))
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
                    log.Error(ex);
                }
                foreach (Alliance alliance in AllAlliances.Values)
                {
                    alliance.ForceFriendlies();
                    alliance.ForceEnemies();
                }
            }
        }

        public override void Update()
        {
            ticks++;
            if (ticks % 128 == 0)
            {

                if (DateTime.Now > NextUpdate)
                {

                    NextUpdate = DateTime.Now.AddMinutes(1);
                    LoadAllAlliances();
                }

            }
        }
    }
}
