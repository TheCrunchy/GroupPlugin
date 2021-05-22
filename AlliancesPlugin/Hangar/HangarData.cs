using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
   public class HangarData
    {
        public int SlotsAmount = 1;
        public int SlotUpgradeNum = 0;
        public Guid allianceId;
        FileUtils utils = new FileUtils();
        public Dictionary<int, HangarItem> ItemsInHangar = new Dictionary<int, HangarItem>();
        public HangarLog GetHangarLog(Alliance alliance)
        {
           
            if (!Directory.Exists(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//");
            }

            if (!File.Exists(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json"))
            {
               HangarLog log = new HangarLog
                {
                    allianceId = alliance.AllianceId
                };
                utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
                return log;
            }
            return utils.ReadFromJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json");
        }

        public Boolean SaveGridToHangar(String gridName, ulong steamid, Alliance alliance)
        {
            HangarLog log = GetHangarLog(alliance);
            HangarLogItem item = new HangarLogItem();
            item.action = "Saved";
            item.steamid = steamid;
            item.GridName = gridName;
            item.time = DateTime.Now;
            log.log.Add(item);
            utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);

            return true;
        }
        public Boolean LoadGridFromHangar(int slotNum, ulong steamid, Alliance alliance, MyIdentity identity, MyFaction faction)
        {

            //check for enemies
            HangarLog log = GetHangarLog(alliance);
            HangarLogItem item = new HangarLogItem();
            item.action = "Loaded";
            item.steamid = steamid;
            item.GridName = ItemsInHangar[slotNum].name;
            item.time = DateTime.Now;
            log.log.Add(item);
            utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);


            return true;
        }
    }
}
