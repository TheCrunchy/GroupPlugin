using System;
using System.Linq;
using System.Text;
using CrunchGroup.Handlers;
using CrunchGroup.Models;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace CrunchGroup.Commands
{
    [Category("group")]
    public class GroupCommands : CommandModule
    {
        public bool FailsGroupChecks()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can create groups", $"{GroupPlugin.PluginName}");
                return true;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group != null)
            {
                Context.Respond("You are already a member of a group! you must leave it first with !group leave", $"{GroupPlugin.PluginName}");
                return true;
            }

            return false;
        }
        public Group IsInGroup()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("You are not a group member.", $"{GroupPlugin.PluginName}");
                return null;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("You are not a group member.", $"{GroupPlugin.PluginName}");
                return null;
            }

            return group;
        }

        [Command("list", "list all groups")]
        [Permission(MyPromoteLevel.None)]
        public void List()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in GroupHandler.LoadedGroups)
            {
                sb.AppendLine($"{item.Value.GroupName} - [{item.Value.GroupTag}]");
            }

            if (Context.Player != null)
            {
                DialogMessage m = new DialogMessage($"Group List", "", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(sb.ToString());
            }
        }

        [Command("check", "check what group a faciton is in")]
        [Permission(MyPromoteLevel.None)]
        public void Check(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{GroupPlugin.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            if (IsInGroup.Any())
            {
                var inGroup = IsInGroup.First();
                Context.Respond($"{inGroup.Value.GroupName} {inGroup.Value.GroupTag}", $"{GroupPlugin.PluginName}");
                return;
            }
            Context.Respond($"Target faction not a member of a group.", $"{GroupPlugin.PluginName}");
        }

        [Command("info", "info on a group")]
        [Permission(MyPromoteLevel.None)]
        public void info(string groupNameOrTag)
        {
            var group = GroupHandler.GetGroupByTag(groupNameOrTag);
            if (group == null)
            {
                Context.Respond("Group not found.",$"{GroupPlugin.PluginName}");
                return;
            }
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Name: {group.GroupName}");
            sb.AppendLine($"Tag: {group.GroupTag}");
            sb.AppendLine($"Description: {group.GroupTag}");
            sb.AppendLine($"Leader: {MySession.Static.Players.TryGetIdentityNameFromSteamId((ulong)group.GroupLeader)}");
            sb.AppendLine("");

            sb.AppendLine("Member factions");
            foreach (var fac in group.GroupMembers)
            {
                var faction = MySession.Static.Factions.TryGetFactionById(fac);
                if (faction != null)
                {
                    sb.AppendLine($"{faction.Name} {faction.Tag}");
                }
            }

            if (Context.Player != null)
            {
                DialogMessage m = new DialogMessage($"Group Info {group.GroupName}", "", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(sb.ToString());
            }
        }


        [Command("create", "create a group")]
        [Permission(MyPromoteLevel.None)]
        public void Create(string groupName, string description = "Default description")
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }
            if (FailsGroupChecks())
            {
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder can create a group", $"{GroupPlugin.PluginName}");
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
            Context.Respond("Group created.", $"{GroupPlugin.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("edit", "edit a group")]
        [Permission(MyPromoteLevel.None)]
        public void Edit(string fieldType, string newValue)
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();

            if (group == null)
            {
                Context.Respond("You are not a member of a group", $"{GroupPlugin.PluginName}");
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId)
            {
                Context.Respond("You are not the group leader.", $"{GroupPlugin.PluginName}");
                return;
            }

            switch (fieldType.ToLower())
            {
                case "name":
                    group.GroupName = newValue;
                    break;
                case "tag":
                    group.GroupTag = newValue;
                    break;
                case "description":
                    group.GroupDescription = newValue;
                    break;
                case "leader":
                    MyIdentity id = GroupPlugin.TryGetIdentity(newValue);
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
            Context.Respond("Group edited.", $"{GroupPlugin.PluginName}");
        }


        [Command("delete", "delete a group")]
        [Permission(MyPromoteLevel.None)]
        public void Delete()
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();
            if (group == null)
            {
                Context.Respond("You are not a member of a group", $"{GroupPlugin.PluginName}");
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId)
            {
                Context.Respond("You are not the group leader.", $"{GroupPlugin.PluginName}");
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
            Context.Respond("Group deleted and moved to archive.", $"{GroupPlugin.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("invite", "invite to group")]
        [Permission(MyPromoteLevel.None)]
        public void invite(string targetTag)
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId && group.GroupAdmins.Contains((long)Context.Player.SteamUserId))
            {
                Context.Respond("You are not the group leader or a group admin.", $"{GroupPlugin.PluginName}");
                return;
            }

            var targetFac = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (targetFac == null)
            {
                Context.Respond("Target faction not found", $"{GroupPlugin.PluginName}");
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

            Context.Respond("Invite sent.", $"{GroupPlugin.PluginName}");
        }

        [Command("join", "join a group")]
        [Permission(MyPromoteLevel.None)]
        public void join(string groupTag)
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can join groups", $"{GroupPlugin.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder or leaders can join a group", $"{GroupPlugin.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            if (IsInGroup.Any())
            {
                Context.Respond($"You are already a member of the group {IsInGroup.First().Value.GroupName}, you must leave that first with !group leave", $"{GroupPlugin.PluginName}");
                return;
            }
            var group = GroupHandler.GetGroupByTag(groupTag) ?? GroupHandler.GetGroupByTag(groupTag);
            if (group == null)
            {
                Context.Respond("Target group not found, see all groups with !group list", $"{GroupPlugin.PluginName}");
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
            GroupHandler.MapPlayers();
        }

        [Command("leave", "leave a group")]
        [Permission(MyPromoteLevel.None)]
        public void leave()
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("Only factions can join groups", $"{GroupPlugin.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond("Only the founder can leave a group", $"{GroupPlugin.PluginName}");
                return;
            }

            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("Could not find group, are you sure you are a member of one?", $"{GroupPlugin.PluginName}");
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
            Context.Respond("Left the group.", $"{GroupPlugin.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("kick", "kick from a group")]
        [Permission(MyPromoteLevel.None)]
        public void kick(string targetTag)
        {
            if (!GroupPlugin.config.PlayerGroupsEnabled)
            {
                Context.Respond("Player made groups are not enabled.", $"{GroupPlugin.PluginName}");
                return;
            }
            var faction = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{GroupPlugin.PluginName}");
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
                Context.Respond("You are not the group leader.", $"{GroupPlugin.PluginName}");
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
            Context.Respond("Kicked from the group.", $"{GroupPlugin.PluginName}");
            GroupHandler.MapPlayers();
        }
    }
}
