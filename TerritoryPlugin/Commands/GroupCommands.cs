using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Territory.Handlers;
using Territory.Models;
using Territory.Models.Events;
using Territory.NexusStuff;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Territory.Commands
{
    [Category("group")]
    public class GroupCommands : CommandModule
    {
        public bool FailsGroupChecks()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can create groups", $"{TerritoryPlugin.PluginName}");
                return true;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group != null)
            {
                Context.Respond("You are already a member of a group! you must leave it first with !group leave", $"{TerritoryPlugin.PluginName}");
                return true;
            }

            return false;
        }
        public Group IsInGroup()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("You are not a group member.", $"{TerritoryPlugin.PluginName}");
                return null;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("You are not a group member.", $"{TerritoryPlugin.PluginName}");
                return null;
            }

            return group;
        }

        [Command("create", "create a group")]
        [Permission(MyPromoteLevel.None)]
        public void Create(string groupName, string description = "Default description")
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }
            if (FailsGroupChecks())
            {
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder can create a group", $"{TerritoryPlugin.PluginName}");
                return;
            }
            var group = new Group()
            {
                GroupName = groupName,
                GroupId = Guid.NewGuid(),
                GroupDescription = description,
                GroupLeader = (long)Context.Player.SteamUserId,
            }; 
           
            group.GroupMembers.Add(faction.FactionId);

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
            Context.Respond("Group created.", $"{TerritoryPlugin.PluginName}");
        }


        [Command("delete", "delete a group")]
        [Permission(MyPromoteLevel.None)]
        public void Delete()
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            if (group.GroupLeader != (long) Context.Player.SteamUserId)
            {
                Context.Respond("You are not the group leader.", $"{TerritoryPlugin.PluginName}");
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
            Context.Respond("Group deleted and moved to archive.", $"{TerritoryPlugin.PluginName}");
        }

        [Command("invite", "invite to group")]
        [Permission(MyPromoteLevel.None)]
        public void invite(string targetTag)
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId && group.GroupAdmins.Contains((long)Context.Player.SteamUserId))
            {
                Context.Respond("You are not the group leader or a group admin.", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var targetFac = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (targetFac == null)
            {
                Context.Respond("Target faction not found",$"{TerritoryPlugin.PluginName}");
                return;
            }

            group.AddInvite(targetFac.FactionId);
            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new InvitedToGroupEvent()
            {
                GroupId = group.GroupId,
                FactionId = targetFac.FactionId
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);

            Context.Respond("Invite sent.", $"{TerritoryPlugin.PluginName}");
        }

        [Command("join", "join a group")]
        [Permission(MyPromoteLevel.None)]
        public void join(string groupTag)
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can join groups", $"{TerritoryPlugin.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder or leaders can join a group", $"{TerritoryPlugin.PluginName}");
                return;
            }
            var group = GroupHandler.GetGroupByTag(groupTag);
            if (group == null)
            {
                group = GroupHandler.GetGroupByName(groupTag);
            }

            if (group == null)
            {
                Context.Respond("Target group not found, see all groups with !group list", $"{TerritoryPlugin.PluginName}");
                return;
            }
            if (group.Invites.Contains(faction.FactionId))
            {
                group.AddMemberToGroup(faction.FactionId);
            }
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
        }

        [Command("leave", "leave a group")]
        [Permission(MyPromoteLevel.None)]
        public void leave()
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can join groups", $"{TerritoryPlugin.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder can leave a group", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("Could not find group, are you sure you are a member of one?", $"{TerritoryPlugin.PluginName}");
                return;
            }
            if (group.GroupMembers.Contains(faction.FactionId))
            {
                group.RemoveMemberFromGroup(faction.FactionId);
            }
            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new LeftGroupEvent()
            {
                JoinedGroupId = group.GroupId,
                FactionId = faction.FactionId
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);
            Context.Respond("Left the group.", $"{TerritoryPlugin.PluginName}");
        }

        [Command("kick", "kick from a group")]
        [Permission(MyPromoteLevel.None)]
        public void kick(string targetTag)
        {
            if (!TerritoryPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{TerritoryPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{TerritoryPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            // && group.GroupAdmins.Contains((long)Context.Player.SteamUserId)
            if (group.GroupLeader != (long)Context.Player.SteamUserId)
            {
                Context.Respond("You are not the group leader.", $"{TerritoryPlugin.PluginName}");
                return;
            }

            if (group.GroupMembers.Contains(faction.FactionId))
            {
                group.RemoveMemberFromGroup(faction.FactionId);
            }
            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new LeftGroupEvent()
            {
                JoinedGroupId = group.GroupId,
                FactionId = faction.FactionId
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);
            Context.Respond("Kicked from the group.", $"{TerritoryPlugin.PluginName}");
        }
    }
}
