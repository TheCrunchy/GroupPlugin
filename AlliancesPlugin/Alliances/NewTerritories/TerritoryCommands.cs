using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    [Category("territory")]
    public class TerritoryCommands : CommandModule
    {
        [Command("unlock", "unlock a territory for capture")]
        [Permission(MyPromoteLevel.Admin)]
        public void Unlock(string territoryName, int hours = 72)
        {
            var territory = AlliancePlugin.Territories.First(x => x.Value.Name == territoryName);
            if (territory.Value == null)
            {
                Context.Respond("Territory Not found.");
                return;
            }

            AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + territory.Value.Name + ".xml", territory.Value);
        }

        [Command("give", "give territory to alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void Unlock(string territoryName, string allianceName)
        {
            var territory = AlliancePlugin.Territories.First(x => x.Value.Name == territoryName);
            if (territory.Value == null)
            {
                Context.Respond("Territory Not found.");
                return;
            }

            var alliance = AlliancePlugin.GetAlliance(allianceName);
            if (alliance == null)
            {
                Context.Respond("Alliance not found.");
                return;
            }

            AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + territory.Value.Name + ".xml", territory.Value);
        }
    }
}
