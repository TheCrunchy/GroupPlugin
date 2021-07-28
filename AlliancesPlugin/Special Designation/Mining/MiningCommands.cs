using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Special_Designation
{
    [Category("miningcontract")]
    public class MiningCommands : CommandModule
    {
        [Command("take", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig(string subtype, int min, int max, int price)
        {
          
        }
    }
}
