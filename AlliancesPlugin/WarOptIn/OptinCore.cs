using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.WarOptIn
{
    public class OptinCore
    {
        public static List<long> AllFactionIds = new List<long>();
        public static List<long> FactionsOptedIn = new List<long>();
        public static ListOfWarParticipants participants = new ListOfWarParticipants();
        public static WarConfig config = new WarConfig();
        public static void DoNeutralUpdate(long firstId, long SecondId)
        {
            MyFactionStateChange change = MyFactionStateChange.SendPeaceRequest;
            MyFactionStateChange change2 = MyFactionStateChange.AcceptPeace;
            List<object[]> Input = new List<object[]>();
            object[] MethodInput = new object[] { change, firstId, SecondId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput);
            object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput2);
            MySession.Static.Factions.SetReputationBetweenFactions(firstId, SecondId, 0);
        }

        public static void SaveFile()
        {
            AlliancePlugin.utils.WriteToJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//Alliances//OptionalWar//WarParticipants.json", participants);
        }
        public static ListOfWarParticipants LoadFile()
        {
            if (!File.Exists(AlliancePlugin.path + "//Alliances//OptionalWar//WarParticipants.json"))
            {
                AlliancePlugin.utils.WriteToJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//Alliances//OptionalWar//WarParticipants.json", new ListOfWarParticipants());
            }
            if (!File.Exists(AlliancePlugin.path + "//Alliances//OptionalWar//WarConfig.json"))
            {
                AlliancePlugin.utils.WriteToJsonFile<WarConfig>(AlliancePlugin.path + "//Alliances//OptionalWar//WarConfig.json", new WarConfig());
         
            }
            config = AlliancePlugin.utils.ReadFromJsonFile<WarConfig>(AlliancePlugin.path + "//Alliances//OptionalWar//WarConfig.json");

            return AlliancePlugin.utils.ReadFromJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//Alliances//OptionalWar//WarParticipants.json");
        }

        public static bool AddToWarParticipants(long id)
        {
            participants = LoadFile();
            if (!participants.FactionsAtWar.Contains(id))
            {
                return false;
            }
            else
            {
                participants.FactionsAtWar.Add(id);
                FactionsOptedIn.Add(id);
                SaveFile();
                return true;
            }
        }
        public static bool RemoveFromWarParticipants(long id)
        {
            participants = LoadFile();
            if (participants.FactionsAtWar.Contains(id))
            {
                participants.FactionsAtWar.Remove(id);
                FactionsOptedIn.Remove(id);
                SaveFile();
                return true;
            }
            else
            { 
                return false;
            }
        }
        public static void StateChange(MyFactionStateChange change, long fromFacId, long toFacId, long playerId, long senderId)
        {
            IMyFaction fac1;
            IMyFaction fac2;
            switch (change)
            {
                case MyFactionStateChange.DeclareWar:
                    fac1 = MySession.Static.Factions.TryGetFactionById(fromFacId);
                    fac2 = MySession.Static.Factions.TryGetFactionById(toFacId);
                    if (fac1 != null && fac2 != null)
                    {
                        if (!FactionsOptedIn.Contains(fromFacId))
                        {
                            AlliancePlugin.SendChatMessage("War Gods", $"You have not opted in to war. To opt in type !war enable", (ulong) senderId);
                            DoNeutralUpdate(fromFacId, toFacId);
                            break;
                        }
                        if (!FactionsOptedIn.Contains(toFacId))
                        {
                            AlliancePlugin.SendChatMessage("War Gods", $"Target faction has not opted in to war.", (ulong)senderId);
                            DoNeutralUpdate(fromFacId, toFacId);
                            break;
                        }
                    }
                    break;
                case MyFactionStateChange.RemoveFaction:
                    fac1 = MySession.Static.Factions.TryGetFactionById(fromFacId);
                    if (fac1 != null)
                    {
                        AllFactionIds.Remove(fromFacId);
                    }
                    break;
            }

        }

        public static void ProcessNewFaction(long newid)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(newid);
            if (faction != null)
            {
                AllFactionIds.Add(newid);
                foreach (MyFaction fac in MySession.Static.Factions.GetAllFactions())
                {
      
                    if (fac.FactionId != newid && fac.Tag.Length == 3)
                    {
                        DoNeutralUpdate(faction.FactionId, fac.FactionId);
                    }
                }
            }
        }

    }
}
