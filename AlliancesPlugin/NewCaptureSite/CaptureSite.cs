using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.NewCaptureSite
{
    public class CaptureSite
    {
        public string Name = "Default, change this";
        public Boolean AllianceSite = false;
        public int amountCaptured = 0;
        public int MinutesBeforeCaptureStarts = 10;
        public Boolean ChangeCapSiteOnUnlock = true;
        public String LinkedTerritory = "Change this";
        public Boolean LockAfterSoManyHoursOpen = false;
        public int LockAgainAfterThisManyHours = 2;
        public Boolean CaptureStarted = false;
        public Boolean HasTerritory = false;
        public Boolean Locked = false;
        public Boolean OnlyDoLootOnceAfterCap = false;
        public int SecondsForOneLootSpawnAfterCap = 600;
        //    public Boolean UnlockAtTheseTimes = false;
        //     public List<int> HoursToUnlockAfter = new List<int>();
        public int CapturesRequiredForTerritory = 3;
        public List<TerritoryProgress> TerritoryCapProgress = new List<TerritoryProgress>();
        private Dictionary<Guid, int> CapProgress = new Dictionary<Guid, int>();
        public void SaveCapProgress()
        {
            TerritoryCapProgress.Clear();
            foreach (KeyValuePair<Guid, int> pair in CapProgress)
            {
                TerritoryProgress prog = new TerritoryProgress();
                prog.setTerritoryProgress(pair.Key, pair.Value);
                TerritoryCapProgress.Add(prog);
            }
        }
        public void LoadCapProgress()
        {
            foreach (TerritoryProgress progress in TerritoryCapProgress)
            {
                AddCapProgress(progress.AllianceId, progress.Progress);
            }
        }
        public void ClearCapProgress()
        {
            this.TerritoryCapProgress.Clear();
            this.CapProgress.Clear();
        }
        public int GetCapProgress(Guid allianceId)
        {
            if (CapProgress.TryGetValue(allianceId, out int prog))
            {
                return prog;
            }

            return 0;
        }
        public void AddCapProgress(Guid allianceId, int num = 1)
        {
            if (CapProgress.TryGetValue(allianceId, out int prog))
            {
                CapProgress[allianceId] += num;
            }
            else
            {
                CapProgress.Add(allianceId, num);
            }
        }
        public class TerritoryProgress
        {
            public void setTerritoryProgress(Guid allianceid, int progress)
            {
                AllianceId = allianceid;
                Progress = progress;
            }
            public Guid AllianceId = Guid.Empty;
            public int Progress = 0;
        }
        private LootLocation currentLoot = null;
        public LootLocation GetLootSite()
        {
            if (currentLoot != null)
            {
                return currentLoot;
            }
            foreach (LootLocation lot in loot)
            {
                if (lot.Num == GetCurrentLocation().LinkedLootLocation)
                {
                    currentLoot = lot;
                    return lot;
                }
            }
            return null;
        }
        public Location GetNewCapSite(Location ignore)
        {
            Random random = new Random();
            List<Location> temp = new List<Location>();
            foreach (Location loc in locations)
            {
                if (!loc.Name.Equals(ignore))
                {
                    if (random.NextDouble() <= loc.chance)
                    {
                        temp.Add(loc);
                    }
                }
            }
            if (temp.Count == 1)
            {
                return temp[0];
            }
            random = new Random();
            int r = random.Next(temp.Count);
            return temp[r];
        }

        public int SecondsBetweenCaptureCheck = 60;
        public int PointsPerCap = 10;
        public int PointsToCap = 100;
        public int CurrentSite = 1;
        public Guid AllianceOwner = Guid.Empty;
        public Guid CapturingAlliance = Guid.Empty;
        public long FactionOwner = 1;
        public long CapturingFaction = 0;
        public int hourCooldownAfterFail = 1;
        public int hoursToLockAfterTerritoryCap = 12;
        public int hoursToLockAfterNormalCap = 12;
        public DateTime nextCaptureAvailable = DateTime.Now;
        public Boolean doChatMessages = true;
        public Boolean doDiscordMessages = true;
   
        public DateTime nextCaptureInterval = DateTime.Now;

        public DateTime unlockTime = DateTime.Now;
        public void setLootSite(LootLocation loc)
        {
           
        }
        public Location GetCurrentLocation()
        {
            foreach (Location loc in locations)
            {
                if (loc.Num == this.CurrentSite)
                {
                    return loc;
                }
            }
            return null;
        }
        public List<Location> locations = new List<Location>();
        public List<LootLocation> loot = new List<LootLocation>();
    }
}
