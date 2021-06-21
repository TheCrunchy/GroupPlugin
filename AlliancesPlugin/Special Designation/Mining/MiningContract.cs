using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation
{
    public class MiningContract
    {
        public Boolean enabled = true;
        public string subTypeId;
        public int minimunAmount;
        public int maximumAmount;
        public int reputationGain;
        public int minimumRepToTake = 0;
        public int maximumRepToTake = 0;
        public string GpsToDeliverTo;
        public long price;
    }
}
