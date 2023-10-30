using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.NewTerritoryCapture.Models
{
    public interface ICapLogic
    {
        //return if the cap has succeeded, and if so an object of who now owns it
        Tuple<bool, Object> ProcessCap();
    }

    public class AllianceSuitCapLogic : ICapLogic
    {
        public string Test1 = "Test string";
        public Tuple<bool, object> ProcessCap()
        {
            AlliancePlugin.Log.Info(Test1);
            //do capture logic for suits in alliances

            return Tuple.Create<bool,Object>(false, null);
        }
    }

    public class AllianceGridCapLogic : ICapLogic
    {
        public int Test2 = 5;
        public Tuple<bool, object> ProcessCap()
        {
            AlliancePlugin.Log.Info(Test2);
            //do capture logic for grids in alliances
            return Tuple.Create<bool, Object>(false, null);
        }
    }
}
