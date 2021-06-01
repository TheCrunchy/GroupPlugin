using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin
{
    [Category("jumpzone")]
    public class ZoneCommands : CommandModule
    {
        [Command("reload", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig()
        {
            AlliancePlugin.LoadAllJumpZones();

            Context.Respond("Reloaded config");
        }
      
    }
}
