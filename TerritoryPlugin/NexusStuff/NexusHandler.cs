using System;
using System.Linq;
using CrunchGroup.Handlers;
using CrunchGroup.Models.Events;
using Sandbox.ModAPI;
using Torch.API.Managers;
using VRage.Sync;

namespace CrunchGroup.NexusStuff
{
    public static class NexusHandler
    {
        public static Action<GroupEvent> NexusMessage { get; set; }
        public static Action<GroupEvent, bool> FromServerNexusMessage { get; set; }

        public static void RaiseEvent(GroupEvent groupEvent)
        {
            if (Core.NexusInstalled)
            {
                var message = MyAPIGateway.Utilities.SerializeToBinary<GroupEvent>(groupEvent);
                Core.API.SendMessageToAllServers(message);
            }
            else
            {
                Handle(groupEvent, 0, true);
                // GroupPlugin.Log.Error("Nexus not installed");
            }

        }

        public static void Handle(GroupEvent message, ulong steamId, bool fromServer)
        {
            if (fromServer)
            {
                switch (message.EventType)
                {
                    case "GlobalChatEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GlobalChatEvent>(message.EventObject);
                            Core.SendChatMessage(ev.Author, ev.Message, 0l);
                            break;
                        }
                    case "GroupGPSEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupGPSEvent>(message.EventObject);
                            var group = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Key == ev.GroupId).Value ?? null;
                            if (group == null)
                            {
                                return;
                            }
                            group.SendGroupSignal(ev.Position, ev.Name, ev.Color);
                            break;
                        }
                    case "GroupCreatedEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupCreatedEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupCreated(ev);
                            break;
                        }

                    case "GroupDeletedEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupDeletedEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupDeleted(ev);
                            break;
                        }
                    case "JoinGroupEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<JoinGroupEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupJoin(ev);
                            break;
                        }

                    case "LeftGroupEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<LeftGroupEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupLeave(ev);
                            break;
                        }
                    case "InvitedToGroupEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<InvitedToGroupEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupInvite(ev);
                            break;
                        }
                    case "NameChangedEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupChangedEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupChange(ev);
                            break;
                        }
                    case "GroupChangedEvent":
                        {
                            var ev = MyAPIGateway.Utilities.SerializeFromBinary<GroupChangedEvent>(message.EventObject);
                            GroupEventHandler.HandleGroupChange(ev);
                            break;
                        }
                    default:
                        NexusMessage?.Invoke(message);
                        if (Core.config.DebugMode)
                        {
                            Core.Log.Error($"{message.EventType} not added to the handle switch, sent to scripts if necessary");
                        }
                        break;
                }
            }
            else
            {
                NexusMessage?.Invoke(message);
            }
        }

        public static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            // Core.Log.Info("Recieved a nexus event");
            try
            {
                var message = MyAPIGateway.Utilities.SerializeFromBinary<GroupEvent>(data);

                if (!fromServer && steamID != 0)
                {
                  
                    Core.Log.Error($"Relaying event to all servers from player {steamID}");
                    NexusHandler.RaiseEvent(message);
                    if (Core.NexusInstalled)
                    {
                        Handle(message, steamID, false);
                    }
                
                }
                if (fromServer)
                {
                    Handle(message, steamID, fromServer);
                }
            }
            catch (Exception e)
            {
                Core.Log.Error($"Errored on nexus event {e}");
                throw;
            }
            //Core.Log.Info("Handled a nexus event");
        }
    }
}
