using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.Special_Designation
{
    public class ContractHolder
    {
        public ulong steamId;
        public Guid Id = System.Guid.NewGuid();
        public MiningContract contract;
        public double DeliveryX;
        public double DeliveryY;
        public double DeliveryZ;
        public double AreaX;
        public double AreaY;
        public double AreaZ;

        public int minedAmount;
        public int amountToMine;
        public long price;
        public void GenerateAmountToMine()
        {
            Random rnd = new Random();
            amountToMine = rnd.Next(contract.minimunAmount - 1, contract.maximumAmount + 1);
         
        }

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
