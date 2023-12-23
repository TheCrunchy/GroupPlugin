using System;
using CrunchGroup.Handlers;
using CrunchGroup.Models.Events;
using Sandbox.ModAPI;

namespace CrunchGroup.NexusStuff
{
    public static class NexusHandler
    {
        public static void RaiseEvent(GroupEvent groupEvent)
        {
            if (GroupPlugin.NexusInstalled)
            {
                var message = MyAPIGateway.Utilities.SerializeToBinary<GroupEvent>(groupEvent);
                GroupPlugin.API.SendMessageToAllServers(message);
            }
            else
            {
                Handle(groupEvent);
               // GroupPlugin.Log.Error("Nexus not installed");
            }
          
        }

        public static void Handle(GroupEvent message)
        {
            switch (message.EventType)
            {
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
                default:
                    GroupPlugin.Log.Error($"{message.EventType} not added to the handle switch");
                    break;
            }
        }

        public static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
         //   GroupPlugin.Log.Info("Recieved a nexus event");
            try
            {
                var message = MyAPIGateway.Utilities.SerializeFromBinary<GroupEvent>(data);
                Handle(message);
            }
            catch (Exception e)
            {
                GroupPlugin.Log.Error($"Errored on nexus event {e}");
                throw;
            }
           // GroupPlugin.Log.Info("Handled a nexus event");
        }
    }
}
