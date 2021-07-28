using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Special_Designation
{
    public class MiningContract
    {
        public ulong steamId;

        public int contractPrice = 0;

        public int reputation = 0;

        public string OreSubType;

        public int minedAmount = 0;

        public int amountToMine = 0;
        public void DoPlayerGps(long identityId)
        {

        }
        public void GenerateAmountToMine(int min, int max)
        {
            Random rnd = new Random();
            amountToMine = rnd.Next(min - 1, max + 1);
         
        }

        public Boolean AddToContractAmount(int amount)
        {
            minedAmount += amount;
            if (minedAmount >= amount)
            {
                return true;
            }
            return false;
        }

        public MyGps DeliveryLocation;
       
    }
}
