using AlliancesPlugin.Alliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.KamikazeTerritories;
using AlliancesPlugin.Territory_Version_2.CapLogics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Integrations
{
    public static class AllianceIntegrationCore
    {
        public static Guid GetAllianceId(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return Guid.Empty;
            }
            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance != null) return alliance.AllianceId;
            alliance = AlliancePlugin.GetAlliance(faction);
            return alliance == null ? Guid.Empty : alliance.AllianceId;
        }

        public static Alliance GetAllianceObj(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return null;
            }
            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance != null) return alliance;
            alliance = AlliancePlugin.GetAlliance(faction);
            return alliance ?? null;
        }
        public static List<long> GetAllianceMembers(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return new List<long>();
            }
          
            var alliance = AlliancePlugin.GetAlliance(faction);
            if (alliance == null) return new List<long>();

            return alliance.AllianceMembers;
        }

        public static int GetMaximumForShipClassType(string factionTag, string classType)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return 0;
            }

            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance == null)
            {
                return 0;
            }
            alliance.LoadShipClassLimits();
            return alliance.GetShipClassLimit(classType);
        }

        public static bool DoesPlayerHavePermission(long PlayerIdentityId, string Permission)
        {

            var fac = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);
            if (fac == null)
            {
                return false;
            }

            var alliance = AlliancePlugin.GetAllianceNoLoading(fac);
            if (alliance == null)
            {
                return false;
            }

            if (!Enum.TryParse(Permission, out AccessLevel level)) return false;
            var steamid = MySession.Static.Players.TryGetSteamId(PlayerIdentityId);
            return alliance.HasAccess(steamid, level);
        }

        public static double GetRefineryYieldMultiplier(long PlayerId, MyRefinery Refin)
        {
            return MyProductionPatch.GetRefineryYieldMultiplier(PlayerId, Refin);
        }

        public static double GetAssemblerSpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            return MyProductionPatch.GetAssemblerSpeedMultiplier(PlayerId, Assembler);
        }
        public static double GetRefinerySpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            return MyProductionPatch.GetAssemblerSpeedMultiplier(PlayerId, Assembler);
        }


        public static void ReceiveModMessage(byte[] data)
        {
            var Data = MyAPIGateway.Utilities.SerializeFromBinary<ModMessage>(data);

            switch (Data.Type)
            {
                case "DataRequest":
                    var request = MyAPIGateway.Utilities.SerializeFromBinary<DataRequest>(Data.Member);
                    switch (request.DataType)
                    {
                        case "Territory":
                            SendPlayerTerritories(request.SteamId);
                            //  AlliancePlugin.Log.Info($"Sending territories to {request.SteamId}");
                            break;
                        case "WarStatus":
                            //    AlliancePlugin.Log.Info($"Sending war status to {request.SteamId}");
                            SendPlayerWarStatus(request.SteamId);
                            SendPlayerHudStatus(request.SteamId);
                            AllianceCommands.SendStatusToClient(request.SteamId);
                            break;
                        case "Hud":
                            //    AlliancePlugin.Log.Info($"Sending war status to {request.SteamId}");
                            SendPlayerHudStatus(request.SteamId);
                            break;
                        case "Chat":
                            //    AlliancePlugin.Log.Info($"Sending war status to {request.SteamId}");
                          //  AllianceChat.AddOrRemoveToChat(request.SteamId);
                            break;
                    }
                    break;

            }
        }

        public static void SendAllAllianceMemberDataToMods()
        {
            if (AlliancePlugin.AllAlliances == null) return;
            var data = new List<AllianceData>();
            foreach (var sending in AlliancePlugin.AllAlliances.Select(alliance => new AllianceData()
            {
                AllianceName = alliance.Value.name,
                AllianceId = alliance.Value.AllianceId,
                FactionIds = alliance.Value.AllianceMembers
            }))
            {
                data.Add(sending);
            }

            var modmessage = new ModMessage()
            {
                Type = "AllianceLists",
                Member = MyAPIGateway.Utilities.SerializeToBinary(data)
            };

            var binaryData = MyAPIGateway.Utilities.SerializeToBinary(modmessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(8544, binaryData);
        }

        public static void SendPlayerHudStatus(ulong steamPlayerId)
        {

            var message = new BoolStatus
            {
                Enabled = AlliancePlugin.config.EnablePvPAreaHud
            };


            var statusM = MyAPIGateway.Utilities.SerializeToBinary(message);
            var modmessage = new ModMessage()
            {
                Type = "HudStatus",
                Member = statusM
            };

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(modmessage);

            var binaryData = MyAPIGateway.Utilities.SerializeToBinary(modmessage);
            MyAPIGateway.Multiplayer.SendMessageTo(8544, binaryData, steamPlayerId);
        }
        public static void SendPlayerWarStatus(ulong steamPlayerId)
        {
            var id = AlliancePlugin.GetIdentityByNameOrId(steamPlayerId.ToString());
            if (id == null)
            {
                return;
            }
            IMyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);

            var message = new BoolStatus
            {
                Enabled = playerFac == null ||
                          AlliancePlugin.warcore.participants.FactionsAtWar.Contains(playerFac.FactionId)
            };

            if (!AlliancePlugin.config.DisablePvP || !AlliancePlugin.config.EnableOptionalWar)
            {
                message.Enabled = true;
            }

            var statusM = MyAPIGateway.Utilities.SerializeToBinary(message);
            var modmessage = new ModMessage()
            {
                Type = "WarStatus",
                Member = statusM
            };

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(modmessage);

            var binaryData = MyAPIGateway.Utilities.SerializeToBinary(modmessage);
            MyAPIGateway.Multiplayer.SendMessageTo(8544, binaryData, steamPlayerId);
        }
        public static void SendPlayerTerritories(ulong steamPlayerId)
        {
            var id = AlliancePlugin.GetIdentityByNameOrId(steamPlayerId.ToString());
            if (id == null)
            {
                return;
            }
            IMyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);

            var message = new PlayerDataPvP
            {
                PvPAreas = new List<PvPArea>()
            };


            foreach (var area in MessageHandler.Territories.Select(Territory => new PvPArea
            {
                Name = Territory.Name ?? "PvP Area",
                Position = Territory.Position,
                Distance = Territory.Radius,
                AreaForcesPvP = Territory.ForcesPvP
            }))
            {
                message.PvPAreas.Add(area);
            }

            foreach (var territory in AlliancePlugin.Territories)
            {
                message.PvPAreas.Add(new PvPArea()
                {
                    AreaForcesPvP = territory.Value.ForcesPvP,
                    Name = territory.Value.Name,
                    Position = territory.Value.Position,
                    Distance = territory.Value.Radius
                });

                foreach (var capture in territory.Value.CapturePoints)
                {
                    if (capture is AllianceGridCapLogic gridcap)
                    {
                        message.PvPAreas.Add(new PvPArea()
                        {
                            AreaForcesPvP = true,
                            Name = gridcap.PointName,
                            Position = gridcap.GPSofPoint,
                            Distance = gridcap.CaptureRadius
                        });
                    }
                }
            }

            var statusM = MyAPIGateway.Utilities.SerializeToBinary(message);
            var modmessage = new ModMessage()
            {
                Type = "PvPAreas",
                Member = statusM
            };

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(modmessage);

            var binaryData = MyAPIGateway.Utilities.SerializeToBinary(modmessage);
            MyAPIGateway.Multiplayer.SendMessageTo(8544, binaryData, steamPlayerId);
        }
    }
}
