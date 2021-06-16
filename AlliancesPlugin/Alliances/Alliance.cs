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

namespace AlliancesPlugin.Alliances
{
    public class Alliance
    {
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
        public List<long> EnemyFactions = new List<long>();

        public List<long> Invites = new List<long>();
        public List<long> AllianceMembers = new List<long>();

        public long bankBalance = 0;
        public Boolean hasUnlockedShipyard = false;
        public Boolean hasUnlockedHangar = false;
        FileUtils utils = new FileUtils();

        public Dictionary<String, RankPermissions> CustomRankPermissions = new Dictionary<string, RankPermissions>();
        public Dictionary<ulong, String> PlayersCustomRank = new Dictionary<ulong, string>();
        public RankPermissions UnrankedPerms = new RankPermissions();
        public int CurrentMetaPoints = 0;

        public Dictionary<ulong, RankPermissions> playerPermissions = new Dictionary<ulong, RankPermissions>();

        public bool ElectionCycle = false;
        public long ShipyardFee = 25000000;
        public Boolean HasAccess(ulong id, AccessLevel level)
        {
            if (SupremeLeader == id)
            {
                return true;
            }
            if (PlayersCustomRank.ContainsKey(id))
            {
                if (CustomRankPermissions[PlayersCustomRank[id]].permissions.Contains(level))
                {
                    return true;
                }
            }
            if (playerPermissions.ContainsKey(id))
            {
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
        public float GetTaxRate(ulong id)
        {
         
            if (SupremeLeader == id)
            {
                return 0;
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
            if (PlayersCustomRank.ContainsKey(id))
            {
                return PlayersCustomRank[id];
            }
                if (otherTitles.ContainsKey(id))
            {
                return otherTitles[id];
            }
         

            return "Unranked";
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
                }

                return utils.ReadFromJsonFile<HangarData>(AlliancePlugin.path + "//HangarData//" + AllianceId + "//hangar.json");
            }
            return null;
        }
        public PrintQueue LoadPrintQueue()
        {
            if (hasUnlockedShipyard)
            {
                return utils.ReadFromJsonFile<PrintQueue>(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json");
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
                item.Amount = amount;
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
        public string OutputAlliance()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(description);
            sb.AppendLine("");
            bankBalance = DatabaseForBank.GetBalance(AllianceId);
            sb.AppendLine("Bank Balance : " + String.Format("{0:n0}", bankBalance) + " SC.");
            sb.AppendLine("");
            sb.AppendLine("Meta Points : " + String.Format("{0:n0}", CurrentMetaPoints));
            sb.AppendLine("");
            sb.AppendLine(LeaderTitle);

            sb.AppendLine(AlliancePlugin.GetPlayerName(SupremeLeader));
            sb.AppendLine("");

            StringBuilder perms = new StringBuilder();
            foreach (KeyValuePair<String, RankPermissions> customs in CustomRankPermissions)
            {
                perms.Clear();
                foreach (AccessLevel level in customs.Value.permissions)
                {
                    perms.Append(level.ToString() + ", ");
                }
                sb.AppendLine(customs.Key + "Permissions : " + perms.ToString());
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
            sb.AppendLine("");
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
            foreach (long id in EnemyFactions)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag);
                }
                foreach (String s in enemies)
                {
                    sb.AppendLine(s);
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Member Factions");
            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag + " - " + fac.Members.Count + " members");
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Pending invites");
            foreach (long id in Invites)
            {
               IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null) {
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
                            foreach (long id2 in EnemyFactions)
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
        }
        public void ForceAddMember(long id)
        {
            if (!AllianceMembers.Contains(id))
            {
                AllianceMembers.Add(id);
            }
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
            if (Invites.Contains(fac.FactionId))
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
