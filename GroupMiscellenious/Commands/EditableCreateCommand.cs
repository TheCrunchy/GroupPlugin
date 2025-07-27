using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
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

namespace GroupMiscellenious.Commands
{
   public class EditableCreateCommand : CommandModule
    {

        [Command("alphacreategroup", "create")]
        [Permission(MyPromoteLevel.None)]
        public void Create(string groupName, string description = "Default description")
        {
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


    }
}
