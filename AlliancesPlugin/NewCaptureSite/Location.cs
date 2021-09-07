using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.NewCaptureSite
{
    public class Location
    {
        public Boolean Enabled = false;
        public string WorldName = "SENDS17";
        public double chance = 1;
        public int Num = 1;
        public double X = 1;
        public double Y = 1;
        public double Z = 1;
        public int CaptureRadiusInMetre = 200;
        public Boolean RequireCaptureBlockForLootGen = true;
        public int CaptureBlockRange = 40000;
        public string captureBlockType = "Beacon";
        public string captureBlockSubtype = "LargeBlockBeacon";
        public string Name = "Example Location 1";
        public int LinkedLootLocation = 1;
        public string KothBuildingOwner = "BOB";
        public string GpsName = "Capture site Location 1";
        public int MaxLootAmount = 1;
        public String LinkedTerritory = "Change this";
        public Boolean HasTerritory = false;
    }
}
