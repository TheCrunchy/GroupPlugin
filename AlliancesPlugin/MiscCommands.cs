using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using AlliancesPlugin.Alliances;
using Sandbox.Game.Screens.Helpers;
using VRageMath;
using System.IO;

namespace AlliancesPlugin
{
   public class MiscCommands : CommandModule
    {
        public static Dictionary<long, DateTime> distressCooldowns = new Dictionary<long, DateTime>();
        public static Dictionary<long, int> distressAmounts = new Dictionary<long, int>();
        [Command("distress", "distress signals")]
        [Permission(MyPromoteLevel.None)]
        public void distress(string reason = "")
        {


            if (Context.Player == null)
            {
                Context.Respond("no no console no distress");
                return;
            }


            IMyFaction playerFac = FacUtils.GetPlayersFaction(Context.Player.Identity.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("You dont have a faction.");
                return;
            }
            if (reason != "")
            {
                reason = Context.RawArgs;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("A faction is required to use alliance features.");
                return;

            }

            if (distressCooldowns.TryGetValue(Context.Player.IdentityId, out DateTime time))
            {
                if (DateTime.Now < time)
                {
                    Context.Respond(AllianceCommands.GetCooldownMessage(time));
                    return;
                }
                else
                {
                    distressCooldowns[Context.Player.IdentityId] = DateTime.Now.AddSeconds(30);
                }
            }
            else
            {
                distressCooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(30));

            }
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(fac);
            if (alliance != null)
            {
                if (distressAmounts.ContainsKey(Context.Player.IdentityId)) {
                    distressAmounts[Context.Player.IdentityId] += 1;
                    AllianceChat.SendChatMessage(alliance.AllianceId, "Distress Signal", CreateGps(Context.Player.Character.GetPosition(), Color.Yellow, 600, Context.Player.Character.DisplayName + " " + distressAmounts[Context.Player.IdentityId], reason).ToString(), true);
                }
                else {
                    AllianceChat.SendChatMessage(alliance.AllianceId, "Distress Signal", CreateGps(Context.Player.Character.GetPosition(), Color.Yellow, 600, Context.Player.Character.DisplayName, reason).ToString(), true);
                    distressAmounts.Add(Context.Player.IdentityId, 1);
                }
          
            }
        }
        FileUtils utils = new FileUtils();
        [Command("al chat", "toggle alliance chat")]
        [Permission(MyPromoteLevel.None)]
        public void DoAllianceChat(string message = "")
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            PlayerData data;
            if (File.Exists(AlliancePlugin.path + "//PlayerData//" + Context.Player.SteamUserId + ".xml"))
            {

                data = utils.ReadFromXmlFile<PlayerData>(AlliancePlugin.path + "//PlayerData//" + Context.Player.SteamUserId + ".xml");
            }
            else
            {
                data = new PlayerData();
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (AllianceChat.PeopleInAllianceChat.ContainsKey(Context.Player.SteamUserId))
            {
                data.InAllianceChat = false;
                AllianceChat.PeopleInAllianceChat.Remove(Context.Player.SteamUserId);
                Context.Respond("Leaving alliance chat.", Color.Red);
                utils.WriteToXmlFile<PlayerData>(AlliancePlugin.path + "//PlayerData//" + Context.Player.SteamUserId + ".xml", data);
                return;
            }
            if (alliance != null)
            {
                {
                    data.InAllianceChat = true;
                    AllianceChat.PeopleInAllianceChat.Add(Context.Player.SteamUserId, alliance.AllianceId);
                    Context.Respond("Entering alliance chat.", Color.Cyan);
                    utils.WriteToXmlFile<PlayerData>(AlliancePlugin.path + "//PlayerData//" + Context.Player.SteamUserId + ".xml", data);
                }
            }
            else
            {
                Context.Respond("You must be in an alliance to use alliance chat.");
            }
        }
        private MyGps CreateGps(Vector3D Position, Color gpsColor, int seconds, String Nation, String Reason)
        {

            MyGps gps = new MyGps
            {
                Coords = Position,
                Name = Nation + " - Distress Signal ",
                DisplayName = Nation + " - Distress Signal ",
                GPSColor = gpsColor,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, seconds, 0),
                Description = "Nation Distress Signal \n" + Reason,
            };
            gps.UpdateHash();


            return gps;
        }
    }
   

}
