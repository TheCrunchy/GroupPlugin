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
using VRageMath;

namespace AlliancesPlugin
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

        public static List<ulong> PeopleInAllianceChat = new List<ulong>();
        public static void DoChatMessage(TorchChatMessage msg, ref bool consumed)
        {
            if (msg.Message.StartsWith("!"))
            {
                return;
            }
            if (PeopleInAllianceChat.Contains((ulong)msg.AuthorSteamId))
            {

                if (AlliancePlugin.playersAllianceId.ContainsKey((ulong)msg.AuthorSteamId))
                {
                    consumed = true;
                    Guid allianceId = AlliancePlugin.playersAllianceId[(ulong)msg.AuthorSteamId];

                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
                    List<ulong> OtherMembers = AlliancePlugin.playersInAlliances[allianceId];

                    // ShipyardCommands.SendMessage(msg.Author, "You are in alliance chat", Color.BlueViolet, (long)msg.AuthorSteamId);
                    foreach (ulong id in OtherMembers)
                    {
                        ShipyardCommands.SendMessage(alliance.GetTitle((ulong)msg.AuthorSteamId) + " | " + msg.Author, msg.Message, new Color(66, 163, 237), (long)id);

                        MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                        if (ScanChat(msg.Message, null) != null)
                        {
                            MyGps gpsRef = ScanChat(msg.Message, null);
                            gpsRef.GPSColor = Color.Yellow;
                            gpsRef.AlwaysVisible = true;
                            gpsRef.ShowOnHud = true;

                            long idenId = MySession.Static.Players.TryGetIdentityId(id);
                            gpscol.SendAddGps(idenId, ref gpsRef);
                        }
                    }
                }
                else
                {
                    PeopleInAllianceChat.Remove((ulong)msg.AuthorSteamId);
                }
            }

        }
        public static void Login(IPlayer p)
        {
            if (p == null)
            {
                return;
            }
            AlliancePlugin.Log.Info("Login?");
            MyIdentity id = AlliancePlugin.GetIdentityByNameOrId(p.SteamId.ToString());
            if (id == null)
            {
                return;
            }
            AlliancePlugin.Log.Info("got id");
            if (MySession.Static.Factions.GetPlayerFaction(id.IdentityId) != null && AlliancePlugin.FactionsInAlliances.ContainsKey(MySession.Static.Factions.GetPlayerFaction(id.IdentityId).FactionId))
            {
                AlliancePlugin.Log.Info("faction isnt null and alliances thing contains it");
                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(id.IdentityId) as MyFaction);
                if (AlliancePlugin.playersInAlliances.ContainsKey(alliance.AllianceId))
                {

                    if (!AlliancePlugin.playersInAlliances[alliance.AllianceId].Contains(p.SteamId))
                    {

                        AlliancePlugin.Log.Info("Adding them to the list?");
                        AlliancePlugin.playersInAlliances[alliance.AllianceId].Add(p.SteamId);
                        AlliancePlugin.playersAllianceId.Add(p.SteamId, alliance.AllianceId);
                    }
                }
                else
                {
                    AlliancePlugin.Log.Info("Doesnt");
                }
                //else
                //{
                //    List<ulong> bob = new List<ulong>();
                //    bob.Add(p.SteamId);
                //    AlliancePlugin.playersInAlliances.Add(alliance.AllianceId, bob);
                //    AlliancePlugin.playersAllianceId.Add(p.SteamId, alliance.AllianceId);
                //}
            }
        }


        public static void Logout(IPlayer p)
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
            if (MySession.Static.Factions.TryGetFactionById(id.IdentityId) != null && AlliancePlugin.FactionsInAlliances.ContainsKey(MySession.Static.Factions.TryGetFactionById(id.IdentityId).FactionId))
            {
                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(id.IdentityId) as MyFaction);
                if (AlliancePlugin.playersInAlliances.ContainsKey(alliance.AllianceId))
                {
                    if (AlliancePlugin.playersInAlliances[alliance.AllianceId].Contains(p.SteamId))
                    {
                        AlliancePlugin.playersInAlliances[alliance.AllianceId].Remove(p.SteamId);
                    }
                }
            }

        }
    }
}
