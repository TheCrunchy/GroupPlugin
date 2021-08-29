using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.NewCaptureSite
{
    public class LootLocation
    {
        public int Num = 1;
        public string WorldName = "SENDS17";
        public double X = 1;
        public double Y = 1;
        public double Z = 1;
        public string Name;
        public string LootBoxTerminalName = "LOOT BOX";
        public int SecondsBetweenCoreSpawn = 180;
        public List<RewardItem> loot = new List<RewardItem>();
        public DateTime nextCoreSpawn = DateTime.Now;
        public long LootboxGridEntityId = 0;
        public int RadiusToCheck = 1000;
        public string KothBuildingOwner = "BOB";
    }
}
