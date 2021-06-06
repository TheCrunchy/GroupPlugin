using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation
{
    public class Hard : MiningContract
    {

        public string subTypeIdTwo;
        public int minimunAmountTwo;
        public int maximumAmountTwo;
        public int pricePerOreTwo;
        public float chanceToObtainSecondOre = 0;

        public bool HasSecondItem = false;
        public bool HasThirdItem = false;
        public string subTypeIdThree;
        public int minimunAmountThree;
        public int maximumAmountThree;
        public int pricePerOreThree;
        public float chanceToObtainThirdOre = 0;
    }
}
