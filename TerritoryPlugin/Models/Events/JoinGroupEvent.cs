﻿using System;
using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class JoinGroupEvent
    {
        [ProtoMember(1)]
        public Guid JoinedGroupId { get; set; }
        [ProtoMember(2)]
        public long FactionId { get; set; }
    }
}
