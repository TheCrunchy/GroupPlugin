using System;
using System.Linq;
using System.Net.Http.Headers;
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
    [Category(Core.PluginCommandPrefix)]
    public class GroupCommands : CommandModule
    {
        public bool FailsGroupChecks()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond($"Only factions can create {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return true;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group != null)
            {
                Context.Respond($"You are already a member of a {Core.PluginCommandPrefix}! you must leave it first with !{Core.PluginCommandPrefix} leave", $"{Core.PluginName}");
                return true;
            }

            return false;
        }
        public Group IsInGroup()
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond($"You are not a {Core.PluginCommandPrefix} member.", $"{Core.PluginName}");
                return null;
            }
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond($"You are not a {Core.PluginCommandPrefix} member.", $"{Core.PluginName}");
                return null;
            }

            return group;
        }

        [Command("list", "list all")]
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
                DialogMessage m = new DialogMessage($"{Core.PluginCommandPrefix} List", "", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(sb.ToString());
            }
        }

        [Command("check", "check what a faction is in")]
        [Permission(MyPromoteLevel.None)]
        public void Check(string factionTag)
        {
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
                Context.Respond($"{inGroup.Value.GroupName} {inGroup.Value.GroupTag}", $"{Core.PluginName}");
                return;
            }
            Context.Respond($"Target faction not a member of a {Core.PluginCommandPrefix}.", $"{Core.PluginName}");
        }

        [Command("info", "display info")]
        [Permission(MyPromoteLevel.None)]
        public void info(string groupNameOrTag = "")
        {
            var group = GroupHandler.GetGroupByTag(groupNameOrTag);
            if (group == null)
            {
                group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
                if (group == null)
                {
                    Context.Respond($"{Core.PluginCommandPrefix} not found.", $"{Core.PluginName}");
                    return;
                }
            }
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Name: {group.GroupName}");
            sb.AppendLine($"Tag: {group.GroupTag}");
            sb.AppendLine($"Description: {group.GroupDescription}");
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
                DialogMessage m = new DialogMessage($"{Core.PluginCommandPrefix} Info {group.GroupName}", "", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(sb.ToString());
            }
        }


        [Command("create", "create")]
        [Permission(MyPromoteLevel.None)]
        public void Create(string groupName, string description = "Default description")
        {
            if (!Core.config.PlayerGroupsEnabled)
            {
                Context.Respond($"Player made {Core.PluginCommandPrefix}s are not enabled.", $"{Core.PluginName}");
                return;
            }
            if (FailsGroupChecks())
            {
                return;
            }
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond($"Only the founder can create a {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return;
            }
            if (GroupHandler.LoadedGroups.Any(x =>
                    x.Value.GroupName != null && x.Value.GroupName.ToLower() == groupName.ToLower()))
            {
                Context.Respond($"{Core.PluginCommandPrefix} with that name already exists");
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
            Context.Respond($"{Core.PluginCommandPrefix} created.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("edit", "edit")]
        [Permission(MyPromoteLevel.None)]
        public void Edit(string fieldType, string newValue)
        {

            var group = IsInGroup();

            if (group == null)
            {
                Context.Respond($"You are not a member of a {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId)
            {
                Context.Respond($"You are not the {Core.PluginCommandPrefix} leader.", $"{Core.PluginName}");
                return;
            }

            switch (fieldType.ToLower())
            {
                case "name":
                    if (GroupHandler.LoadedGroups.Any(x =>
                            x.Value.GroupName != null && x.Value.GroupName.ToLower() == newValue.ToLower()))
                    {
                        Context.Respond($"{Core.PluginCommandPrefix} with that name already exists");
                        return;
                    }
                    group.GroupName = newValue;
                    break;
                case "tag":
                    if (GroupHandler.LoadedGroups.Any(x =>
                            x.Value.GroupTag != null && x.Value.GroupTag.ToLower() == newValue.ToLower()))
                    {
                        Context.Respond($"{Core.PluginCommandPrefix} with that tag already exists");
                        return;
                    }
                    group.GroupTag = newValue;
                    break;
                case "description":
                    group.GroupDescription = newValue;
                    break;
                case "leader":
                    {
                        MyIdentity id = Core.TryGetIdentity(newValue);
                        if (id == null)
                        {
                            Context.Respond("Could not find that player");
                            return;
                        }

                        group.GroupLeader = (long)MySession.Static.Players.TryGetSteamId(id.IdentityId);
                        break;
                    }
                case "admins":
                    {
                        MyIdentity id = Core.TryGetIdentity(newValue);
                        if (id == null)
                        {
                            Context.Respond("Could not find that player");
                            return;
                        }
                        var steam = MySession.Static.Players.TryGetSteamId(id.IdentityId);
                        if (group.GroupAdmins.Contains(steam))
                        {
                            group.GroupAdmins.Remove(steam);
                            Context.Respond("Player removed from admins.");
                        }
                        else
                        {
                            group.GroupAdmins.Add(steam);
                            Context.Respond("Player added to admins.");
                        }
                        break;
                    }
                case "webhook":
                    group.DiscordWebhook = newValue;
                    break;
                default:
                    Context.Respond("Valid editable fields are Name, Tag, Description, Leader, Webhook, admins");
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
            Context.Respond($"{Core.PluginCommandPrefix} edited.", $"{Core.PluginName}");
        }


        [Command("delete", "delete")]
        [Permission(MyPromoteLevel.None)]
        public void Delete()
        {
            if (!Core.config.PlayerGroupsEnabled)
            {
                Context.Respond($"Player made {Core.PluginCommandPrefix}s are not enabled.", $"{Core.PluginName}");
                return;
            }

            var group = IsInGroup();
            if (group == null)
            {
                Context.Respond($"You are not a member of a {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId)
            {
                Context.Respond($"You are not the {Core.PluginCommandPrefix} leader.", $"{Core.PluginName}");
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
            Context.Respond($"{Core.PluginCommandPrefix} deleted and moved to archive.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("invite", "invite")]
        [Permission(MyPromoteLevel.None)]
        public void invite(string targetTag)
        {

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId && !group.GroupAdmins.Contains(Context.Player.SteamUserId))
            {
                Context.Respond($"You are not the {Core.PluginCommandPrefix} leader or a {Core.PluginCommandPrefix} admin.", $"{Core.PluginName}");
                return;
            }

            var targetFac = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (targetFac == null)
            {
                Context.Respond("Target faction not found", $"{Core.PluginName}");
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

            Context.Respond("Invite sent.", $"{Core.PluginName}");
        }

        [Command("war", "war dec another group, or remove from the enemy list")]
        [Permission(MyPromoteLevel.None)]
        public void war(string targetTag)
        {

            var group = IsInGroup();

            if (group == null)
            {
                return;
            }
            if (group.GroupLeader != (long)Context.Player.SteamUserId && !group.GroupAdmins.Contains(Context.Player.SteamUserId))
            {
                Context.Respond($"You are not the {Core.PluginCommandPrefix} leader or a {Core.PluginCommandPrefix} admin.", $"{Core.PluginName}");
                return;
            }

            var target = GroupHandler.GetGroupByTag(targetTag);
            if (target == null)
            {
                Context.Respond($"{Core.PluginCommandPrefix} not found.", $"{Core.PluginName}");
                return;
            }

            if (!group.GroupEnemies.Remove(target.GroupId))
            {
                Context.Respond("Added to enemies");
                group.GroupEnemies.Add(target.GroupId);
            }
            else
            {
                Context.Respond("Removed from enemies");
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

            Context.Respond("Invite sent.", $"{Core.PluginName}");
        }


        [Command("join", "join")]
        [Permission(MyPromoteLevel.None)]
        public void join(string groupTag)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond($"Only factions can join {Core.PluginCommandPrefix}s", $"{Core.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
            {
                Context.Respond($"Only the founder or leaders can join a {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return;
            }

            var IsInGroup = GroupHandler.LoadedGroups.Where(x => x.Value.GroupMembers.Contains(faction.FactionId));
            if (IsInGroup.Any())
            {
                Context.Respond($"You are already a member of the {Core.PluginCommandPrefix} {IsInGroup.First().Value.GroupName}, you must leave that first with !{Core.PluginCommandPrefix} leave", $"{Core.PluginName}");
                return;
            }
            var group = GroupHandler.GetGroupByTag(groupTag) ?? GroupHandler.GetGroupByTag(groupTag);
            if (group == null)
            {
                Context.Respond($"Target {Core.PluginCommandPrefix} not found, see all {Core.PluginCommandPrefix}s with !{Core.PluginCommandPrefix} list", $"{Core.PluginName}");
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

        [Command("leave", "leave")]
        [Permission(MyPromoteLevel.None)]
        public void leave()
        {

            var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond($"Only factions can join {Core.PluginCommandPrefix}s", $"{Core.PluginName}");
                return;
            }
            if (!faction.IsFounder(Context.Player.IdentityId))
            {
                Context.Respond($"Only the founder can leave a {Core.PluginCommandPrefix}", $"{Core.PluginName}");
                return;
            }

            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond($"Could not find {Core.PluginCommandPrefix}, are you sure you are a member of one?", $"{Core.PluginName}");
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
            Context.Respond($"Left the {Core.PluginCommandPrefix}.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }

        [Command("kick", "kick from")]
        [Permission(MyPromoteLevel.None)]
        public void kick(string targetTag)
        {

            var faction = MySession.Static.Factions.TryGetFactionByTag(targetTag);
            if (faction == null)
            {
                Context.Respond("Faction not found", $"{Core.PluginName}");
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
                Context.Respond($"You are not the {Core.PluginCommandPrefix} leader.", $"{Core.PluginName}");
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
            Context.Respond($"Kicked from the {Core.PluginCommandPrefix}.", $"{Core.PluginName}");
            GroupHandler.MapPlayers();
        }
    }
}
