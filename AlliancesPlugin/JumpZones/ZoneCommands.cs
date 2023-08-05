using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.JumpZones
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
