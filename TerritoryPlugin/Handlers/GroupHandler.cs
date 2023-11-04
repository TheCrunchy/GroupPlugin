using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.LayoutRenderers;
using Sandbox.ModAPI.Ingame;
using Territory.Models;

namespace Territory.Handlers
{
    public static class GroupHandler
    {
        public static Dictionary<Guid, Group> LoadedGroups { get; set; } = new Dictionary<Guid, Group>();
        public static Dictionary<long, Guid> PlayersGroups { get; set; } = new();
        private static int ticks = 0;
        public static void DoGroupLoop()
        {
            ticks++;
            if (ticks % 100 == 0)
            {
                ProcessFriendlies();
                MapPlayers();
            }
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
            return null;
        }

        public static void MapPlayers()
        {

        }
    }
}
