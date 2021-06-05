using AlliancesPlugin.Alliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.KOTH
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
        public void UnlockKoth(string name, string allianceName = "")
        {
            foreach (KothConfig koth in AlliancePlugin.KOTHs)
            {
                if (koth.KothName.Equals(name))
                {
                    koth.nextCaptureAvailable = DateTime.Now;
                    koth.nextCaptureInterval = DateTime.Now;
                  koth.CaptureStarted = true;
                    koth.nextCaptureAvailable = DateTime.Now.AddSeconds(1);
                   // koth.owner = Guid.Empty;
                    if (!allianceName.Equals(""))
                    {
                        Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceName);
                        koth.capturingNation = alliance.AllianceId;
                    }
                    else
                    {
                        koth.capturingNation = Guid.Empty;
                    }
                    Context.Respond("Unlocked the koth");
                }

            }
        }
        [Command("meta", "output all point counts")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputAllPoints()
        {
            foreach (Alliance alliance in AlliancePlugin.AllAlliances.Values)
            {
                Context.Respond(alliance.name + " " + alliance.CurrentMetaPoints);

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
