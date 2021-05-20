using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class PrintQueue
    {
        public Guid allianceId;
        public int upgradeSlots = 0;
        public float upgradeSpeed = AlliancePlugin.shipyardConfig.StartingSpeedMultiply;
        public List<string> claimedGrids = new List<string>();

        public int SpeedUpgrade = 0;
        public int SlotsUpgrade = 0;
        //  public Int64 PriceLastUpgrade = 0;
        public Dictionary<int, PrintQueueItem> queue = new Dictionary<int, PrintQueueItem>();
        public void SaveLog(Alliance alliance, PrintLog log1)
        {
            FileUtils utils = new FileUtils();
            utils.WriteToJsonFile<PrintLog>(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId + "//log.json", log1);
        }
        public PrintLog GetLog(Alliance alliance)
        {
            FileUtils utils = new FileUtils();
            if (!Directory.Exists(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId);
            }
            if (!File.Exists(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId + "//log.json")){
                PrintLog log = new PrintLog
                {
                    allianceId = alliance.AllianceId
                };
                utils.WriteToJsonFile<PrintLog>(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId + "//log.json", log);
                return log;
            }
            return utils.ReadFromJsonFile<PrintLog>(AlliancePlugin.path + "//ShipyardData//" + alliance.AllianceId + "//log.json");
        }
        public bool canAddToQueue()
        {
            if (upgradeSlots == 0)
            {

                return false;
            }
            if (queue.Count == upgradeSlots)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Dictionary<int, PrintQueueItem> getQueue()
        {
            return queue;
        }

        public int getAvailableSlot()
        {
            for (int i = 1; i <= upgradeSlots; i++)
            {
                if (!queue.ContainsKey(i))
                {
                    return i;
                }
            }
            return 0;
        }
        public bool addToQueue(String name, long steam, long identity, string ownerName, DateTime start, DateTime end, double x, double y, double z)
        {
            if (getAvailableSlot() != 0)
            {
                queue.Add(getAvailableSlot(), new PrintQueueItem(name, steam, identity, ownerName, start, end, x, y, z));
                return true;
            }
            return false;
        }

        public void removeFromQueue(int slot)
        {
            this.queue.Remove(slot);
          
        }
        public float upgradePrice()
        {
            int nextUpgrade = upgradeSlots + 1;
            if (nextUpgrade > AlliancePlugin.shipyardConfig.MaxShipyardSlots)
            {
                return 0;
            }
            else
            {

                //    if (upgradeSlots > 1)
                //    {
                //        return this.PriceLastUpgrade * MoneyPlugin.config.MultiplierPerSlot;
                //   }
                //    else
                // {
                return 0;
                //   }
            }
        }
        public float upgrade(Int64 Balance, long playerID)
        {
            int nextUpgrade = upgradeSlots + 1;
            if (nextUpgrade > AlliancePlugin.shipyardConfig.MaxShipyardSlots)
            {
                return 0;
            }

            if (Balance >= upgradePrice())
            {

                EconUtils.takeMoney(playerID, Convert.ToInt64(upgradePrice()));
                upgradeSlots += 1;
                return upgradePrice();
            }
            return 0;
        }

    }
}
