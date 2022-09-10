﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace AlliancesPlugin.KamikazeTerritories
{
    [ProtoContract]
    public class PlayerDataPvP
    {
        [ProtoMember(1)] 
        public List<PvPArea> PvPAreas;
    }
}