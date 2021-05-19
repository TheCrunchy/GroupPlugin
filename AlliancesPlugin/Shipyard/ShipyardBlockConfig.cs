using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class ShipyardBlockConfig
    {
        public string SubtypeId;
        public float SecondsPerBlock = 1f;
        public int SCPerBlock = 5000;
        public string FuelTypeId = "MyObjectBuilder_Ingot";
        public string FuelSubTypeId = "MyObjectBuilder_Uranium";
        public float SecondsPerInterval = 10;
        public int FuelPerInterval = 1;
    }
}
