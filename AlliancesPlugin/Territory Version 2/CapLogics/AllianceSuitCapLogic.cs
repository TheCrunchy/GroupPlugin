using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{

    public class AllianceSuitCapLogic : ICapLogic
    {
        public string Test1 = "Test string";
        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Territory territory)
        {
            if (CanLoop())
            {
             //   AlliancePlugin.Log.Info(Test1);
                //do capture logic for suits in alliances

                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            }

            return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public IPointOwner PointOwner { get; set; }
    }

}
