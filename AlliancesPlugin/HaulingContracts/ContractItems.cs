using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class ContractItems
    {
            public string ContractItemId;
            public string ItemType;
            public string SubType;
            public int MinToDeliver;
            public int MaxToDeliver;
            public int MinPrice;
            public int MaxPrice;
            public int AmountToDeliver;
            public int chance;
            public int reputation;
            public string difficulty;
            public void SetAmountToDeliver()
            {
                Random random = new Random();
                AmountToDeliver = random.Next(MinToDeliver, MaxToDeliver + 1);
            }
            public void SetAmountToDeliver(int amount)
            {
                AmountToDeliver = amount;
            }
            public int GetAmountToDeliver()
            {
                return AmountToDeliver;
            }
            public int GetPrice()
            {
                Random random = new Random();
                return random.Next(MinPrice, MaxPrice + 1);

            }
        
    }
}
