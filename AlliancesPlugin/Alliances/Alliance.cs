using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using AlliancesPlugin.Hangar;
using AlliancesPlugin.Shipyard;
using AlliancesPlugin.JumpGates;
using VRage.Game;
using System.Numerics;
using VRageMath;
using AlliancesPlugin.KOTH;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Alliances.Upgrades.ShipClasses;
using AlliancesPlugin.Territory_Version_2.Models;

namespace AlliancesPlugin.Alliances
{
    public class Alliance
    {
        public Boolean IsOpenToAll = false;
        public Guid AllianceId = System.Guid.NewGuid();
        public String name;
        public String description;
        public ulong SupremeLeader;
        public string LeaderTitle = "Supreme Leader";
        public Boolean AllowElections = false;
        public List<long> BlockedFactions = new List<long>();
        public Dictionary<ulong, String> otherTitles = new Dictionary<ulong, string>();
        public string DiscordToken = string.Empty;
        public ulong DiscordChannelId = 0;
        public List<String> enemies = new List<String>();
        public List<string> EditorKicks = new List<string>();
        public List<string> EditorInvites = new List<string>();
        public List<long> EnemyFactions = new List<long>();
        public List<String> friendlies = new List<String>();
        public List<long> FriendlyFactions = new List<long>();
        public int reputation = 0;
        public List<long> Invites = new List<long>();
        public List<long> AllianceMembers = new List<long>();
        public int GridRepairUpgrade = 0;
        public long bankBalance = 0;
        public Boolean hasUnlockedShipyard = false;
        public Boolean hasUnlockedHangar = false;
        FileUtils utils = new FileUtils();

        public Dictionary<String, RankPermissions> CustomRankPermissions = new Dictionary<string, RankPermissions>();
        public Dictionary<ulong, String> PlayersCustomRank = new Dictionary<ulong, string>();
        public RankPermissions UnrankedPerms = new RankPermissions();
        public int CurrentMetaPoints = 0;
        public Dictionary<string, string> inheritance = new Dictionary<string, string>();
        public Dictionary<ulong, RankPermissions> playerPermissions = new Dictionary<ulong, RankPermissions>();

        public bool ElectionCycle = false;
        public long ShipyardFee = 25000000;
        public int r = 66;
        public int g = 163;
        public int b = 237;
        public int failedUpkeep = 0;
        public int RefineryUpgradeLevel = 0;
        public int AssemblerUpgradeLevel = 0;
        public string DiscordWebhookCaps;
        public string DiscordWebhookRadar;
        public Boolean ForceFriends = true;

        private ShipClassLimits _classLimits;

        public ShipClassLimits GetShipClassLimits()
        {
            if (_classLimits != null)
            {
                return _classLimits;
            }
            return LoadShipClassLimits();
        
        }

        public Dictionary<long, string> GetMemberFactions()
        {
            var returning = new Dictionary<long, string>();

            foreach (var member in AllianceMembers)
            {
                var faction = MySession.Static.Factions.TryGetFactionById(member);
                if (faction != null)
                {
                    returning.Add(member, faction.Tag);
                }
            }

            return returning;
        }

        public int GetShipClassLimit(string className)
        {
            return _classLimits.GetLimitForClass(className);
        }

        public ShipClassLimits LoadShipClassLimits()
        {
       
            var path = $"{AlliancePlugin.path}//ShipClassLimits//{AllianceId}.xml";
            if (!File.Exists(path))
            {
                ShipClassLimits templimits = new ShipClassLimits();
                AlliancePlugin.utils.WriteToXmlFile<ShipClassLimits>(path, templimits);
                return templimits;
            }
           var limits = AlliancePlugin.utils.ReadFromXmlFile<ShipClassLimits>(path);
            limits.PutLimitsInDictionary();
            _classLimits = limits;
            return limits;
        }

