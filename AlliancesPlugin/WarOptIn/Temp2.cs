using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;


namespace Blues_Armor_Matrix
{
	public static class Globals
	{
		public static List<List<string>> Armors = new List<List<string>>{
			  //new List<string>{"TypeID", "SubTypeID", 	 			   "Damage From:"Bullet", "Rocket","Explosion","Environment","Tempature","LowPreasure", "MaxHit",	"false"}				
				new List<string>{"Package",                 "Armor",                    "0.80",   "50.0",    "40.0",   "0.5",        "0",         "0",          "200",      "false"},
				new List<string>{"Package",                 "SpartanII",                "0.995",  "15.0",    "8.0",    "0.9998",     "1",         "0.8",        "0",        "false"},
				new List<string>{"Package",                 "SpartanIII",               "0.99",   "20.0",    "12.0",   "0.9975",     "1",         "0.8",        "-28000",   "true"},
				new List<string>{"OxygenContainerObject",   "SpartanIV",                "0.99",   "15.0",    "8.0",    "0.9998",     "1",         "0.5",        "-30000",   "true"},
		};
		public static float MaxOboardReactors = 3f; //It's a fucking character suit you do not even really need one reactor :/
		public static List<List<string>> Reactors = new List<List<string>>{
				new List<string>{"Component",               "FusionReactor",    "Ingot",    "HydrogenPowerCell",    "0.05", "0.5", "100", "false"},
				new List<string>{"Package",                 "SpartanII",        "Ingot",    "HydrogenPowerCell",    "0.05", "0.5", "100", "true"},
				new List<string>{"Package",                 "SpartanIII",       "Ingot",    "HydrogenPowerCell",    "0.05", "0.5", "50",  "true"},
				new List<string>{"OxygenContainerObject",   "SpartanIV",        "Ingot",    "HydrogenPowerCell",    "0.05", "0.5", "50",  "true"},
		};
		public static List<List<string>> Medkits = new List<List<string>>{
			//Comsumable Items  {Type,Subtype,Amount Consumed,HealthGained,Maxhealperitem,Blacklist for Armor }
				new List<string>{"ConsumableItem", "Medkit",    "1",    "30", "100", "false"},
				new List<string>{"Ingot",           "BioFoam",  "1",    "10", "30", "false"},
		};
		//public static readonly MyDefinitionId HydrogenId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");





	}
}