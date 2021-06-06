using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation
{
    public class Medium : MiningContract
    {
        public string subTypeIdTwo;
        public int minimunAmountTwo;
        public int maximumAmountTwo;
        public int pricePerOreTwo;
        public float chanceToObtainSecondOre = 0;
        public bool HasSecondItem = false;
    }
}
