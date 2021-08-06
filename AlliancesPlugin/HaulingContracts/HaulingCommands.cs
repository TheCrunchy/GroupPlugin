using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
namespace AlliancesPlugin.HaulingContracts
{
    [Category("contract")]
    public class HaulingCommands : CommandModule
    {
        //stackoverflow 
        public static string RemoveWhitespace(string input)
        {
            return string.Concat(input.Where(c => !char.IsWhiteSpace(c)));
        }

        [Command("whitelist add", "add to contract whitelist")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddWhitelist(string input)
        {
            //if (input.Contains(","))
            //{
            //    //input = input.Replace(" ", "");

            //    String input2 = RemoveWhitespace(input);
            //    String[] ids = input2.Split(',');

            //    if (Database.AddMultipleToWhitelist(ids))
            //    {
            //        Context.Respond("Added them");
            //    }
            //    else
            //    {
            //        Context.Respond("Error adding them");
            //    }

            //}
            //else
            //{

            Context.Respond("Error adding them, remember no spaces!");

            //   }
        }

        [Command("whitelist remove", "add to contract whitelist")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveWhitelist(string input)
        {
            //if (input.Contains(","))
            //{
            //   String input2 = RemoveWhitespace(input);
            //    String[] ids = input2.Split(',');
            //    if (Database.RemoveMultipleFromWhitelist(ids))
            //    {
            //        Context.Respond("Removed them");
            //    }
            //    else
            //    {
            //        Context.Respond("Error removing them");
            //    }
            //}
            //else
            //{

            Context.Respond("Error removing them, remember no spaces!");

            //}
        }
        [Command("whitelist output", "output")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputWHitelist()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ulong id in HaulingCore.Whitelist)
            {
                sb.AppendLine(id.ToString());

            }
            Context.Respond(sb.ToString());
        }

        [Command("truck", "output definitions")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputDefinitions()
        {
            StringBuilder ingots = new StringBuilder();
            StringBuilder components = new StringBuilder();
            StringBuilder ore = new StringBuilder();
            StringBuilder ammo = new StringBuilder();

            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ingot"))
                {

                    ingots.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Component"))
                {

                    components.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ore"))
                {
                    ore.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_AmmoMagazine"))
                {
                    ammo.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }

            }
            StringBuilder output = new StringBuilder();
            output.AppendLine("TypeId, SubtypeId, minAmount, maxAmount, minPrice, maxPrice");
            output.AppendLine(ingots.ToString());
            output.AppendLine(components.ToString());
            output.AppendLine(ore.ToString());
            output.AppendLine(ammo.ToString());



            if (!System.IO.File.Exists(AlliancePlugin.path + "//HaulingStuff"))
            {
                System.IO.Directory.CreateDirectory(AlliancePlugin.path + "//HaulingStuff//");
            }
            File.WriteAllText(AlliancePlugin.path + "//HaulingStuff//definitions.csv", output.ToString());
        }

        public Boolean getChance(int minimalChance)
        {
            Random random = new Random();
            return random.Next(99) + 1 <= minimalChance;
        }

        [Command("quit", "quit a contract")]
        [Permission(MyPromoteLevel.None)]
        public void QuitContract()
        {
            if (HaulingCore.getActiveContract(Context.Player.SteamUserId) != null)
            {


                File.Delete(AlliancePlugin.path + "//HaulingStuff//PlayerData//" + Context.Player.SteamUserId + ".json");
                HaulingCore.SendMessage("The Boss", "Contract quit", Color.Yellow, Context.Player.SteamUserId);
                List<IMyGps> playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Context.Player.IdentityId, playerList);
                HaulingCore.activeContracts.Remove(Context.Player.SteamUserId);
                foreach (IMyGps gps in playerList)
                {
                    if (gps.Name.Contains("Delivery Location, bring hauling vehicle within 300m"))
                    {
                        MyAPIGateway.Session?.GPS.RemoveGps(Context.Player.Identity.IdentityId, gps);
                    }
                }
            }
            else
            {
                HaulingCore.SendMessage("The Boss", "You dont currently have a contract.", Color.Yellow, Context.Player.SteamUserId);

            }
        }
        [Command("setupDatabase", "show contract details")]
        [Permission(MyPromoteLevel.None)]
        public void SetupDatabase()
        {

        }
        [Command("info", "show contract details")]
        [Permission(MyPromoteLevel.None)]
        public void ContractDetails()
        {

            StringBuilder contractDetails = new StringBuilder();
            if (HaulingCore.getActiveContract(Context.Player.SteamUserId) != null)
            {
                HaulingContract contract = HaulingCore.getActiveContract(Context.Player.SteamUserId);
                int pay = HaulingCore.GetMinimumPay(contract.getItemsInContract());
                contractDetails = HaulingCore.MakeContractDetails(contract.getItemsInContract());
                MyGps gps = HaulingCore.getDeliveryLocation();

                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                gpscol.SendAddGps(Context.Player.Identity.IdentityId, ref gps);
                DialogMessage m = new DialogMessage("Contract Details", "Obtain and deliver these items", contractDetails.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You dont currently have a contract", "The Boss");
            }
        }

        [Command("take", "take a contract")]
        [Permission(MyPromoteLevel.Admin)]
        public void TakeContract()
        {

            if (HaulingCore.getActiveContract(Context.Player.SteamUserId) != null)
            {
                Context.Respond("You cannot take another contract while you have an active one. To quit a contract use !contract quit", "The Boss");
            }
            else
            {
                HaulingCore.GenerateContract(Context.Player.SteamUserId, Context.Player.IdentityId);

            }

        }
    }
}