        public long GetExpectedUpkeep()
        {

            float upkeep = 0;
            int terCount = 0;
            bool hasTerritory = false;
            foreach (Territory ter in AlliancePlugin.Territories.Values)
            {

            }
            if (hasTerritory)
            {
                if (this.RefineryUpgradeLevel > 0)
                {
                    if (MyProductionPatch.upgrades.ContainsKey(this.RefineryUpgradeLevel))
                    {
                        upkeep += MyProductionPatch.upgrades[this.RefineryUpgradeLevel].AddsToUpkeep;
                    }
                }
                if (this.AssemblerUpgradeLevel > 0)
                {
                    if (MyProductionPatch.assemblerupgrades.ContainsKey(this.AssemblerUpgradeLevel))
                    {
                        upkeep += MyProductionPatch.assemblerupgrades[this.AssemblerUpgradeLevel].AddsToUpkeep;
                    }
                }
            }
            var cutoff = DateTime.Now - TimeSpan.FromDays(3);
            upkeep += AlliancePlugin.config.BaseUpkeepFee;
            foreach (long id in AllianceMembers)
            {
                MyFaction fac = MySession.Static.Factions.TryGetFactionById(id) as MyFaction;
                if (fac != null)
                {
                    upkeep += AlliancePlugin.config.BaseUpkeepFee * AlliancePlugin.config.PercentPerFac;
                    upkeep += AlliancePlugin.config.FeePerMember * fac.Members.Count;
                }
            }
            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.OwnerAlliance == AllianceId && !gate.CanBeRented)
                {
                    upkeep += gate.upkeep;
                }
            }
            if (this.hasUnlockedHangar)
            {
                upkeep += AlliancePlugin.config.HangarUpkeep;
            }
         //   if (this.hasUnlockedShipyard)
         //   {
         //       upkeep += AlliancePlugin.config.ShipyardUpkeep;
          //  }
            return (long)upkeep;
        }
        public long GetUpkeep()
        {

            float upkeep = 0;
            int terCount = 0;
            bool hasTerritory = false;
            foreach (Territory ter in AlliancePlugin.Territories.Values)
            {
           
            }
            if (hasTerritory)
            {
                if (this.RefineryUpgradeLevel > 0)
                {
                    if (MyProductionPatch.upgrades.ContainsKey(this.RefineryUpgradeLevel))
                    {
                        upkeep += MyProductionPatch.upgrades[this.RefineryUpgradeLevel].AddsToUpkeep;
                    }
                }
                if (this.AssemblerUpgradeLevel > 0)
                {
                    if (MyProductionPatch.assemblerupgrades.ContainsKey(this.AssemblerUpgradeLevel))
                    {
                        upkeep += MyProductionPatch.assemblerupgrades[this.AssemblerUpgradeLevel].AddsToUpkeep;
                    }
                }
            }
 
            var cutoff = DateTime.Now - TimeSpan.FromDays(3);
            upkeep += AlliancePlugin.config.BaseUpkeepFee;
            foreach (long id in AllianceMembers)
            {
                MyFaction fac = MySession.Static.Factions.TryGetFactionById(id) as MyFaction;
                if (fac != null)
                {
                    upkeep += AlliancePlugin.config.BaseUpkeepFee * AlliancePlugin.config.PercentPerFac;
                    foreach (KeyValuePair<long, MyFactionMember> mem in fac.Members)
                    {
                        MyIdentity idenid = MySession.Static.Players.TryGetIdentity(mem.Value.PlayerId);
                        DateTime referenceTime = idenid.LastLoginTime;
                        if (idenid.LastLogoutTime > referenceTime)
                            referenceTime = idenid.LastLogoutTime;
                        if (referenceTime >= cutoff)
                        {
                            upkeep += AlliancePlugin.config.FeePerMember;

                        }

                    }

                }
            }
            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.OwnerAlliance == AllianceId && !gate.CanBeRented)
                {
                    upkeep += gate.upkeep;
                }
            }
            if (this.hasUnlockedHangar)
            {
                upkeep += AlliancePlugin.config.HangarUpkeep;
            }
         //   if (this.hasUnlockedShipyard)
          //  {
           ///     upkeep += AlliancePlugin.config.ShipyardUpkeep;
           // }
            return (long)upkeep;
        }


        public List<AccessLevel> GetInheritedPermissions(String rank)
        {
            List<AccessLevel> levels = new List<AccessLevel>();

            if (!inheritance.TryGetValue(rank, out string minion)) return levels;
            levels.AddRange(CustomRankPermissions[rank].permissions);
            if (inheritance.ContainsKey(minion))
            {
                levels.AddRange(GetInheritedPermissions(minion));
            }

            return levels;
        }
        public Boolean HasInheritedAccess(AccessLevel level, string Rank)
        {
            List<AccessLevel> levels = new List<AccessLevel>();
            levels.AddRange(GetInheritedPermissions(Rank));

            return false;
        }
        public Boolean HasAccess(ulong id, AccessLevel level)
        {
            
            if (SupremeLeader == id)
            {
                return true;
            }

            if (PlayersCustomRank.ContainsKey(id))
            {
                if (CustomRankPermissions[PlayersCustomRank[id]].permissions.Contains(AccessLevel.Everything))
                {
                    return true;
                }
                if (CustomRankPermissions[PlayersCustomRank[id]].permissions.Contains(level))
                {
                    return true;
                }
                else
                {
                    if (HasInheritedAccess(level, PlayersCustomRank[id]))
                    {
                        return true;
                    }
                }
            }
            if (playerPermissions.ContainsKey(id))
            {
                if (playerPermissions[id].permissions.Contains(AccessLevel.Everything))
                {
                    return true;
                }
                if (playerPermissions[id].permissions.Contains(level))
                {
                    return true;
                }
            }

            if (UnrankedPerms.permissions.Contains(level))
            {
                return true;
            }


            return false;
        }

        public int GetFactionCount()
        {
            int count = 0;
            foreach (long id in this.AllianceMembers)
            {
                if (MySession.Static.Factions.TryGetFactionById(id) != null)
                {
                    count++;
                }
            }
            return count;
        }
        public float leadertax = 0;
        public float GetTaxRate(ulong id)
        {


            if (SupremeLeader == id)
            {
                return leadertax;
            }
            if (HasAccess(id, AccessLevel.TaxExempt))
            {
                return 0;
            }


            if (PlayersCustomRank.ContainsKey(id))
            {
                if (CustomRankPermissions.ContainsKey(PlayersCustomRank[id]))
                {
                    return CustomRankPermissions[PlayersCustomRank[id]].taxRate;
                }
            }




            return UnrankedPerms.taxRate;
        }
        public string GetTitle(ulong id)
        {
            if (SupremeLeader == id)
            {
                return LeaderTitle;
            }
            if (otherTitles.ContainsKey(id))
            {
                return otherTitles[id];
            }
            if (PlayersCustomRank.ContainsKey(id))
            {
                return PlayersCustomRank[id];
            }
        


            return "";
        }
        public string OutputMembers()
        {

            StringBuilder sb = new StringBuilder();
            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag);
                    foreach (KeyValuePair<long, MyFactionMember> member in fac.Members)
                    {
                        sb.AppendLine(AlliancePlugin.GetPlayerName(MySession.Static.Players.TryGetSteamId(member.Value.PlayerId)));
                    }
                    sb.AppendLine("");
                }
            }
            return sb.ToString();
        }
        public HangarData LoadHangar()
        {
            if (hasUnlockedHangar)
            {
                if (!Directory.Exists(AlliancePlugin.path + "//HangarData//" + AllianceId + "//"))
                {
                    Directory.CreateDirectory(AlliancePlugin.path + "//HangarData//" + AllianceId + "//");
                }
                if (!File.Exists(AlliancePlugin.path + "//HangarData//" + AllianceId + "//hangar.json"))
                {
                    HangarData data = new HangarData();
                    data.allianceId = AllianceId;
                    utils.WriteToJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + AllianceId + "//hangar.json", data);

                    return data;
                }
                HangarData hangar = utils.ReadFromJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + AllianceId + "//hangar.json");
                hangar.SlotsAmount = (int) HangarCommands.slotUpgrades[hangar.SlotUpgradeNum].NewSlots;
                return hangar;
            }
            return null;
        }
        public PrintQueue LoadPrintQueue()
        {
            if (File.Exists(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json"))
            {
       
                    
                   PrintQueue queue = utils.ReadFromJsonFile<PrintQueue>(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json");
                if (ShipyardCommands.slotUpgrades.ContainsKey(queue.SlotsUpgrade)){
                    queue.upgradeSlots = (int)ShipyardCommands.slotUpgrades[queue.SlotsUpgrade].NewSlots;
                }
                else{
                    queue.upgradeSlots = (int)ShipyardCommands.slotUpgrades[queue.SlotsUpgrade - 1].NewSlots;
                }
         
                return queue;
            }
            return null;
        }

        private Dictionary<String, StringBuilder> otherTitlesDic = new Dictionary<string, StringBuilder>();
        public void SavePrintQueue(PrintQueue queue)
        {
            if (!Directory.Exists(AlliancePlugin.path + "//ShipyardData//" + AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//ShipyardData//" + AllianceId);
            }

            utils.WriteToJsonFile<PrintQueue>(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json", queue);

            return;
        }
        public void PayPlayer(Int64 amount, ulong steamid, ulong targetId)
        {
            bankBalance -= amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.Action = "paid";
            item.PlayerPaid = targetId;
            item.TimeClaimed = DateTime.Now;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void PayShipyardFee(Int64 amount, ulong steamid)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.Action = "shipyard fee";
            item.TimeClaimed = DateTime.Now;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
            AlliancePlugin.SaveAllianceData(this);
        }
        public void PayDividend(Int64 amount, List<long> ids, ulong steamid)
        {
            Int64 amountToPay = amount / ids.Count();
            BankLog log = GetLog();
            foreach (long id in ids)
            {
                BankLogItem item = new BankLogItem();
                EconUtils.addMoney(id, amountToPay);
                bankBalance -= amountToPay;
                item.SteamId = steamid;
                item.Amount = amount / ids.Count;
                item.Action = "dividend";
                item.PlayerPaid = MySession.Static.Players.TryGetSteamId(id);
                item.TimeClaimed = DateTime.Now;
                item.BankAmount = bankBalance;
                log.log.Add(item);
            }
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }

        public void PayFaction(Int64 amount, ulong steamid, long factionid)
        {
            bankBalance -= amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.Action = "paid fac";
            item.FactionPaid = factionid;
            item.TimeClaimed = DateTime.Now;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void WithdrawMoney(Int64 amount, ulong steamid)
        {
            bankBalance -= amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.Action = "withdrew";
            item.TimeClaimed = DateTime.Now;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void DepositMoney(Int64 amount, ulong steamid)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "deposited";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void DepositKOTH(Int64 amount, ulong steamid)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "koth";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void Upkeep(Int64 amount, ulong steamid)
        {
            bankBalance -= amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "upkeep";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void AdminWithdraw(Int64 amount, ulong steamid)
        {
            bankBalance -= amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "admin withdraw";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void AdminAdd(Int64 amount, ulong steamid)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "admin add";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void DepositTax(Int64 amount, ulong steamid)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "tax";
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void DepositTerritoryTax(Int64 amount, ulong steamid, string territory)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "territory tax " + territory;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public void GateFee(Int64 amount, ulong steamid, string GateName)
        {
            bankBalance += amount;
            BankLog log = GetLog();
            BankLogItem item = new BankLogItem();
            item.SteamId = steamid;
            item.Amount = amount;
            item.TimeClaimed = DateTime.Now;
            item.Action = "gate fee " + GateName;
            item.BankAmount = bankBalance;
            log.log.Add(item);
            utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
        }
        public BankLog GetLog()
        {
            if (!Directory.Exists(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//");
            }

            if (!File.Exists(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json"))
            {
                BankLog log = new BankLog
                {
                    allianceId = AllianceId
                };
                utils.WriteToJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json", log);
                return log;
            }
            return utils.ReadFromJsonFile<BankLog>(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId + "//log.json");
        }

        public Dictionary<ulong, string> GetPlayerSteamIds()
        {
            var returning = new Dictionary<ulong, string>();

            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    foreach (KeyValuePair<long, MyFactionMember> member in fac.Members)
                    {
                        var steamId = MySession.Static.Players.TryGetSteamId(member.Value.PlayerId);
                        if (!returning.ContainsKey(steamId))
                        {
                            returning.Add(steamId, AlliancePlugin.GetPlayerName(steamId));
                        }
                    }
                }
            }

            return returning;
        }

        public string OutputAlliance()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Alliance Leader: " + LeaderTitle + " " + AlliancePlugin.GetPlayerName(SupremeLeader));
            sb.AppendLine("Open to All factions: " + IsOpenToAll);
            if (!String.IsNullOrEmpty(description))
            {
                sb.AppendLine("");
                sb.AppendLine(description);
            }
            sb.AppendLine("");
            bankBalance = DatabaseForBank.GetBalance(AllianceId);
            sb.AppendLine("Bank Balance : " + String.Format("{0:n0}", bankBalance) + " SC.");
            sb.AppendLine("");
            if (failedUpkeep > 0)
            {
                sb.AppendLine("Failed Upkeep : " + this.failedUpkeep + " Deleted at " + AlliancePlugin.config.UpkeepFailBeforeDelete);
            }

            sb.AppendLine("Maximum Upkeep Value : " + String.Format("{0:n0}", this.GetExpectedUpkeep()) + " SC.");
            sb.AppendLine("Current Upkeep Value : " + String.Format("{0:n0}", this.GetUpkeep()) + " SC.");
            int mult = this.GetFactionCount();
            sb.AppendLine("");
            if (AlliancePlugin.config.DoItemUpkeep)
            {
                sb.AppendLine("Item Upkeep");
                foreach (KeyValuePair<MyDefinitionId, int> keys in AlliancePlugin.ItemUpkeep)
                {
                    string temp = keys.Key.ToString();
                    temp = temp.Replace("MyObjectBuilder_", "");
                    temp = temp.Replace("/", " ");
                    sb.AppendLine(temp + " : " + keys.Value * mult);
                }
            }

            sb.AppendLine("Vault contents");
            sb.AppendLine(DatabaseForBank.GetVaultString(this));
            sb.AppendLine("");
            sb.AppendLine("Refinery upgrade level : " + this.RefineryUpgradeLevel);
            sb.AppendLine("Assembler upgrade level : " + this.AssemblerUpgradeLevel);
            sb.AppendLine("");
            sb.AppendLine("Meta Points : " + String.Format("{0:n0}", CurrentMetaPoints));
            sb.AppendLine("");

            StringBuilder perms = new StringBuilder();
            foreach (KeyValuePair<String, RankPermissions> customs in CustomRankPermissions)
            {
                perms.Clear();
                foreach (AccessLevel level in customs.Value.permissions)
                {
                    perms.Append(level.ToString() + ", ");
                }
                sb.AppendLine("");
                sb.AppendLine(customs.Key + " Permissions : " + perms.ToString());
                sb.AppendLine(customs.Key + " tax rate : " + CustomRankPermissions[customs.Key].taxRate * 100 + "%");
            }

            sb.AppendLine("");
            perms.Clear();
            foreach (AccessLevel level in UnrankedPerms.permissions)
            {
                perms.Append(level.ToString() + ", ");
            }
            sb.AppendLine("Unranked Permissions : " + perms.ToString());
            sb.AppendLine("Unranked tax rate : " + UnrankedPerms.taxRate * 100 + "%");
            sb.AppendLine("");
            otherTitlesDic.Clear();
            foreach (KeyValuePair<ulong, String> titles in PlayersCustomRank)
            {
                if (otherTitlesDic.ContainsKey(titles.Value))
                {

                    otherTitlesDic[titles.Value].AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                }
                else
                {
                    StringBuilder sbb = new StringBuilder();
                    sbb.AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                    otherTitlesDic.Add(titles.Value, sbb);
                }

            }
            foreach (KeyValuePair<String, StringBuilder> key in otherTitlesDic)
            {
                sb.AppendLine(key.Value.ToString());
            }
            otherTitlesDic.Clear();
            foreach (KeyValuePair<ulong, String> titles in otherTitles)
            {
                if (otherTitlesDic.ContainsKey(titles.Value))
                {

                    otherTitlesDic[titles.Value].AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                }
                else
                {
                    StringBuilder sbb = new StringBuilder();
                    sbb.AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                    otherTitlesDic.Add(titles.Value, sbb);
                }

            }
            foreach (KeyValuePair<String, StringBuilder> key in otherTitlesDic)
            {
                sb.AppendLine(key.Value.ToString());
            }
            sb.AppendLine("");
            sb.AppendLine("Hostile Factions and Hostile Alliances");
            foreach (String s in enemies)
            {
                sb.AppendLine(s);
            }

            foreach (long id in EnemyFactions)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag);
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Member Factions");
            int memberCount = 0;
            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag + " - " + fac.Members.Count + " members");
                    memberCount += fac.Members.Count;
                }
            }
            sb.AppendLine("Total Members " + memberCount);
            sb.AppendLine("");
            sb.AppendLine("Pending invites");
            foreach (long id in Invites)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Name + " - " + fac.Tag);
                }
            }

            return sb.ToString();
        }

        public void DoFriendlyUpdate(long firstId, long SecondId)
        {
            MyFactionStateChange change = MyFactionStateChange.SendFriendRequest;
            MyFactionStateChange change2 = MyFactionStateChange.AcceptFriendRequest;
            List<object[]> Input = new List<object[]>();
            object[] MethodInput = new object[] { change, firstId, SecondId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput);
            object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput2);

        }

        public void ForceFriendlies()
        {
            if (ForceFriends)
            {
                foreach (long id in AllianceMembers)
                {
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                    if (fac != null)
                    {
                        foreach (long id2 in AllianceMembers)
                        {
                            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                          
                            if (fac2 != null && fac != fac2)
                            {
                              
                                if (!MySession.Static.Factions.AreFactionsFriends(id, id2))
                                {
                                    MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 1500);
                                    DoFriendlyUpdate(id, id2);

                                }
                            }
                        }
                    }
                }
            }
        }

        public void ForceEnemies()
        {

            foreach (String s in enemies)
            {
                if (AlliancePlugin.AllAlliances.TryGetValue(s, out Alliance enemy))
                {
                    foreach (long id in AllianceMembers)
                    {
                        IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                        if (fac != null)
                        {

                            foreach (long id2 in enemy.AllianceMembers)
                            {
                                IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                                if (fac2 != null && fac != fac2)
                                {
                                    if (!MySession.Static.Factions.AreFactionsEnemies(id, id2))
                                    {
                                        MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 0);
                                        MyFactionCollection.DeclareWar(id, id2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (long id2 in EnemyFactions)
            {
                foreach (long id in AllianceMembers)
                {

                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                    IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                    if (fac2 != null && fac != fac2)
                    {
                        if (!MySession.Static.Factions.AreFactionsEnemies(id, id2))
                        {
                            MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 0);
                            MyFactionCollection.DeclareWar(id, id2);
                        }
                    }
                }
            }
        }
        public void ForceAddMember(long id)
        {
            if (!AllianceMembers.Contains(id))
            {
                AllianceMembers.Add(id);
            }
            ForceFriendlies();
        }
        public void SendInvite(long id)
        {

            if (!Invites.Contains(id))
            {
                Invites.Add(id);
            }

        }
        public void RevokeInvite(long id)
        {
            if (Invites.Contains(id))
            {
                Invites.Remove(id);
            }

        }
        public void RemoveTitle(ulong steamid, String title)
        {
            if (CustomRankPermissions.ContainsKey(title))
            {
                if (PlayersCustomRank.ContainsKey(steamid))
                {
                    PlayersCustomRank.Remove(steamid);
                    return;
                }

            }
            if (otherTitles.ContainsKey(steamid))
            {
                otherTitles.Remove(steamid);
                return;
            }
        }
        public void SetTitle(ulong steamid, String title)
        {
            if (CustomRankPermissions.ContainsKey(title))
            {
                if (PlayersCustomRank.ContainsKey(steamid))
                {
                    PlayersCustomRank[steamid] = title;
                }
                else
                {
                    PlayersCustomRank.Add(steamid, title);
                }
                return;
            }
            if (otherTitles.ContainsKey(steamid))
            {
                otherTitles[steamid] = title;
            }
            else
            {
                otherTitles.Add(steamid, title);
            }
        }
        public Boolean HasPermissionToInvite(ulong id)
        {
            if (SupremeLeader.Equals(id))
                return true;
            if (HasAccess(id, AccessLevel.Invite))
                return true;
            return false;
        }
        public Boolean JoinAlliance(MyFaction fac)
        {
            if (BlockedFactions.Contains(fac.FactionId))
            {
                return false;
            }
            if (Invites.Contains(fac.FactionId) || IsOpenToAll)
            {
                Invites.Remove(fac.FactionId);
                AllianceMembers.Remove(fac.FactionId);
                AllianceMembers.Add(fac.FactionId);
                ForceFriendlies();
                return true;
            }
            else
            {
                return false;
            }
        }

        public long GetBalance()
        {
            return this.bankBalance;
        }

    }
}
