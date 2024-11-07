using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using NGPlugin;
using NGPlugin.SyncComponents;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Scripts
{
    public class NexusDirectReference : CommandModule
    {
        [Command("ngtest", "test command")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            var item = SyncBase.GetSync<OnlineServerSync>().GetAllOnlinePlayers();
            Context.Respond($"{item.Count} players online");
        }
    }
}
