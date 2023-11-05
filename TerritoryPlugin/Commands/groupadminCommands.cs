using System;
using System.Linq;
using CrunchGroup.Handlers;
using CrunchGroup.Models;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace CrunchGroup.Commands
{
    [Category("groupadmin")]
    public class GroupadminCommands : CommandModule
    {
        [Command("create", "create a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void Create(string groupName, string description = "Default description")
        {
            var group = new Group()
            {
                GroupName = groupName,
                GroupId = Guid.NewGuid(),
                GroupDescription = description,
            };

            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new GroupCreatedEvent()
            {
                CreatedGroup = JsonConvert.SerializeObject(group)
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);
            Context.Respond("Group deleted.", $"{TerritoryPlugin.PluginName}");
        }

        [Command("add", "add to a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddMember(string groupTag, string factionTag)
        {
            var group = GroupHandler.GetGroupByTag(groupTag);
            if (group == null)
            {
                Context.Respond("Group not found", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            if (IsInGroup.Any())
            {
                var inGroup = IsInGroup.First();
                inGroup.Value.RemoveMemberFromGroup(faction.FactionId);
                var LeaveEvent = new GroupEvent();
                var LeavecreatedEvent = new LeftGroupEvent()
                {
                    JoinedGroupId = inGroup.Value.GroupId,
                    FactionId = faction.FactionId
                };
                LeaveEvent.EventObject = MyAPIGateway.Utilities.SerializeToBinary(LeavecreatedEvent);
                LeaveEvent.EventType = LeavecreatedEvent.GetType().Name;
                Storage.StorageHandler.Save(inGroup.Value);
                GroupHandler.AddGroup(inGroup.Value);
                NexusHandler.RaiseEvent(LeaveEvent);
                Context.Respond($"Faction was in group {inGroup.Value.GroupName} {inGroup.Value.GroupTag}, they have been kicked from it.", $"{TerritoryPlugin.PluginName}");
            }
            group.AddMemberToGroup(faction.FactionId);
            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new JoinGroupEvent()
            {
                JoinedGroupId = group.GroupId,
                FactionId = faction.FactionId
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);

            Context.Respond("Group member added.", $"{TerritoryPlugin.PluginName}");
        }

        [Command("add", "add to a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveMember(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            if (IsInGroup.Any())
            {
                var inGroup = IsInGroup.First();
                inGroup.Value.RemoveMemberFromGroup(faction.FactionId);
                var LeaveEvent = new GroupEvent();
                var LeavecreatedEvent = new LeftGroupEvent()
                {
                    JoinedGroupId = inGroup.Value.GroupId,
                    FactionId = faction.FactionId
                };
                LeaveEvent.EventObject = MyAPIGateway.Utilities.SerializeToBinary(LeavecreatedEvent);
                LeaveEvent.EventType = LeavecreatedEvent.GetType().Name;
                NexusHandler.RaiseEvent(LeaveEvent);
                Storage.StorageHandler.Save(inGroup.Value);
                GroupHandler.AddGroup(inGroup.Value);
                Context.Respond($"Faction was in group {inGroup.Value.GroupName} {inGroup.Value.GroupTag}, they have been kicked from it.", $"{TerritoryPlugin.PluginName}");
                return;
            }
           
            Context.Respond("Faction was not a member of a group.", $"{TerritoryPlugin.PluginName}");
        }
    }
}
