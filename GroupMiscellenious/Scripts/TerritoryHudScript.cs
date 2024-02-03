using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private static int ticks;
        public static DateTime NextSendTerritory = DateTime.Now.AddMinutes(5);
        public static void UpdateExample()
        {
            ticks++;
            if (ticks % 128 == 0)
            {
                if (DateTime.Now > NextSendTerritory)
                {
                    foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                    {
                        SendPlayerTerritories(player.Id.SteamId);
                    }
                }
            }

        }

        public static void Patch(PatchContext ctx)
        {
            Core.UpdateCycle += UpdateExample;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8544, ReceiveModMessage);
        }

        public static void SendPlayerHudStatus(ulong steamPlayerId)
        {

            var message = new BoolStatus
            {
                Enabled = true
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

        public static void SendPlayerTerritories(ulong steamPlayerId)
        {
            var id = Core.GetIdentityByNameOrId(steamPlayerId.ToString());
            if (id == null)
            {
                return;
            }
            IMyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);

            var message = new PlayerDataPvP
            {
                PvPAreas = new List<PvPArea>()
            };



            foreach (var territory in Core.Territories)
            {
                var owner = "Unowned";
                switch (territory.Value.Owner)
                {
                    case FactionPointOwner faction:
                        MyFaction fac = (MyFaction)faction.GetOwner();
                        owner = fac.Tag;
                        break;
                    case GroupPointOwner group:
                        var groupT = (Group)group.GetOwner();
                        owner = groupT.GroupName;
                        break;
                }

                message.PvPAreas.Add(new PvPArea()
                {
                    Name = territory.Value.Name ?? "PvP Area",
                    Position = territory.Value.Position,
                    Distance = territory.Value.RadiusDistance,
                    OwnerName = owner
                });

                foreach (var capture in territory.Value.CapturePoints)
                {
                    owner = "Unowned";
                    switch (capture.PointOwner)
                    {
                        case FactionPointOwner faction:
                            MyFaction fac = (MyFaction)faction.GetOwner();
                            owner = fac.Tag;
                            break;
                        case GroupPointOwner group:
                            var groupT = (Group)group.GetOwner();
                            owner = groupT.GroupName;
                            break;
                    }
                    if (capture is FactionGridCapLogic gridcap)
                    {
                        message.PvPAreas.Add(new PvPArea()
                        {
                            Name = gridcap.PointName,
                            Position = gridcap.GPSofPoint,
                            Distance = gridcap.CaptureRadius,
                            OwnerName = owner
                        });
                    }
                    else
                    {
                        message.PvPAreas.Add(new PvPArea()
                        {
                            Name = capture.PointName,
                            Position = capture.GetPointsLocationIfSet(),
                            Distance = 10000,
                            OwnerName = owner
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
        public static void ReceiveModMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
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
