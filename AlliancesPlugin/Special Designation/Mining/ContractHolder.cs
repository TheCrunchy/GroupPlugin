using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Special_Designation
{
    public class ContractHolder
    {
        public ulong steamId;
        public MiningContract contract;
        public int DeliveryX;
        public int DeliveryY;
        public int DeliveryZ;
        public int AreaX;
        public int AreaY;
        public int AreaZ;

        public int MiningRadius;
        public Vector3 GetDeliveryLocation()
        {
            return new Vector3(DeliveryX, DeliveryY, DeliveryZ);
        }
        public Vector3 GetMiningArea()
        {
            return new Vector3(AreaX, AreaY, AreaZ);
        }
    }
}
