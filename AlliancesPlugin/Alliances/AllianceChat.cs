using AlliancesPlugin.Shipyard;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    public static class AllianceChat
    {
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

        public static Dictionary<ulong, Guid> PeopleInAllianceChat = new Dictionary<ulong, Guid>();
        public static void SendChatMessage(Guid allianceId, string prefix, string message, bool toDiscord)
        {
            prefix = prefix.Replace(":", "");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
            List<ulong> OtherMembers = new List<ulong>();


            bool DiscordSent = false;
            if (toDiscord && DiscordStuff.AllianceHasBot(allianceId))
            {
                try
                {
                    DiscordStuff.SendAllianceMessage(alliance, prefix, message);
                    DiscordSent = true;
                }
                catch (Exception)
                {
                }
            }
           

            if (!DiscordSent)
            {
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    MyFaction fac = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId) as MyFaction;
                    if (fac != null)
                    {
                        if (alliance.AllianceMembers.Contains(fac.FactionId))
                        {
                            OtherMembers.Add(player.Id.SteamId);
                        }
                    }

                }
                foreach (ulong id in OtherMembers)
                {

                    ShipyardCommands.SendMessage(prefix, message, new Color(66, 163, 237), (long)id);
                    MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                    if (ScanChat(message, null) != null)
                    {
                        MyGps gpsRef = ScanChat(message, null);
                        gpsRef.GPSColor = Color.Yellow;
                        gpsRef.AlwaysVisible = true;
                        gpsRef.ShowOnHud = true;

                        long idenId = MySession.Static.Players.TryGetIdentityId(id);
                        gpscol.SendAddGps(idenId, ref gpsRef);
                    }
                }

            }
        }
        public static void SendChatMessage(Guid allianceId, string prefix, string message)
        {

            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
            List<ulong> OtherMembers = new List<ulong>();

            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                MyFaction fac = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId) as MyFaction;
                if (fac != null)
                {
                    if (alliance.AllianceMembers.Contains(fac.FactionId))
                    {
                        OtherMembers.Add(player.Id.SteamId);
                    }
                }

            }
            foreach (ulong id in OtherMembers)
            {

                ShipyardCommands.SendMessage(prefix, message, new Color(66, 163, 237), (long)id);
                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                if (ScanChat(message, null) != null)
                {
                    MyGps gpsRef = ScanChat(message, null);
                    gpsRef.GPSColor = Color.Yellow;
                    gpsRef.AlwaysVisible = true;
                    gpsRef.ShowOnHud = true;

                    long idenId = MySession.Static.Players.TryGetIdentityId(id);
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        gpscol.SendAddGps(idenId, ref gpsRef);
                    });

                }
            }
        }
        public static void DoChatMessage(TorchChatMessage msg, ref bool consumed)
        {
            if (msg.AuthorSteamId == null)
            {
                return;
            }
            if (msg.Message.StartsWith("!"))
            {
                return;
            }

            if (PeopleInAllianceChat.ContainsKey((ulong)msg.AuthorSteamId))
            {


                consumed = true;
                Guid allianceId = PeopleInAllianceChat[(ulong)msg.AuthorSteamId];
                List<ulong> OtherMembers = new List<ulong>();

                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    MyFaction fac = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId) as MyFaction;
                    if (fac != null)
                    {
                        if (alliance.AllianceMembers.Contains(fac.FactionId))
                        {
                            OtherMembers.Add(player.Id.SteamId);
                        }
                    }

                }

                // ShipyardCommands.SendMessage(msg.Author, "You are in alliance chat", Color.BlueViolet, (long)msg.AuthorSteamId);
                SendChatMessage(allianceId, alliance.GetTitle((ulong)msg.AuthorSteamId) + " | " + msg.Author, msg.Message, true);
            }
            else
            {
                //  PeopleInAllianceChat.Remove((ulong)msg.AuthorSteamId);
            }


        }
        public static void Login(IPlayer p)
        {
            if (p == null)
            {
                return;
            }
            MyIdentity id = AlliancePlugin.GetIdentityByNameOrId(p.SteamId.ToString());
            if (id == null)
            {
                return;
            }
            IMyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            MyFaction arrr = MySession.Static.Factions.TryGetFactionByTag("arrr");
            if (arrr != null)
            {
                if (playerFac != null && !MySession.Static.Factions.AreFactionsEnemies(arrr.FactionId, FacUtils.GetPlayersFaction(id.IdentityId).FactionId))
                {
                    Sandbox.Game.Multiplayer.MyFactionCollection.DeclareWar(playerFac.FactionId, arrr.FactionId);
                }
            }

            MyFaction ACME = MySession.Static.Factions.TryGetFactionByTag("ACME");

            if (ACME != null)
            {
                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(id.IdentityId, ACME.FactionId, 0);
                MySession.Static.Factions.AddFactionPlayerReputation(id.IdentityId, ACME.FactionId, 0);
            }
            MyFaction wolf = MySession.Static.Factions.TryGetFactionByTag("WOLF");
            if (wolf != null)
            {
                if (playerFac != null && !MySession.Static.Factions.AreFactionsEnemies(wolf.FactionId, FacUtils.GetPlayersFaction(id.IdentityId).FactionId))
                {
                    Sandbox.Game.Multiplayer.MyFactionCollection.DeclareWar(playerFac.FactionId, wolf.FactionId);
                }
            }
        }

        public static void Logout(IPlayer p)
        {
            //if (p == null)
            //{
            //    return;
            //}

            //MyIdentity id = AlliancePlugin.GetIdentityByNameOrId(p.SteamId.ToString());
            //if (id == null)
            //{
            //    return;
            //}
            //if (MySession.Static.Factions.TryGetFactionById(id.IdentityId) != null && AlliancePlugin.FactionsInAlliances.ContainsKey(MySession.Static.Factions.TryGetFactionById(id.IdentityId).FactionId))
            //{
            //    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(id.IdentityId) as MyFaction);
            //    if (AlliancePlugin.playersInAlliances.ContainsKey(alliance.AllianceId))
            //    {
            //        if (AlliancePlugin.playersInAlliances[alliance.AllianceId].Contains(p.SteamId))
            //        {
            //            AlliancePlugin.playersInAlliances[alliance.AllianceId].Remove(p.SteamId);
            //        }
            //    }
            //}

        }
    }
}
