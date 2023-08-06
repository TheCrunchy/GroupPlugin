//systems
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
//Sandboxs
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.World;
//using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Components;
//using Sandbox.Game.MultiPlayer;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Sandbox.Definitions;
//Vrage
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.Utils;
using VRage.Game.ModAPI.Network;
using VRage.Network;
using VRage.Sync;
using VRageMath;
using VRage;
//Using Global Variables why Using? Idk Blame keen
//using Blues_Armor_Matrix;
namespace Blues_Armor_Matrix
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Character), false)]
	public class Characters : MyGameLogicComponent
	{
		public static Characters Instance;
		private MyObjectBuilder_EntityBase SuitBuilder;
		private IMyCharacter PlayerSuit;
		public bool IamWearingArmor = false;
		public string ArmorIAmWearing = null;
		//Initializer
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			//Sets Instace
			Instance = this;
			//Gets the Character
			PlayerSuit = Entity as IMyCharacter;
			//Gets the Mycharacter_builder
			SuitBuilder = objectBuilder;
			//Update Every 100th Frame
			NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
			//Globals.Armors.

		}
		//Update Every Time
		public override void UpdateAfterSimulation100()
		{
			base.UpdateAfterSimulation100();
			if (PlayerSuit.IsDead) { return; }//We will see
			MyInventory SuitInventory = PlayerSuit.GetInventory() as MyInventory;//Get Character Inventory
			IMyPlayer Player = MyAPIGateway.Players.GetPlayerControllingEntity(PlayerSuit);//Get Player Controlling Character
			PowerReactors(SuitInventory, Player);
			ApplyMedPacks(SuitInventory, Player);
			ApplyRestictions(SuitInventory, Player);
		}
		private void PowerReactors(MyInventory SuitInventory, IMyPlayer Player)
		{
			float OnboardReactors = 0f;
			foreach (List<string> Reactor in Globals.Reactors)
			{
				var ReactorItem = Sandbox.Game.MyVisualScriptLogicProvider.GetDefinitionId(Reactor[0], Reactor[1]);
				OnboardReactors += Convert.ToSingle(SuitInventory.GetItemAmount(ReactorItem).ToString());
			}
			if (Globals.MaxOboardReactors >= OnboardReactors && OnboardReactors > 0)
			{
				foreach (List<string> Reactor in Globals.Reactors)
				{
					var ReactorItem = Sandbox.Game.MyVisualScriptLogicProvider.GetDefinitionId(Reactor[0], Reactor[1]);
					var FuelItem = Sandbox.Game.MyVisualScriptLogicProvider.GetDefinitionId(Reactor[2], Reactor[3]);
					//var test = VRage.Game.ModAPI.Ingame.IMyInventoryItem.GetItemId("Package");
					float AvalibleFuel = Convert.ToSingle(SuitInventory.GetItemAmount(FuelItem).ToString());
					if (AvalibleFuel >= Convert.ToSingle(Reactor[4]))
					{
						float Charge = Convert.ToSingle(PlayerSuit.SuitEnergyLevel + (Convert.ToSingle(Reactor[5]) / 100f));
						if (Charge <= (Convert.ToSingle(Reactor[6]) / 100f) && (Convert.ToSingle(Reactor[6]) / 100f) <= 1f)
						{
							MyVisualScriptLogicProvider.SetPlayersEnergyLevel(Player.IdentityId, Charge);
							//MyVisualScriptLogicProvider.RemoveFromPlayersInventory(Player.IdentityId, FuelItem, (MyFixedPoint)0.1f);
							SuitInventory.RemoveItemsOfType((MyFixedPoint)Convert.ToSingle(Reactor[4]), FuelItem, MyItemFlags.None, false);
							// VRage.MyFixedPoint.operator float
						}
					}
					else
					{
						float PlayerHydrogen = MyVisualScriptLogicProvider.GetPlayersHydrogenLevel(Player.IdentityId);
						float Charge = Convert.ToSingle(PlayerSuit.SuitEnergyLevel + (Convert.ToSingle(Reactor[5]) / 100f));
						if (PlayerHydrogen > 0 && PlayerHydrogen < 1 && (PlayerHydrogen + Charge < 1))
						{
							MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(Player.IdentityId, PlayerHydrogen - 0.01f);
							MyVisualScriptLogicProvider.SetPlayersEnergyLevel(Player.IdentityId, (Charge / 2));
						}
					}
				}
			}
		}
		private void ApplyMedPacks(MyInventory SuitInventory, IMyPlayer Player)
		{

		}
		private void ApplyRestictions(MyInventory SuitInventory, IMyPlayer Player)
		{
			int NumberOfArmors = 0;//Count of how many Armors a Player has in inventory
			foreach (List<string> Armor in Globals.Armors)
			{
				var ArmorItem = Sandbox.Game.MyVisualScriptLogicProvider.GetDefinitionId(Armor[0], Armor[1]);
				var item = SuitInventory.FindItem(ArmorItem);
				if (item.HasValue)
				{
					IamWearingArmor = true;
					NumberOfArmors += 1;
				}
			}
			if (NumberOfArmors == 1)
			{
				if (PlayerSuit.EnabledThrusts) { MyVisualScriptLogicProvider.SendChatMessage(Player.DisplayName.ToString() + " You may not use JetPackThrusters while wearing armor!", "Blue's Armor Matrix", Player.IdentityId, "Green"); PlayerSuit.SwitchThrusts(); }
				if (PlayerSuit.EquippedTool is IMyAngleGrinder) { var controlEnt = (PlayerSuit) as Sandbox.Game.Entities.IMyControllableEntity; controlEnt?.SwitchToWeapon(null); MyVisualScriptLogicProvider.SendChatMessage(Player.DisplayName.ToString() + " You may not use handgrinders while wearing armor!", "Blue's Armor Matrix", Player.IdentityId, "Green"); }
			}
		}
	}
}