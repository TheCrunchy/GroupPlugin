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
using VRage.Game.ModAPI;

namespace Territory.Commands
{
    [Category("groupadmin")]
    public class GroupadminCommands : CommandModule
    {
 

        [Command("create", "create a group")]
        [Permission(MyPromoteLevel.Admin)]
        public void Create(string groupName, string description = "Default description")
        {
            var group = new Group()
            {
                GroupName = groupName,
                GroupId = Guid.NewGuid(),
                GroupDescription = description,
            };

            Storage.StorageHandler.Save(group);
            GroupHandler.AddGroup(group);
            var Event = new GroupEvent();
            var createdEvent = new GroupCreatedEvent()
            {
                CreatedGroup = JsonConvert.SerializeObject(group)
            };
            TerritoryPlugin.Log.Error("1");
            Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
            TerritoryPlugin.Log.Error("2");
            Event.EventType = createdEvent.GetType().Name;
            TerritoryPlugin.Log.Error("3");
            NexusHandler.RaiseEvent(Event);
            TerritoryPlugin.Log.Error("4");
            Context.Respond("Group created.", $"{TerritoryPlugin.PluginName}");
        }

    }
}
