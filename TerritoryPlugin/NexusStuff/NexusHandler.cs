using System;
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

        public static void RaiseEvent(GroupEvent groupEvent)
        {
            if (Core.NexusInstalled)
            {
                var message = MyAPIGateway.Utilities.SerializeToBinary<GroupEvent>(groupEvent);
                Core.API.SendMessageToAllServers(message);
            }
            else
            {
                Handle(groupEvent, 0,true);
               // GroupPlugin.Log.Error("Nexus not installed");
            }
          
        }

        public static void Handle(GroupEvent message,ulong steamId, bool fromServer)
        {
            if (!fromServer && steamId != 0)
            {
                NexusMessage?.Invoke(message);
                return;
            }
            switch (message.EventType)
            {
                case "GlobalChatEvent":
                {
                    var ev = MyAPIGateway.Utilities.SerializeFromBinary<GlobalChatEvent>(message.EventObject);
                    Core.SendChatMessage(ev.Author, ev.Message, 0l);
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

        public static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
           // Core.Log.Info("Recieved a nexus event");
            try
            {
                var message = MyAPIGateway.Utilities.SerializeFromBinary<GroupEvent>(data);
                Handle(message, steamID,fromServer);
                if (!fromServer && steamID != 0 && Core.NexusInstalled)
                {
                    Core.Log.Error($"Relaying event to all servers from player");
                    NexusHandler.RaiseEvent(message);
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
