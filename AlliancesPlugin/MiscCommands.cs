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

namespace AlliancesPlugin
{
    class MiscCommands : CommandModule
    {
        public static Dictionary<long, DateTime> distressCooldowns = new Dictionary<long, DateTime>();

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
                AllianceChat.SendChatMessage(alliance.AllianceId, "Distress Signal", CreateGps(Context.Player.Character.GetPosition(), Color.Yellow, 600, Context.Player.Character.DisplayName, reason).ToString(), true);
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
