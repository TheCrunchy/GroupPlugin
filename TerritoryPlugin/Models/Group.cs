using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Territory.Models
{
    public class Group
    {
        public bool IsPlayerCreatedGroup { get; set; }
        public string GroupName { get; set; }
        public string GroupTag { get; set; }
        public string GroupDescription { get; set; }
        public Guid GroupId = Guid.NewGuid();

        public long GroupLeader { get; set; }
        public List<long> GroupAdmins { get; set; }

        public List<long> GroupMembers { get; set; } = new List<long>();
        //civil war stuff, let the players opt in to being excluded
        public Dictionary<long, List<long>> FriendlyExclusions { get; set; } = new();
        public List<long> Invites { get; set; } = new();
        public void AddMemberToGroup(long factionId)
        {
            Invites.Remove(factionId);
            GroupMembers.Add(factionId);
        }

        public void DeleteGroup()
        {
            foreach (var member in GroupMembers)
            {
                RemoveMemberFromGroup(member);
            }
        }

        public void RemoveMemberFromGroup(long factionId)
        {
            GroupMembers.Remove(factionId);
            foreach (var id in GroupMembers)
            {

                var fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac == null) continue;
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyFactionCollection.DeclareWar(id, factionId);
                });
            }
        }

        public void AddInvite(long factionId)
        {
            Invites.Add(factionId);
        }
        public void ProcessFriendlies()
        {
            foreach (long id in GroupMembers)
            {
                var fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac == null) continue;
                var exclusions = new List<long>();
                if (FriendlyExclusions.ContainsKey(id))
                {
                    exclusions = FriendlyExclusions[id];
                }

                foreach (long id2 in GroupMembers)
                {
                    if (exclusions.Contains(id2))
                    {
                        continue;
                    }
                    var fac2 = MySession.Static.Factions.TryGetFactionById(id2);

                    if (fac2 == null || fac == fac2) continue;
                    if (MySession.Static.Factions.AreFactionsFriends(id, id2)) continue;
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 1500);
                        DoFriendlyUpdate(id, id2);
                    });
                }
            }
        }

        public void DoFriendlyUpdate(long firstId, long SecondId)
        {
            MyFactionStateChange change = MyFactionStateChange.SendFriendRequest;
            MyFactionStateChange change2 = MyFactionStateChange.AcceptFriendRequest;
            List<object[]> Input = new List<object[]>();
            object[] MethodInput = new object[] { change, firstId, SecondId, 0L };
            TerritoryPlugin.sendChange?.Invoke(null, MethodInput);
            object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
            TerritoryPlugin.sendChange?.Invoke(null, MethodInput2);

        }
    }
}
