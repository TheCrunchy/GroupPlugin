using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Territory.Handlers;
using Territory.Models.Events;

namespace Territory.NexusStuff
{
    public static class NexusHandler
    {
        public static void RaiseEvent(GroupEvent groupEvent)
        {
            var message = MyAPIGateway.Utilities.SerializeToBinary<GroupEvent>(groupEvent);
            TerritoryPlugin.API.SendMessageToAllServers(message);
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
                        var ev = MyAPIGateway.Utilities.SerializeFromBinary<NameChangedEvent>(message.EventObject);
                        GroupEventHandler.HandleGroupRename(ev);
                        break;
                    }
                default:
                    TerritoryPlugin.Log.Error($"{message.EventType} not added to the handle switch");
                    break;
            }
        }

        public static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            TerritoryPlugin.Log.Info("Recieved a nexus event");
            try
            {
                var message = MyAPIGateway.Utilities.SerializeFromBinary<GroupEvent>(data);
                Handle(message);
            }
            catch (Exception e)
            {
                TerritoryPlugin.Log.Error($"Errored on nexus event {e}");
                throw;
            }
            TerritoryPlugin.Log.Info("Handled a nexus event");
        }
    }
}
