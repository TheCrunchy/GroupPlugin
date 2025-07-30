using System;
using System.Collections.Generic;
using System.Linq;
using CrunchGroup.Models;
using Sandbox.Game.World;

namespace CrunchGroup.Handlers
{
    public static class GroupHandler
    {
        public static Dictionary<Guid, Group> LoadedGroups { get; set; } = new Dictionary<Guid, Group>();
        public static Dictionary<long, Guid> PlayersGroups { get; set; } = new();
        public static void DoGroupLoop()
        {
            ProcessFriendlies();
          
        }

        public static Group GetGroupByTag(string tag)
        {
            return LoadedGroups.FirstOrDefault(x => x.Value.GroupTag == tag).Value ?? LoadedGroups.FirstOrDefault(x => x.Value.GroupName == tag).Value ?? null;
        }
        public static Group GetGroupById(Guid id)
        {
            return LoadedGroups.FirstOrDefault(x => x.Value.GroupId == id).Value ?? null;
        }
        public static Group GetFactionsGroup(long factionId)
        {
            return LoadedGroups.FirstOrDefault(x => x.Value.GroupMembers.Contains(factionId)).Value ?? null;
        }

        public static void AddGroup(Group group)
        {
            if (LoadedGroups.ContainsKey(group.GroupId))
            {
                LoadedGroups[group.GroupId] = group;
            }
            else
            {
                LoadedGroups.Add(group.GroupId, group);
            }
        }

        public static void RemoveGroup(Guid group)
        {
            var yeet = PlayersGroups.Where(x => x.Value == group).Select(player => player.Key).ToList();

            foreach (var item in yeet)
            {
                PlayersGroups.Remove(item);
            }
         
            LoadedGroups.Remove(group);
        }

        public static void ProcessFriendlies()
        {
            foreach (var item in LoadedGroups.Values)
            {
                item.ProcessFriendlies();
            }
        }


        public static Group GetPlayersGroup(long steamId)
        {
            return PlayersGroups.ContainsKey(steamId) ? LoadedGroups[PlayersGroups[steamId]] : null;
        }

        public static void MapPlayers()
        {
            PlayersGroups.Clear();

            foreach (var group in LoadedGroups.Values)
            {
                foreach (var factionId in group.GroupMembers)
                {
                    var faction = MySession.Static.Factions.TryGetFactionById(factionId);
                    if (faction == null) continue;

                    foreach (var member in faction.Members.Values.Select(x => x.PlayerId).Distinct())
                    {
                        var steam = MySession.Static.Players.TryGetSteamId(member);
                        if (steam == 0l)
                        {
                            continue;
                        }
                       
                        PlayersGroups[(long)steam] = group.GroupId;
                    }
                }
            }
        }
    }
}
