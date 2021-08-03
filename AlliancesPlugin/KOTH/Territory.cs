﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KOTH
{
    public class Territory
    {
        public Guid Id = System.Guid.NewGuid();
        public string Name;
        public int Radius = 50000;
        public bool enabled = true;
        public Guid Alliance = Guid.Empty;
        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public double x;
        public double y;
        public double z;
        public float ShipyardReductionPercent = 0;
        public Boolean HasStation = false;
        public long AddToUpkeepIfStationAboveLimit = 50000000;
        public double stationX = 0;
        public double stationY = 0;
        public double stationZ = 0;
        public DateTime transferTime = DateTime.Now;
        public Guid transferTo = Guid.Empty;
        public Guid previousOwner = Guid.Empty;
        public string FactionTagForStationOwner = "ACME";
    }
}