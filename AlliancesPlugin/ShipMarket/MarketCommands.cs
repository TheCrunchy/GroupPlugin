using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.ShipMarket
{
    [Category("market")]
    public class MarketCommands : CommandModule
    {
        public static MarketList list = new MarketList();
        public static void LoadAllMarketData()
        {

        }
        [Command("view", "view all ships in the market")]
        [Permission(MyPromoteLevel.None)]
        public void ViewMarket()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, MarketItem> items in list.items)
            {
                sb.AppendLine(items.Key + " - " + items.Value.Name);
            }
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        [Command("info", "view information on the target id")]
        [Permission(MyPromoteLevel.None)]
        public void ViewItemInfo(int number)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                return;
            }
            StringBuilder sb = new StringBuilder();
            MarketItem item = list.items[number];
            sb.AppendLine("Seller Name: " + AlliancePlugin.GetPlayerName(item.SellerSteamId));
            sb.AppendLine("Current ID: " + number);
            sb.AppendLine("Name: " + item.Name);
            sb.AppendLine("Price: " + String.Format("{0:n0}", item.Price) + " SC.");
            sb.AppendLine("PCU" + item.PCU);
            sb.AppendLine("Grid Weight:" + item.GridMass);
            sb.AppendLine("Description: " + item.Description);
            sb.AppendLine("");
            sb.AppendLine("Blocks on grid");
            foreach (KeyValuePair<string, Dictionary<string, int>> keys in item.CountsOfBlocks)
            {
                sb.AppendLine(keys.Key);
                foreach (KeyValuePair<string, int> blocks in keys.Value)
                {
                    sb.AppendLine(blocks.Key + " " + blocks.Value);
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Cargo Items");
            foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> keys in item.Cargo)
            {
                sb.AppendLine(keys.Key.ToString().Replace("MyObjectBuilder_", "") + " " + keys.Value);
            }
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        [Command("mine", "output the IDs of items belonging to you.")]
        [Permission(MyPromoteLevel.None)]
        public void OutputMyIds()
        {
            StringBuilder sb = new StringBuilder();

            StringBuilder sb2 = new StringBuilder();
            foreach (KeyValuePair<int, MarketItem> item in list.items)
            {
                if (item.Value.SellerSteamId.Equals(Context.Player.SteamUserId))
                {
                    if (item.Value.Status == ItemStatus.Listed)
                    {
                        sb.AppendLine("Item ID: " + item.Key + " - Item Name " + item.Value.Name + " - Listed Price " + String.Format("{0:n0}", item.Value.Price));
                    }
                    else
                    {
                        sb2.AppendLine(item.Value.Name + " - Sold for " + String.Format("{0:n0}", item.Value.Price) + " SC. Sold at " + item.Value.soldAt.ToString());
                    }
                
                }
            }
            sb.AppendLine("");
            sb.AppendStringBuilder(sb2);
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        [Command("search", "view information on the target id")]
        [Permission(MyPromoteLevel.None)]
        public void SearchMarket(string input)
        {
            Dictionary<int, MarketItem> foundItems = new Dictionary<int, MarketItem>();
            string[] split = Context.RawArgs.Split(',');

            foreach (KeyValuePair<int, MarketItem> item in list.items)
            {
                foreach (String s in split)
                {
                    if (item.Value.Name.Contains(s) || item.Value.Name.ToLower().Contains(s.ToLower()))
                    {
                        if (!foundItems.ContainsKey(item.Key))
                        {
                            foundItems.Add(item.Key, item.Value);
                        }
                    }
                    if (item.Value.Description.Contains(s) || item.Value.Description.ToLower().Contains(s.ToLower()))
                    {
                        if (!foundItems.ContainsKey(item.Key))
                        {
                            foundItems.Add(item.Key, item.Value);
                        }
                    }
                    if (item.Value.GridTags.Contains(s) || item.Value.GetLowerTags().Contains(s.ToLower()))
                    {
                        if (!foundItems.ContainsKey(item.Key))
                        {
                            foundItems.Add(item.Key, item.Value);
                        }
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, MarketItem> item in foundItems)
            {
                sb.AppendLine("Item ID: " + item.Key + " - Item Name " + item.Value.Name);
            }
            if (foundItems.Count == 0)
            {
                ModCommunication.SendMessageTo(new DialogMessage("The Market", "", "No results found"), Context.Player.SteamUserId);
                return;
            }
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
    }
}
