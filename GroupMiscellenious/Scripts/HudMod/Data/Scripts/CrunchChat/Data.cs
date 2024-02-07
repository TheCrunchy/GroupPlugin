using System;
using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace Crunch
{
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
    public class DataRequest
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)] 
        public string DataType;
    }

    [ProtoContract]
    public class GroupChatEvent
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
        [ProtoMember(2)]
        public ulong SenderId { get; set; }
        [ProtoMember(3)]
        public string SenderName { get; set; }

        [ProtoMember(4)]
        public string Message { get; set; }
    }

    [ProtoContract]
    public class GroupEvent
    {
        [ProtoMember(1)]
        public byte[] EventObject { get; set; }

        [ProtoMember(2)]
        public string EventType { get; set; }
    }
}
