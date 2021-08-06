using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin
{
    public static class HaulingCore
    {
        public static List<MyGps> DeliveryLocations = new List<MyGps>();
        public static Dictionary<ulong, int> reputation = new Dictionary<ulong, int>();
        private static Dictionary<String, ContractItems> easyItems = new Dictionary<string, ContractItems>();
        public static Dictionary<ulong, HaulingContract> activeContracts = new Dictionary<ulong, HaulingContract>();
        public static List<ulong> Whitelist = new List<ulong>();
        public static StringBuilder MakeContractDetails(List<ContractItems> items)
        {
            int rep = 0;
            StringBuilder contractDetails = new StringBuilder();
            foreach (ContractItems tempitem in items)
            {
                rep += tempitem.reputation;
                contractDetails.AppendLine("Obtain and deliver " + String.Format("{0:n0}", tempitem.AmountToDeliver) + " " + tempitem.SubType + " " + tempitem.ItemType);
            }
            contractDetails.AppendLine("");
            contractDetails.AppendLine("Minimum Payment " + String.Format("{0:n0}", GetMinimumPay(items)) + " SC.");
            contractDetails.AppendLine("");
            contractDetails.AppendLine("To quit contract use !contract quit");
            return contractDetails;
        }
        public static void AddToEasyContractItems(ContractItems item)
        {
            if (easyItems.ContainsKey(item.ContractItemId))
            {
                easyItems.Remove(item.ContractItemId);
                easyItems.Add(item.ContractItemId, item);
            }
            else
            {
                easyItems.Add(item.ContractItemId, item);
            }
        }
        public static ContractItems ReadContractItem(String[] split, string difficulty)
        {
            foreach (String s in split)
            {
                s.Replace(" ", "");
            }
            ContractItems temp = new ContractItems();
            temp.ContractItemId = split[0].Replace(" ", "");
            temp.ItemType = split[1].Replace(" ", "");
            temp.SubType = split[2].Replace(" ", "");
            temp.MinToDeliver = int.Parse(split[3].Replace(" ", ""));
            temp.MaxToDeliver = int.Parse(split[4].Replace(" ", ""));
            temp.MinPrice = int.Parse(split[5].Replace(" ", ""));
            temp.MaxPrice = int.Parse(split[6].Replace(" ", ""));
            temp.chance = int.Parse(split[7].Replace(" ", ""));
            temp.difficulty = difficulty;
            return temp;
        }

        public static int GetMinimumPay(List<ContractItems> items)
        {
            int pay = 0;
            foreach (ContractItems tempitem in items)
            {

                pay += tempitem.AmountToDeliver * tempitem.MinPrice;
            }
            return pay;
        }
        public static void SendMessage(string author, string message, Color color, ulong steamID)
        {


           // Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId(steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
        public static MyGps getDeliveryLocation()
        {

            Random random = new Random();
            if (DeliveryLocations.Count == 1 || DeliveryLocations.Count == 0)
             {
                return DeliveryLocations[0];
           }
             int r = random.Next(DeliveryLocations.Count);
              return DeliveryLocations[r];

        }
        private static List<ContractItems> getItems(List<ContractItems> items, int AmountToPick)
        {
            List<ContractItems> returnList = new List<ContractItems>();
            List<ContractItems> oof = new List<ContractItems>();
            List<ContractItems> SortedList = items.OrderByDescending(o => o.chance).ToList();
            SortedList.Reverse();
            int amountPicked = 0;

            //sort the list by descending then reverse it so we check the lowest chances first
            Random random = new Random();
            foreach (ContractItems item in SortedList)
            {
                item.SetAmountToDeliver();
                int chance = random.Next(101);
                if (chance <= item.chance)
                {
                    returnList.Add(item);
                }
            }
            //check theres at least one item on the contract, if not pick one at complete random
            if (returnList.Count == 0)
            {
                return null;
            }
            if (returnList.Count == 1)
            {
              oof.Add(returnList.ElementAt(0));
            }
            else
            {
                int index = random.Next(returnList.Count - 1);
                ContractItems temp = returnList.ElementAt(index);
                oof.Add(temp);
            }
            return oof;
        }

        //i really hate this code, i should make it one method
        //wrote this at like 4am
        public static List<ContractItems> getRandomContractItem(int amount)
        {
            Random random = new Random();
            List<ContractItems> list = new List<ContractItems>();
            List<ContractItems> temp = new List<ContractItems>();
            foreach (ContractItems item in easyItems.Values)
            {
                temp.Add(item);
            }
            int chance = random.Next(101);
            list = getItems(temp, amount);
            return list;

        }
        public static Boolean GenerateContract(ulong steamid, long identityid)
        {
            //if (config.UsingWhitelist)
            //{
            //    if (!Whitelist.Contains(steamid))
            //    {
            //        DialogMessage m = new DialogMessage("Contract Failure", "Fail", config.WhitelistMessage);
            //        ModCommunication.SendMessageTo(m, steamid);
            //        return false;
            //    }
            //}
            FileUtils utils = new FileUtils();
            if (getActiveContract(steamid) != null)
            {
                SendMessage("The Boss", "You already have a contract!", Color.Red, steamid);
                DialogMessage m = new DialogMessage("Contract fail", "", "You already have a contract, to quit use !contract quit");
                ModCommunication.SendMessageTo(m, steamid);
                return false;
            }
            else
            {
                //this code is awful and i want to redo it, probably throwing the generation in a new method and changing this reputation check to just change the amount

                    List<ContractItems> items = getRandomContractItem(1);
                    MyGps gps = getDeliveryLocation();
                    HaulingContract contract = new HaulingContract();
                  
                    contract.items = items;
                    contract.GpsX = gps.Coords.X;
                    contract.GpsY = gps.Coords.Y;
                    contract.GpsZ = gps.Coords.Z;

                   // Database.addNewContract(steamid, contract);
                    StringBuilder contractDetails = new StringBuilder();
                    contractDetails = MakeContractDetails(contract.getItemsInContract());

                activeContracts.Add(steamid, contract);
                utils.WriteToJsonFile<HaulingContract>(AlliancePlugin.path + "//HaulingStuff//PlayerData//" + steamid + ".json", contract);
                    gps = contract.GetDeliveryLocation();
                    MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                    
                    gpscol.SendAddGps(identityid, ref gps);
                    // MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gps);
                    DialogMessage m = new DialogMessage("Contract Details", "Obtain and deliver these items", contractDetails.ToString());
                    ModCommunication.SendMessageTo(m, steamid);
                
                return true;
            }
        }
        public static HaulingContract getActiveContract(ulong steamid)
        {

            if (activeContracts.TryGetValue(steamid, out HaulingContract contract))
            {
                return contract;
            }

            return null;
        }
        public static void RemoveContract(ulong steamid, long identityid)
        {
            activeContracts.Remove(steamid);
            List<IMyGps> playerList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(identityid, playerList);
            foreach (IMyGps gps in playerList)
            {
                if (gps.Name.Contains("Delivery Location, bring hauling vehicle within 300m"))
                {
                    MyAPIGateway.Session?.GPS.RemoveGps(identityid, gps);
                }
            }

        }
    }
}
