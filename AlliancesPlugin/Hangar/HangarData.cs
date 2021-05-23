using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

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
        public void SaveHangar(Alliance alliance)
        {
            utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
        }
        public int getAvailableSlot()
        {
            for (int i = 1; i <= SlotsAmount; i++)
            {
                if (!ItemsInHangar.ContainsKey(i))
                {
                    return i;
                }
            }
            return 0;
        }
        public Boolean SaveGridToHangar(String gridName, ulong steamid, Alliance alliance, Vector3D position, MyFaction faction, List<MyCubeGrid> gridsToSave, long IdentityId)
        {
  
            HangarLog log = GetHangarLog(alliance);
            HangarLogItem item = new HangarLogItem();
            item.action = "Saved";
            item.steamid = steamid;
            item.GridName = gridName;
            item.time = DateTime.Now;
            log.log.Add(item);
            utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
            HangarItem hangItem = new HangarItem();
            hangItem.name = gridName;
                hangItem.steamid = steamid;
            hangItem.position = position;
            ItemsInHangar.Add(getAvailableSlot(), hangItem);
            GridManager.SaveGridNoDelete(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + gridName + ".xml"), gridName, false, true, gridsToSave);
            utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
            return true;
        }
        public Boolean LoadGridFromHangar(int slotNum, ulong steamid, Alliance alliance, MyIdentity identity, MyFaction faction)
        {
           
          
            if (GridManager.LoadGrid(System.IO.Path.Combine(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml"), ItemsInHangar[slotNum].position, false, steamid, ItemsInHangar[slotNum].name))
                {
                HangarLog log = GetHangarLog(alliance);
                HangarLogItem item = new HangarLogItem();
                item.action = "Loaded";
                item.steamid = steamid;
                item.GridName = ItemsInHangar[slotNum].name;
                item.time = DateTime.Now;
                log.log.Add(item);
                utils.WriteToJsonFile<HangarLog>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//log.json", log);
                ItemsInHangar.Remove(slotNum);
                List<HangarItem> temp = new List<HangarItem>();
                foreach (HangarItem hangitem in ItemsInHangar.Values)
                {
                    temp.Add(hangitem);
                }
                ItemsInHangar.Clear();
                int i = 1;
                foreach (HangarItem hangitem in temp)
                {
                    ItemsInHangar.Add(i, hangitem);
                    i++;
                }
                utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//hangar.json", this);
                File.Delete(AlliancePlugin.path + "//HangarData//" + alliance.AllianceId + "//" + ItemsInHangar[slotNum].name + ".xml");
            }
            else
            {
                return false;
            }
                    //check for enemies
 


            return true;
        }
    }
}
