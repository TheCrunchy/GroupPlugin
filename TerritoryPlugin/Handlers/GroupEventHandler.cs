using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Territory.Models;
using Territory.Models.Events;

namespace Territory.Handlers
{
    public static class GroupEventHandler
    {
        public static void HandleGroupJoin(JoinGroupEvent groupEvent)
        {
            if (GroupHandler.LoadedGroups.TryGetValue(groupEvent.JoinedGroupId, out var group))
            {
                group.AddMemberToGroup(groupEvent.FactionId);
                GroupHandler.LoadedGroups[groupEvent.JoinedGroupId] = group;
            }
        }
        public static void HandleGroupLeave(LeftGroupEvent groupEvent)
        {
            if (GroupHandler.LoadedGroups.TryGetValue(groupEvent.JoinedGroupId, out var group))
            {
                group.RemoveMemberFromGroup(groupEvent.FactionId);
                GroupHandler.LoadedGroups[groupEvent.JoinedGroupId] = group;
            }
        }
        public static void HandleGroupChange(GroupChangedEvent groupEvent)
        {
            if (GroupHandler.LoadedGroups.TryGetValue(groupEvent.Group.GroupId, out var group))
            {
                group = groupEvent.Group;
                GroupHandler.LoadedGroups[groupEvent.Group.GroupId] = group;
            }

        }
        public static void HandleGroupDeleted(GroupDeletedEvent groupEvent)
        {
            GroupHandler.RemoveGroup(groupEvent.GroupId);
        }
        public static void HandleGroupCreated(GroupCreatedEvent groupEvent)
        {
            GroupHandler.AddGroup(JsonConvert.DeserializeObject<Group>(groupEvent.CreatedGroup));
        }

        public static void HandleGroupInvite(InvitedToGroupEvent groupEvent)
        {
            if (GroupHandler.LoadedGroups.TryGetValue(groupEvent.GroupId, out var group))
            {
                group.AddInvite(groupEvent.FactionId);
                GroupHandler.LoadedGroups[groupEvent.GroupId] = group;
            }
        }
    }
}
