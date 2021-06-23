using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Shipyard
{
    public class UpgradeCost
    {
        public Dictionary<MyDefinitionId, int> itemsRequired = new Dictionary<MyDefinitionId, int>();
        public int id;
        public string type;
        public long MoneyRequired = 0;
        public double NewLevel = 1;
        public int MetaPointCost = 0;
    }
}
