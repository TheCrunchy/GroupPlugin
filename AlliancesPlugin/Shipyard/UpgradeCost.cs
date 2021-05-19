using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin
{
    public class UpgradeCost
    {
        public Dictionary<MyDefinitionId, int> itemsRequired = new Dictionary<MyDefinitionId, int>();
        public long MoneyRequired = 0;
    }
}
