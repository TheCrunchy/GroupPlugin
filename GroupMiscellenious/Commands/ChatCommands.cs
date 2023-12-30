using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using CrunchGroup;
using CrunchGroup.Handlers;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using ProtoBuf;
using Sandbox.ModAPI;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers.ChatManager;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRageMath;
namespace GroupMiscellenious.Commands
{
    [PatchShim]
    public class ChatCommands : CommandModule
    {
        public static void Patch(PatchContext ctx)
        {

            Core.Session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += LoadLogin;
            Core.Session.Managers.GetManager<ChatManagerServer>().MessageProcessing += DoChatMessage;
            NexusHandler.NexusMessage += NexusMessage;
        }

        public static void DoChatMessage(TorchChatMessage msg, ref bool consumed)
        {

            if (msg.AuthorSteamId == null)
            {
                return;
            }

            if (msg.Channel == Sandbox.Game.Gui.ChatChannel.Private || msg.Channel == Sandbox.Game.Gui.ChatChannel.Faction)
            {
                return;
            }

            if (msg.Message.StartsWith("!"))
            {
                return;
            }

            if (InGroupChat.TryGetValue((ulong)msg.AuthorSteamId, out var inChat))
            {
                if (!inChat)
                {
                    return;
                }
                var group = GroupHandler.GetPlayersGroup((long)msg.AuthorSteamId);
                if (group == null)
                {
                    InGroupChat.Remove((ulong)msg.AuthorSteamId);
                    return;
                }
                else
                {
                    var message = new NotificationMessage("Message sent to group chat, toggle chat with !gc", 3000, "Green");
                    consumed = true;
                    ModCommunication.SendMessageTo(message, (ulong)msg.AuthorSteamId);

                    var Event = new GroupEvent();
                    var createdEvent = new GroupChatEvent()
                    {
                        SenderName = msg.Author,
                        SenderId = (ulong)msg.AuthorSteamId,
                        GroupId = group.GroupId,
                        Message = msg.Message
                    };
                    Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
                    Event.EventType = createdEvent.GetType().Name;

                    NexusHandler.RaiseEvent(Event);
                    if (Core.NexusInstalled)
                    {
                        NexusHandler.NexusMessage?.Invoke(Event);
                    }
                }
            }
        }

        [ProtoContract]
        public class GroupChatEvent
        {
            [ProtoMember(1)]
            public Guid GroupId { get; set; }
            [ProtoMember(2)]
            public ulong SenderId { get; set; }
            [ProtoMember(3)]
            public string SenderName { get; set; }

            [ProtoMember(4)]
            public string Message { get; set; }
        }

        [ProtoContract]
        public class ToggleGroupChatEvent
        {
            [ProtoMember(1)]
            public ulong SenderId { get; set; }
            [ProtoMember(2)]
            public bool InChat { get; set; }
        }
        [ProtoContract]
        public class GroupDistressEvent
        {
            [ProtoMember(1)]
            public Guid GroupId { get; set; }
            [ProtoMember(2)]
            public ulong SenderId { get; set; }
            [ProtoMember(3)]
            public Vector3 Position { get; set; }

        }


        public static void LoadLogin(IPlayer player)
        {
            //load the player to see if they are in chat 
        }

        public static void NexusMessage(GroupEvent message)
        {
            switch (message.EventType)
            {
                case "GroupChatEvent":
                    {
                        var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupChatEvent>(message.EventObject);
                        var group = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Key == ev.GroupId).Value ?? null;
                        if (group == null)
                        {
                            return;
                        }
                        group.SendGroupMessage(ev.SenderId, ev.SenderName, ev.Message);
                        break;
                    }
                case "GroupDistressEvent":
                    {
                        var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupDistressEvent>(message.EventObject);
                        var group = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Key == ev.GroupId).Value ?? null;
                        if (group == null)
                        {
                            return;
                        }
                        group.SendGroupSignal(ev.Position);
                        break;
                    }
                case "ToggleGroupChatEvent":
                {
                    var ev = MyAPIGateway.Utilities.SerializeFromBinary<ToggleGroupChatEvent>(message.EventObject);
                    InGroupChat[ev.SenderId] = ev.InChat;
                    break;
                }
            }
        }

        private static Dictionary<ulong, bool> InGroupChat = new Dictionary<ulong, bool>();


        [Command("gc", "toggle group chat")]
        [Permission(MyPromoteLevel.None)]
        public void Chat()
        {
            var Event = new GroupEvent();
            var createdEvent = new ToggleGroupChatEvent();
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("Group not found.", $"{Core.PluginName}");
                Event = new GroupEvent();
                createdEvent = new ToggleGroupChatEvent()
                {
                    SenderId = (ulong)Context.Player.SteamUserId,
                    InChat = false,
                };
                Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
                Event.EventType = createdEvent.GetType().Name;
                NexusHandler.RaiseEvent(Event);

                if (Core.NexusInstalled)
                {
                    NexusHandler.NexusMessage?.Invoke(Event);
                }
                return;
            }

            if (InGroupChat.ContainsKey(Context.Player.SteamUserId))
            {
                Context.Respond("Leaving group chat", $"{Core.PluginName}");
                Event = new GroupEvent();
                createdEvent = new ToggleGroupChatEvent()
                {
                    SenderId = (ulong)Context.Player.SteamUserId,
                    InChat = false,
                };
                Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
                Event.EventType = createdEvent.GetType().Name;
                NexusHandler.RaiseEvent(Event);

                if (Core.NexusInstalled)
                {
                    NexusHandler.NexusMessage?.Invoke(Event);
                }
                return;
            }
            Context.Respond("Entering group chat", $"{Core.PluginName}");
            Event = new GroupEvent();
            createdEvent = new ToggleGroupChatEvent()
            {
                SenderId = (ulong)Context.Player.SteamUserId,
                InChat = true,
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);

            if (Core.NexusInstalled)
            {
                NexusHandler.NexusMessage?.Invoke(Event);
            }
        }


        [Command("distress", "send a distress signal")]
        [Permission(MyPromoteLevel.None)]
        public void Distress()
        {
            var group = GroupHandler.GetPlayersGroup((long)Context.Player.SteamUserId);
            if (group == null)
            {
                Context.Respond("Group not found.", $"{Core.PluginName}");
                return;
            }
            var Event = new GroupEvent();
            var createdEvent = new GroupDistressEvent()
            {
                Position = Context.Player.Character.PositionComp.GetPosition(),
                SenderId = (ulong)Context.Player.SteamUserId,
                GroupId = group.GroupId,
            };
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            Event.EventType = createdEvent.GetType().Name;
            NexusHandler.RaiseEvent(Event);

            if (Core.NexusInstalled)
            {
                NexusHandler.NexusMessage?.Invoke(Event);
            }
        }
    }
}
