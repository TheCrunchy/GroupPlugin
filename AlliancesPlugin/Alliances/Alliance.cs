﻿using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace AlliancesPlugin
{
    public class Alliance
    {
        public Guid AllianceId = System.Guid.NewGuid();
        public String name;
        public String description;
        public ulong SupremeLeader;
        public string LeaderTitle = "His Glorious Majesty the Supreme Leader";
        public string AdmiralTitle = "Grand Admiral";
        public string OfficerTitle = "Admiral";
        public Boolean AllowElections = false;
        public List<long> BlockedFactions = new List<long>();
        public Dictionary<ulong, String> otherTitles = new Dictionary<ulong, string>();

        public List<String> enemies = new List<String>();
        public List<long> EnemyFactions = new List<long>();

        public List<long> Invites = new List<long>();
        public List<long> AllianceMembers = new List<long>();

        public List<ulong> admirals = new List<ulong>();
        public List<ulong> officers = new List<ulong>();

        public List<ulong> bankAccess = new List<ulong>();
        public long bankBalance = 0;
        public Boolean hasUnlockedShipyard = false;
        public PrintQueue LoadPrintQueue()
        {
            if (hasUnlockedShipyard)
            {
                FileUtils utils = new FileUtils();
                return utils.ReadFromJsonFile<PrintQueue>(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json");
            }
            return null;
        }
        public void SavePrintQueue(PrintQueue queue)
        {
            FileUtils utils = new FileUtils();
            if (!Directory.Exists(AlliancePlugin.path + "//ShipyardData//" + AllianceId)){
                Directory.CreateDirectory(AlliancePlugin.path + "//ShipyardData//" + AllianceId);
            }

            utils.WriteToJsonFile<PrintQueue>(AlliancePlugin.path + "//ShipyardData//" + AllianceId + "//queue.json", queue);
         
            return;
        }
        public void PayPlayer(Int64 amount, ulong steamid, ulong targetId)
        {
            bankBalance -= amount;
            FileUtils utils = new FileUtils();
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
            FileUtils utils = new FileUtils();
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
            FileUtils utils = new FileUtils();
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
            FileUtils utils = new FileUtils();
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
            FileUtils utils = new FileUtils();
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
        public BankLog GetLog()
        {
            FileUtils utils = new FileUtils();
            if (!Directory.Exists(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId))
            {
                Directory.CreateDirectory(AlliancePlugin.path + "//AllianceBankLogs//" + AllianceId +"//");
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
            sb.AppendLine("Bank Balance : " + String.Format("{0:n0}", bankBalance) + " SC.");
            sb.AppendLine("");
            sb.AppendLine(LeaderTitle);

            sb.AppendLine(MyMultiplayer.Static.GetMemberName(SupremeLeader));
            sb.AppendLine("");
            foreach (ulong id in admirals)
            {
                sb.AppendLine(AdmiralTitle + " " + MyMultiplayer.Static.GetMemberName(id));
            }
            sb.AppendLine("");
            foreach (ulong id in officers)
            {
                sb.AppendLine(OfficerTitle + " " + MyMultiplayer.Static.GetMemberName(id));
            }
            sb.AppendLine("");
            foreach (ulong id in bankAccess)
            {
                sb.AppendLine("Banker " + " " + MyMultiplayer.Static.GetMemberName(id));
            }
            sb.AppendLine("");
            sb.AppendLine("Other Titled Members");
            foreach (KeyValuePair<ulong, String> titles in otherTitles)
            {
                sb.AppendLine(titles.Value + " " + MyMultiplayer.Static.GetMemberName(titles.Key));
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
            if (admirals.Contains(id))
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

                foreach (long id in AllianceMembers)
                {
                    IMyFaction allianceMember = MySession.Static.Factions.TryGetFactionById(id);
                    if (allianceMember != null)
                    {
                        DoFriendlyUpdate(fac.FactionId, allianceMember.FactionId);
                        MySession.Static.Factions.SetReputationBetweenFactions(fac.FactionId, allianceMember.FactionId, 1500);
                    }
                }
                AllianceMembers.Add(fac.FactionId);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void KickMember(string tag)
        {
            //
        }
        public long GetBalance()
        {
            return 0;
        }

    }
}
