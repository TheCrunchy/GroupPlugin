using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using CrunchGroup.Handlers;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using GroupMiscellenious.Commands;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace GroupMiscellenious.Scripts
{
    public static class AutojoinScript
    {
        public static List<string> GroupNamesToAutoJoin = new List<string>() { "GroupTag1", "GroupTag2" };
        public static void Patch(PatchContext ctx)
        {
            MySession.Static.Factions.FactionCreated += FactionsOnFactionCreated;

            foreach (var faction in MySession.Static.Factions)
            {
                if (faction.Value.IsEveryoneNpc())
                {
                    continue;
                }
                ProcessFaction(faction.Value);
            }
        }

        private static void FactionsOnFactionCreated(long Obj)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(Obj);
            if (faction != null)
            {
                ProcessFaction(faction);
            }
        }

        private static void ProcessFaction(IMyFaction faction)
        {
            var groupTag = GroupNamesToAutoJoin.GetRandomItemFromList();
            var group = GroupHandler.GetGroupByTag(groupTag);
            if (group != null)
            {
                group.AddMemberToGroup(faction.FactionId);
            }
        }

        public class AutoJoinCommands : CommandModule
        {
            [Command("joingroup", "switch groups")]
            [Permission(MyPromoteLevel.None)]
            public void JoinGroup(string groupTag)
            {
                var group = GroupHandler.GetGroupByTag(groupTag);
                if (group == null)
                {
                    Context.Respond("Group not found", $"{Core.PluginName}");
                    return;
                }

                if (group.IsPlayerCreatedGroup)
                {
                    Context.Respond("Cannot auto join player made groups.");
                    return;
                }
                var faction = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("Player Faction not found", $"{Core.PluginName}");
                    return;
                }

                if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
                {
                    Context.Respond("Only leaders and founders may join groups.", $"{Core.PluginName}");
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
        }
    }
}
