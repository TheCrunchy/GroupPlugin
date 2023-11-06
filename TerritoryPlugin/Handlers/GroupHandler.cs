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
            MapPlayers();
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
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                var faction = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId);
                if (faction == null) continue;
                var group = LoadedGroups.FirstOrDefault(x => x.Value.GroupMembers.Contains(faction.FactionId)).Value ?? null;
                if (group != null)
                {
                    PlayersGroups.Add((long)player.Id.SteamId, group.GroupId);
                }
            }
        }
    }
}
