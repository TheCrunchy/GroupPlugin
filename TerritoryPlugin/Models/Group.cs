using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CrunchGroup.Models
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
            if (GroupMembers.Contains(factionId))
            {
                return;
            }

            Invites.Remove(factionId);
            GroupMembers.Add(factionId);
            var newfac = MySession.Static.Factions.TryGetFactionById(factionId);
            foreach (var fac in GroupMembers)
            {
                var faction = MySession.Static.Factions.TryGetFactionById(fac);
                foreach (var member in faction.Members.Values.Select(x => x.PlayerId).Distinct())
                {
                    Core.SendChatMessage($"{Core.PluginName}", $"{newfac.Name} {newfac.Tag} Has joined the group!", MySession.Static.Players.TryGetSteamId(member));
                }
            }
        }

        public void DeleteGroup()
        {
            foreach (var member in GroupMembers.ToList())
            {
                RemoveMemberFromGroup(member);
            }

            GroupMembers = new List<long>();
        }

        public void RemoveMemberFromGroup(long factionId)
        {
            var newfac = MySession.Static.Factions.TryGetFactionById(factionId);
            foreach (var id in GroupMembers)
            {
                var fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac == null) continue;
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyFactionCollection.DeclareWar(id, factionId);
                });
                foreach (var member in fac.Members.Values.Select(x => x.PlayerId).Distinct())
                {
                    Core.SendChatMessage($"{Core.PluginName}", $"{newfac.Name} {newfac.Tag} Has left the group!", MySession.Static.Players.TryGetSteamId(member));
                }
            }

            GroupMembers.Remove(factionId);
        }

        public void AddInvite(long factionId)
        {
            Invites.Add(factionId);
            var faction = MySession.Static.Factions.TryGetFactionById(factionId);
            foreach (var member in faction.Members)
            {
                Core.SendChatMessage($"{Core.PluginName}", $"You were invited to {GroupName} to accept use !group join {GroupTag} or !group join {GroupName}", MySession.Static.Players.TryGetSteamId(member.Value.PlayerId));
            }
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
            Core.sendChange?.Invoke(null, MethodInput);
            object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
            Core.sendChange?.Invoke(null, MethodInput2);

        }
    }
}
