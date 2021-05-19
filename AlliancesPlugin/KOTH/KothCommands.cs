using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Koth
{
    [Category("koth")]
    public class KothCommands : CommandModule
    {
        [Command("reload", "reload koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnlockKoth()
        {
            AlliancePlugin.LoadConfig();
            Context.Respond("Reloaded");
        }

        [Command("unlock", "unlock koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnlockKoth(string name)
        {
            foreach (KothConfig koth in AlliancePlugin.KOTHs)
            {
                if (koth.KothName.Equals(name))
                {
                    koth.nextCaptureAvailable = DateTime.Now;
                    koth.nextCaptureInterval = DateTime.Now;
                    Context.Respond("Unlocked the koth");
                }

            }
        }
        [Command("output", "unlock koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputAllKothNames()
        {
            foreach (KothConfig koth in AlliancePlugin.KOTHs)
            {
                Context.Respond(koth.KothName);

            }
        }
    }
}
