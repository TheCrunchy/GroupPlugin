using System;
using System.Collections.Generic;
using System.Linq;
using CrunchGroup;
using CrunchGroup.Models;
using CrunchGroup.Territories.CapLogics;
using CrunchGroup.Territories.Models;
using CrunchGroup.Territories.PointOwners;
using ProtoBuf;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;
using VRageMath;

namespace GroupMiscellenious.Scripts
{
    [PatchShim]
    public class TerritoryHudScript
    {
        private static int _ticks;
        public static DateTime NextSendTerritory = DateTime.Now.AddMinutes(5);
        private static bool hasTerritories = false;
        public static void Patch(PatchContext ctx)
        {
            Core.UpdateCycle += UpdateExample;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8544, ReceiveModMessage);
        }

        public static void UpdateExample()
        {
            _ticks++;

            if (_ticks % 128 != 0)
                return;

            if (DateTime.Now <= NextSendTerritory)
                return;

            NextSendTerritory = DateTime.Now.AddMinutes(5);

            // Build the packet ONCE
            var packet = BuildTerritoryPacket();

            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                MyAPIGateway.Multiplayer.SendMessageTo(8544, packet, player.Id.SteamId);
        }

        public static void SendPlayerHudStatus(ulong steamPlayerId)
        {
            SendMessage(steamPlayerId, "HudStatus", new BoolStatus
            {
                Enabled = true
            });
        }

        public static void SendPlayerTerritories(ulong steamPlayerId)
        {
            MyAPIGateway.Multiplayer.SendMessageTo(
                8544,
                BuildTerritoryPacket(),
                steamPlayerId);
            if (hasTerritories)
            {
                SendPlayerHudStatus(steamPlayerId);
            }
        }

        private static byte[] BuildTerritoryPacket()
        {
            var message = new PlayerDataPvP
            {
                PvPAreas = new List<PvPArea>()
            };
            hasTerritories = Core.Territories.Any();
            foreach (var territory in Core.Territories)
            {
                message.PvPAreas.Add(new PvPArea
                {
                    Name = territory.Value.Name ?? "PvP Area",
                    Position = territory.Value.Position,
                    Distance = territory.Value.RadiusDistance,
                    OwnerName = GetOwnerName(territory.Value.Owner)
                });

                foreach (var capture in territory.Value.CapturePoints)
                {
                    var area = new PvPArea
                    {
                        Name = capture.PointName,
                        OwnerName = GetOwnerName(capture.PointOwner)
                    };

                    if (capture is FactionGridCapLogic gridCap)
                    {
                        area.Position = gridCap.GPSofPoint;
                        area.Distance = gridCap.CaptureRadius;
                    }
                    else
                    {
                        area.Position = capture.GetPointsLocationIfSet();
                        area.Distance = 10000;
                    }

                    message.PvPAreas.Add(area);
                }
            }

            return MyAPIGateway.Utilities.SerializeToBinary(
                new ModMessage
                {
                    Type = "PvPAreas",
                    Member = MyAPIGateway.Utilities.SerializeToBinary(message)
                });
        }

        private static void SendMessage<T>(ulong steamPlayerId, string type, T payload)
        {
            var packet = MyAPIGateway.Utilities.SerializeToBinary(
                new ModMessage
                {
                    Type = type,
                    Member = MyAPIGateway.Utilities.SerializeToBinary(payload)
                });

            MyAPIGateway.Multiplayer.SendMessageTo(8544, packet, steamPlayerId);
        }

        private static string GetOwnerName(object owner)
        {
            switch (owner)
            {
                case FactionPointOwner faction:
                    return ((MyFaction)faction.GetOwner()).Tag;

                case GroupPointOwner group:
                    return ((Group)group.GetOwner()).GroupName;

                default:
                    return "Unowned";
            }
        }

        public static void ReceiveModMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<ModMessage>(data);

            if (message.Type != "DataRequest")
                return;

            var request = MyAPIGateway.Utilities.SerializeFromBinary<DataRequest>(message.Member);

            switch (request.DataType)
            {
                case "Territory":
                    SendPlayerTerritories(request.SteamId);
                    break;

                case "Hud":
                    SendPlayerHudStatus(request.SteamId);
                    break;

                case "Chat":
                    // AllianceChat.AddOrRemoveToChat(request.SteamId);
                    break;
            }
        }
    }

    [ProtoContract]
    public class DataRequest
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)]
        public string DataType;
    }

    [ProtoContract]
    public class BoolStatus
    {
        [ProtoMember(1)]
        public bool Enabled;
    }

    [ProtoContract]
    public class ModMessage
    {
        [ProtoMember(1)]
        public string Type;

        [ProtoMember(2)]
        public byte[] Member;
    }

    [ProtoContract]
    public class PlayerDataPvP
    {
        [ProtoMember(1)]
        public List<PvPArea> PvPAreas;
    }

    [ProtoContract]
    public class PvPArea
    {
        [ProtoMember(1)]
        public Vector3D Position;

        [ProtoMember(2)]
        public float Distance;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public string OwnerName;
    }
}