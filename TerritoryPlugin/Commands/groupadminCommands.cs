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
            Context.Respond("Group created.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("add", "add to a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddMember(string groupTag, string factionTag)
        {
            var group = GroupHandler.GetGroupByTag(groupTag);
            if (group == null)
            {
                Context.Respond("Group not found", $"{Core.PluginName}");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{Core.PluginName}");
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
                Context.Respond($"Faction was in group {inGroup.Value.GroupName} {inGroup.Value.GroupTag}, they have been kicked from it.", $"{Core.PluginName}");
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

            Context.Respond("Group member added.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("remove", "remove from a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveMember(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{Core.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            foreach (var inGroup in IsInGroup.ToList())
            {
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
                Context.Respond($"Faction was in group {inGroup.Value.GroupName} {inGroup.Value.GroupTag}, they have been kicked from it.", $"{Core.PluginName}");
            }

            Context.Respond("Removed the faction from any groups they were members of.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("delete", "delete a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void Delete(string groupNameOrTag)
        {
            var group = GroupHandler.GetGroupByTag(groupNameOrTag);

            if (group == null)
            {
                Context.Respond("Group not found", $"{Core.PluginName}");
                return;
            }
            group.DeleteGroup();
            Storage.StorageHandler.Delete(group);
            GroupHandler.RemoveGroup(group.GroupId);
            var Event = new GroupEvent();
            var createdEvent = new GroupDeletedEvent()
            {
                GroupId = group.GroupId
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);
            Context.Respond("Group deleted and moved to archive.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("edit", "edit a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void Edit(string groupNameOrTag, string fieldType, string newValue)
        {
            var group = GroupHandler.GetGroupByTag(groupNameOrTag);

            if (group == null)
            {
                Context.Respond("Group not found", $"{Core.PluginName}");
                return;
            }


            switch (fieldType.ToLower())
            {
                case "name":
                    if (GroupHandler.LoadedGroups.Any(x =>
                            x.Value.GroupName != null && x.Value.GroupName.ToLower() == newValue.ToLower()))
                    {
                        Context.Respond("Group with that name already exists");
                        return;
                    }
                    group.GroupName = newValue;
                    break;
                case "tag":
                    if (GroupHandler.LoadedGroups.Any(x =>
                            x.Value.GroupTag != null && x.Value.GroupTag.ToLower() == newValue.ToLower()))
                    {
                        Context.Respond("Group with that tag already exists");
                        return;
                    }
                    group.GroupTag = newValue;
                    break;
                case "description":
                    group.GroupDescription = newValue;
                    break;
                case "leader":
                    MyIdentity id = Core.TryGetIdentity(newValue);
                    if (id == null)
                    {
                        Context.Respond("Could not find that player");
                        return;
                    }
                    group.GroupLeader = (long)MySession.Static.Players.TryGetSteamId(id.IdentityId);
                    break;
                default:
                    Context.Respond("Valid editable fields are Name, Tag, Description, Leader");
                    return;
            }

            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new GroupChangedEvent()
            {
                Group = JsonConvert.SerializeObject(group)
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);
            Context.Respond("Group edited.", $"{Core.PluginName}");

        }
    }
}
