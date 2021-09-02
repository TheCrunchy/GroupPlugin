using AlliancesPlugin.Alliances;
using AlliancesPlugin.NewCaptureSite;
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
            AlliancePlugin.LoadAllCaptureSites();
            Context.Respond("Reloaded");
        }
        [Command("pause", "pause koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void PauseKoth()
        {
            AlliancePlugin.Paused = true;
            Context.Respond("Paused");
        }
        [Command("start", "start koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void StartKoth()
        {
            AlliancePlugin.Paused = false;
            Context.Respond("Starting");
        }
        [Command("toggle", "enable or disable a koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void ToggleKoth(string name)
        {
            foreach (KothConfig koth in AlliancePlugin.KOTHs)
            {
                if (koth.KothName.Equals(name))
                {
                    koth.enabled = !koth.enabled;
                }

            }
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
                    koth.capturingNation = Guid.Empty;
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
            foreach (CaptureSite site in AlliancePlugin.sites)
            {
                if (site.Name.Equals(name))
                {
                    site.nextCaptureAvailable = DateTime.Now;
                    site.nextCaptureInterval = DateTime.Now;
                    site.CaptureStarted = false;
                    site.nextCaptureAvailable = DateTime.Now.AddSeconds(1);
                    site.CapturingAlliance = Guid.Empty;
                    site.CapturingFaction = 0;
                    // koth.owner = Guid.Empty;
                    if (!allianceName.Equals(""))
                    {
                        Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceName);
                        site.CapturingAlliance = alliance.AllianceId;
                    }
                    else
                    {
                        site.CapturingAlliance = Guid.Empty;
                    }
                    AlliancePlugin.SaveCaptureConfig(site.Name, site);
                    Context.Respond("Unlocked the site");
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
