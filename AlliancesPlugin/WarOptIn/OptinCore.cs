using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using static Sandbox.Game.Multiplayer.MyFactionCollection;

namespace AlliancesPlugin.WarOptIn
{
    public class OptinCore
    {

        public static List<long> AllFactionIds = new List<long>();
        public static List<long> FactionsOptedIn = new List<long>();
        public ListOfWarParticipants participants = new ListOfWarParticipants();
        public WarConfig config = new WarConfig();
        public void DoNeutralUpdate(long firstId, long SecondId)
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                MyFactionPeaceRequestState state = MySession.Static.Factions.GetRequestState(firstId, SecondId);
                if (state != MyFactionPeaceRequestState.Sent)
                {
                    Sandbox.Game.Multiplayer.MyFactionCollection.SendPeaceRequest(firstId, SecondId);
                    Sandbox.Game.Multiplayer.MyFactionCollection.AcceptPeace(firstId, SecondId);
                }
                MyFactionStateChange change = MyFactionStateChange.SendPeaceRequest;
                MyFactionStateChange change2 = MyFactionStateChange.AcceptPeace;
                List<object[]> Input = new List<object[]>();
                object[] MethodInput = new object[] { change, firstId, SecondId, 0L };
                AlliancePlugin.sendChange?.Invoke(null, MethodInput);
                object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
                AlliancePlugin.sendChange?.Invoke(null, MethodInput2);
                MySession.Static.Factions.SetReputationBetweenFactions(firstId, SecondId, 0);
            });
        }

        public void SaveFile()
        {
            AlliancePlugin.utils.WriteToJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//OptionalWar//WarParticipants.json", participants);
        }
        public ListOfWarParticipants LoadFile()
        {
            if (!File.Exists(AlliancePlugin.path + "//OptionalWar//WarParticipants.json"))
            {
                AlliancePlugin.utils.WriteToJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//OptionalWar//WarParticipants.json", new ListOfWarParticipants());
            }
            config = AlliancePlugin.utils.ReadFromJsonFile<WarConfig>(AlliancePlugin.path + "//OptionalWar//WarConfig.json");
            return AlliancePlugin.utils.ReadFromJsonFile<ListOfWarParticipants>(AlliancePlugin.path + "//OptionalWar//WarParticipants.json");
        }

        public String getStatus(long id)
        {
            if (participants.FactionsAtWar.Contains(id))
            {
                return "Enabled.";
            }
            else
            {
                return "Disabled.";
            }
        }

        public bool AddToWarParticipants(long id)
        {
            participants = LoadFile();
            if (participants.FactionsAtWar.Contains(id))
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
        public bool RemoveFromWarParticipants(long id)
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
        public void StateChange(MyFactionStateChange change, long fromFacId, long toFacId, long playerId, long senderId)
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
                        if (fac1.Tag.Length > 3 || fac2.Tag.Length > 3)
                        {
                            return;
                        }
                        if (!participants.FactionsAtWar.Contains(fromFacId))
                        {
                            foreach (MyFactionMember m in fac1.Members.Values)
                            {
                                var id = MySession.Static.Players.TryGetSteamId(m.PlayerId);
                                if (id > 0)
                                {
                                    AlliancePlugin.SendChatMessage("War Gods", $"You have not opted in to war. To opt in type !war enable", id);
                                }
                               
                            }
                            DoNeutralUpdate(fromFacId, toFacId);
                            return;
                        }
                        if (!participants.FactionsAtWar.Contains(toFacId))
                        {
                            foreach (MyFactionMember m in fac1.Members.Values)
                            {
                                var id = MySession.Static.Players.TryGetSteamId(m.PlayerId);
                                if (id > 0)
                                {
                                    AlliancePlugin.SendChatMessage("War Gods", $"Target faction has not opted in to war.", id);
                                }

                            }
                            DoNeutralUpdate(fromFacId, toFacId);
                            return;
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

        public void ProcessNewFaction(long newid)
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
