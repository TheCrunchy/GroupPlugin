using AlliancesPlugin.Alliances;
using AlliancesPlugin.NewCaptureSite;
using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

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

        [Command("open", "open the specified koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnlockKoth(string name)
        {
            foreach (CaptureSite site in AlliancePlugin.sites)
            {
                if (site.Name.Equals(name))
                {
                    site.nextCaptureAvailable = DateTime.Now;
                    site.nextCaptureInterval = DateTime.Now;
                    site.CaptureStarted = false;
                    site.CapturingAlliance = Guid.Empty;
                    site.CapturingFaction = 0;
                    site.FactionOwner = 0;
                    // koth.owner = Guid.Empty;
                    Location loc = site.GetCurrentLocation();
                    MyGps gps = new MyGps();
                    gps.Coords = new Vector3D(loc.X, loc.Y, loc.Z);
                    gps.Name = loc.Name;
                    DiscordStuff.SendMessageToDiscord(loc.Name, "Is now unlocked! Ownership reset to nobody. Find it here " + gps.ToString(), site, true);

                    AlliancePlugin.SaveCaptureConfig(site.Name, site);
                    Context.Respond("Unlocked the site");
                }

            }
        }
        [Command("close", "close the specified koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void LockKoth(string name)
        {
            foreach (CaptureSite site in AlliancePlugin.sites)
            {
                if (site.Name.Equals(name))
                {
                    site.nextCaptureAvailable = DateTime.Now.AddYears(5);
                    site.nextCaptureInterval = DateTime.Now.AddYears(5);
                    site.CaptureStarted = false;
                    site.CapturingAlliance = Guid.Empty;
                    site.CapturingFaction = 0;
                    site.FactionOwner = 0;
                    // koth.owner = Guid.Empty;
                    Location loc = site.GetCurrentLocation();
                    DiscordStuff.SendMessageToDiscord(loc.Name, "Is now locked!", site, true);

                    AlliancePlugin.SaveCaptureConfig(site.Name, site);
                    Context.Respond("Locked the site");
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
                    site.FactionOwner = 0;
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
