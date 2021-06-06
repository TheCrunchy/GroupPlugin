using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation
{
    public class MiningContract
    {
        public Guid contractId = System.Guid.NewGuid();
        public string subTypeId;
        public int minimunAmount;
        public int maximumAmount;
        public int pricePerOre;
        public int reputationGain;
    }
}
