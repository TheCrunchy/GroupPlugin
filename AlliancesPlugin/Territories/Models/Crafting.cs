using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Territory_Version_2.Models
{
    public class RecipeItem
    {
        public string typeid;
        public string subtypeid;
        public int amount;
    }

    public class CraftedItem
    {
        public bool Enabled = true;
        public string typeid;
        public string subtypeid;
        public double chanceToCraft = 0.5;
        public int amountPerCraft;
        public List<RecipeItem> RequriedItems = new List<RecipeItem>();
    }

    public class UpkeepItem
    {
        public string typeid;
        public string subtypeid;
        public int amount;
    }
}
