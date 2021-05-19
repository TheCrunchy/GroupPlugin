using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace AlliancesPlugin
{
    public class GridCosts
    {

        //would probably be better to store a component definition ID instead of string, then i could just use one and get both string and item type from it
        private Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();
        public long IdToPay = 0;
        public Dictionary<String, int> componentsString = new Dictionary<String, int>();
        public int BlockCount = 0;
        public void addToComp(MyDefinitionId id, int amount)
        {
           if (components.ContainsKey(id))
            {
                components[id] += amount;
            }
            else
            {
                components.Add(id, amount);
            }
        }
        public void loadFromString()
        {
            foreach (KeyValuePair<String, int> key in componentsString)
            {
                MyDefinitionId.TryParse(key.Key, out MyDefinitionId temp);
                components.Add(temp, key.Value);
            }
        }
        public Int64 credits;
        public double PCU;
        public string gridName;
        public string facID;
        public void setGridName(String name)
        {
            this.gridName = name;
        }
        public String getGridName()
        {
            return this.gridName;
        }

        public void setFacID(String id)
        {
            this.facID = id;
        }
        public String getFacID()
        {
            return this.facID;
        }
        public Dictionary<MyDefinitionId, int> getComponents()
        {
            return this.components;
        }
        public void setComponentsOverride(Dictionary<MyDefinitionId, int> types)
        {
            this.components = types;
        }
        public void setComponents(Dictionary<MyDefinitionId, int> types)
        {
            foreach (KeyValuePair<MyDefinitionId, int> pair in types)
            {
                if (this.components.ContainsKey(pair.Key))
                {
                    this.components.TryGetValue(pair.Key, out int temp);
                    this.components.Remove(pair.Key);
                    this.components.Add(pair.Key, (pair.Value + temp));
                }
                else
                {
                    this.components.Add(pair.Key, pair.Value);
                }
            }
            this.components = types;
        }

        public void setCredits(Int64 credits)
        {
            this.credits += credits;
        }

        public Int64 getCredits()
        {
            return this.credits;
        }

        public void setPCU(double PCU)
        {
            this.PCU += PCU;
        }

        public double getPCU()
        {
            return this.PCU;
        }
    }
}
