using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{
    public class AllianceGridCapLogic : ICapLogic
    {
        public AllianceGridCapLogic(int Test2)
        {
            this.Test2 = Test2;
        }

        public int Test2 = 5;
        public Task<Tuple<bool, object>> ProcessCap()
        {
            if (CanLoop())
            {
                AlliancePlugin.Log.Info(Test2);
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                //do capture logic for suits in alliances
            }

            return Task.FromResult(Tuple.Create<bool, Object>(false, null));
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public IPointOwner PointOwner { get; set; }
    }
}
