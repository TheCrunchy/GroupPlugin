using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class LootLogic : ISecondaryLogic
    {
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public bool RequireOwner = true;
        public Task DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                if (point.PointOwner == null)
                {
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

    }
}
