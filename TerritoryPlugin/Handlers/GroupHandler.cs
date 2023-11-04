﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.LayoutRenderers;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Territory.Models;

namespace Territory.Handlers
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

        public static Group GetGroupByName(string name)
        {
            return LoadedGroups.FirstOrDefault(x => x.Value.GroupName == name).Value ?? null;
        }


        public static Group GetGroupByTag(string tag)
        {
            return LoadedGroups.FirstOrDefault(x => x.Value.GroupTag == tag).Value ?? null;
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