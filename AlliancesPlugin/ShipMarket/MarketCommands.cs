using AlliancesPlugin.Hangar;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
using VRage.Groups;
using VRageMath;

namespace AlliancesPlugin.ShipMarket
{
    [Category("market")]
    public class MarketCommands : CommandModule
    {
        public static MarketList list = new MarketList();
        [Command("view", "view all ships in the market")]
        [Permission(MyPromoteLevel.Admin)]
        public void ViewMarket()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("To view information on a listing use !market info number");
            sb.AppendLine("To search the market use !market search input.");
            sb.AppendLine("To purchase a grid from the market use !market buy number");
            sb.AppendLine("");
            foreach (KeyValuePair<int, MarketItem> items in list.items)
            {
                sb.AppendLine("[" + items.Key + "] " + items.Value.Name + ", Seller " + AlliancePlugin.GetPlayerName(items.Value.SellerSteamId));
            }
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        [Command("info", "view information on the target id")]
        [Permission(MyPromoteLevel.Admin)]
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
            sb.AppendLine("Grid Weight:" + String.Format("{0:n0}", item.GridMass));
            sb.AppendLine("Description: " + item.Description);
            sb.AppendLine("");
            sb.AppendLine("Blocks on grid");
            foreach (KeyValuePair<string, Dictionary<string, int>> keys in item.CountsOfBlocks)
            {
                sb.AppendLine(keys.Key.Replace("MyObjectBuilder_", ""));
                foreach (KeyValuePair<string, int> blocks in keys.Value)
                {
                    sb.AppendLine(blocks.Key.Replace("MyObjectBuilder_","") + " " + blocks.Value);
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Cargo Items");
            foreach (KeyValuePair<string, MyFixedPoint> keys in item.Cargo)
            {
                sb.AppendLine(keys.Key.ToString().Replace("MyObjectBuilder_", "") + " " + keys.Value);
            }
            DialogMessage m = new DialogMessage("The Market", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }
        FileUtils utils = new FileUtils();
        public static Dictionary<long, DateTime> confirmations = new Dictionary<long, DateTime>();

        [Command("add tag", "add a tag to the item listing")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddTag(int number, string tag)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                return;
            }
            MarketItem item = list.items[number];
            if (!item.SellerSteamId.Equals(Context.Player.SteamUserId))
            {
                Context.Respond("This listing doesnt belong to you.");
                return;
            }
            string[] split = Context.RawArgs.Split(' ');
            int count = 0;
            foreach (String s in split)
            {
                if (count <= 1)
                {
                    count++;
                    continue;
                }
                item.AddTag(s);
            }
            utils.WriteToJsonFile<MarketItem>(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json", item);
        }

        [Command("sell", "sell a grid on the market")]
        [Permission(MyPromoteLevel.Admin)]
        public void Sell(string price, string name)
        {
            if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
            {
                Context.Respond("You cannot use this command in natural gravity!");
                confirmations.Remove(Context.Player.IdentityId);
                return;
            }

            foreach (DeniedLocation denied in AlliancePlugin.HangarDeniedLocations)
            {
                if (Vector3.Distance(Context.Player.GetPosition(), new Vector3(denied.x, denied.y, denied.z)) <= denied.radius)
                {
                    Context.Respond("Cannot sell here, too close to a denied location.");
                    confirmations.Remove(Context.Player.IdentityId);
                    return;
                }
            }
            Int64 amount;
            price = price.Replace(",", "");
            price = price.Replace(".", "");
            price = price.Replace(" ", "");
            try
            {
                amount = Int64.Parse(price);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroupMechanical(Context.Player.Character);

            List<MyCubeGrid> grids = new List<MyCubeGrid>();


            foreach (var item1 in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in item1.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;
                   
                    if (FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, false))
                    {
                        if (!grids.Contains(grid))
                        {
                         
                        
                            foreach (MySurvivalKit block in grid.GetFatBlocks().OfType<MySurvivalKit>())
                            {
                                block.CustomData = "Custom Data was cleared.";
                            }
                            foreach (MyMedicalRoom block in grid.GetFatBlocks().OfType<MyMedicalRoom>())
                            {
                                block.CustomData = "Custom Data was cleared.";
                            }
                            List<MyProgrammableBlock> removeThese = new List<MyProgrammableBlock>();
                            foreach (MyProgrammableBlock block in grid.GetFatBlocks().OfType<MyProgrammableBlock>())
                            {
                                removeThese.Add(block);
                            }
                            foreach (MyProgrammableBlock block in removeThese)
                            {
                                grid.RemoveBlock(block.SlimBlock);
                            }
                            grids.Add(grid);
                        }
                    }

                }
            }
            if (grids.Count == 0)
            {
                Context.Respond("Could not find any grids you own. Are you looking directly at it?");
                return;
            }
            MarketItem item = new MarketItem();
            item.Setup(grids, name, amount, Context.Player.SteamUserId);
            if (list.AddItem(item))
            {
                if (GridManager.SaveGridNoDelete(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml", item.ItemId.ToString(), false, false, grids))
                {
                    Context.Respond("Added the item to the market!");

                    utils.WriteToJsonFile<MarketItem>(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json", item);
                    foreach (MyCubeGrid grid in grids)
                    {
                        if (grid != null)
                        {
                            grid.Close();
                        }
                    }
                }
            }
            else
            {
                Context.Respond("Failed to add the grid to the market. Try again.");
            }
        }

        [Command("remove tag", "add a tag to the item listing")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveTag(int number, string tag)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                return;
            }
            MarketItem item = list.items[number];
            if (!item.SellerSteamId.Equals(Context.Player.SteamUserId))
            {
                Context.Respond("This listing doesnt belong to you.");
                return;
            }
            string[] split = Context.RawArgs.Split(' ');
            int count = 0;
            foreach (String s in split)
            {
                if (count <= 1)
                {
                    count++;
                    continue;
                }
                item.RemoveTag(s);
            }
            Context.Respond("Tags removed, you can seperate them with a space.");
            utils.WriteToJsonFile<MarketItem>(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json", item);
        }
        [Command("description", "change the items description")]
        [Permission(MyPromoteLevel.Admin)]
        public void ChangeDescription(int number, string description)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                return;
            }
            MarketItem item = list.items[number];
            if (!item.SellerSteamId.Equals(Context.Player.SteamUserId))
            {
                Context.Respond("This listing doesnt belong to you.");
                return;
            }
            item.Description = description;
            Context.Respond("Description updated.");
            utils.WriteToJsonFile<MarketItem>(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json", item);
        }
        [Command("end", "end a listing and get the grid back")]
        [Permission(MyPromoteLevel.Admin)]
        public void EndListing(int number)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                return;
            }
            MarketItem item = list.items[number];
            if (!item.SellerSteamId.Equals(Context.Player.SteamUserId))
            {
                Context.Respond("This listing doesnt belong to you.");
                return;
            }
            if (!File.Exists(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json"))
            {
                Context.Respond("That grid is no longer available for sale.");
                return;
            }
            if (!File.Exists(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml"))
            {
                Context.Respond("This grid should be available, but its file for the grid doesnt exist.");
                return;
            }
            if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
            {
                Context.Respond("You cannot use this command in natural gravity!");
                return;
            }

            foreach (DeniedLocation denied in AlliancePlugin.HangarDeniedLocations)
            {
                if (Vector3.Distance(Context.Player.GetPosition(), new Vector3(denied.x, denied.y, denied.z)) <= denied.radius)
                {
                    Context.Respond("Cannot buy here, too close to a denied location.");
                    return;
                }
            }

            if (GridManager.LoadGrid(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml", Context.Player.GetPosition(), true, Context.Player.SteamUserId, item.Name))
            {
                EconUtils.takeMoney(Context.Player.IdentityId, item.Price);
                long sellerId = MySession.Static.Players.TryGetIdentityId(item.SellerSteamId);
                EconUtils.addMoney(sellerId, item.Price);
                if (AlliancePlugin.GridBackupInstalled)
                {
                    AlliancePlugin.BackupGridMethod(GridManager.GetObjectBuilders(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml"), Context.Player.IdentityId);
                }
                item.Buyer = Context.Player.SteamUserId;
                item.soldAt = DateTime.Now;
                if (!Directory.Exists(AlliancePlugin.path + "//ShipMarket//Sold//" + item.SellerSteamId))
                {
                    Directory.CreateDirectory(AlliancePlugin.path + "//ShipMarket//Sold//" + item.SellerSteamId);
                }
                list.items.Remove(number);
                File.Delete(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json");
                File.Delete(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml");
                Context.Respond("Ended the listing, the grid should appear near you.");
            }
            else
            {
                Context.Respond("Failed to load the grid! transaction cancelled.");
                return;
            }
        }

        [Command("buy", "buy the target grid")]
        [Permission(MyPromoteLevel.Admin)]
        public void BuyGrid(int number)
        {
            if (!list.items.ContainsKey(number))
            {
                Context.Respond("There is no item in the market for that slot number.");
                confirmations.Remove(Context.Player.IdentityId);
                return;
            }
            MarketItem item = list.items[number];
            if (!File.Exists(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json"))
            {
                Context.Respond("That grid is no longer available for sale.");
                confirmations.Remove(Context.Player.IdentityId);
                return;
            }
            if (!File.Exists(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml"))
            {
                Context.Respond("This grid should be available, but its file for the grid doesnt exist.");
                confirmations.Remove(Context.Player.IdentityId);
                return;
            }
            if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
            {
                Context.Respond("You cannot use this command in natural gravity!");
                confirmations.Remove(Context.Player.IdentityId);
                return;
            }

            foreach (DeniedLocation denied in AlliancePlugin.HangarDeniedLocations)
            {
                if (Vector3.Distance(Context.Player.GetPosition(), new Vector3(denied.x, denied.y, denied.z)) <= denied.radius)
                {
                    Context.Respond("Cannot buy here, too close to a denied location.");
                    confirmations.Remove(Context.Player.IdentityId);
                    return;
                }
            }
            if (EconUtils.getBalance(Context.Player.IdentityId) >= item.Price)
            {
                if (confirmations.ContainsKey(Context.Player.IdentityId))
                {
                    if (confirmations[Context.Player.IdentityId] >= DateTime.Now)
                    {
                        if (GridManager.LoadGrid(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml", Context.Player.GetPosition(), false, Context.Player.SteamUserId, item.Name))
                        {
                            EconUtils.takeMoney(Context.Player.IdentityId, item.Price);
                            long sellerId = MySession.Static.Players.TryGetIdentityId(item.SellerSteamId);
                            EconUtils.addMoney(sellerId, item.Price);
                            if (AlliancePlugin.GridBackupInstalled)
                            {
                                AlliancePlugin.BackupGridMethod(GridManager.GetObjectBuilders(AlliancePlugin.path + "//ShipMarket//Grids//" + item.ItemId + ".xml"), Context.Player.IdentityId);
                            }
                            item.Buyer = Context.Player.SteamUserId;
                            item.soldAt = DateTime.Now;
                            item.Status = ItemStatus.Sold;
                            Context.Respond("The grid should appear near you.");
                            confirmations.Remove(Context.Player.IdentityId);
                            if (!Directory.Exists(AlliancePlugin.path + "//ShipMarket//Sold//" + item.SellerSteamId))
                            {
                                Directory.CreateDirectory(AlliancePlugin.path + "//ShipMarket//Sold//" + item.SellerSteamId);
                            }
                            list.items.Remove(number);

                            File.Delete(AlliancePlugin.path + "//ShipMarket//ForSale//" + item.ItemId + ".json");
                            utils.WriteToJsonFile<MarketItem>(AlliancePlugin.path + "//ShipMarket//Sold//" + item.SellerSteamId + "//" + item.ItemId, item);
                        }
                        else
                        {
                            Context.Respond("Failed to load the grid! transaction cancelled.");
                            return;
                        }
                    }
                    else
                    {
                        Context.Respond("Time ran out, start again");
                        confirmations[Context.Player.IdentityId] = DateTime.Now.AddSeconds(20);
                    }
                }
                else
                {
                    Context.Respond("Run command again within 20 seconds to confirm. Target grid name is " + item.Name + " Sold by " + AlliancePlugin.GetPlayerName(item.SellerSteamId));
                    confirmations.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(20));
                    Context.Respond("It costs " + String.Format("{0:n0}", item.Price) + " SC.");
                }
            }
            else
            {
                Context.Respond("You cannot afford that. It costs " + String.Format("{0:n0}", item.Price) + " SC.");
            }

        }
        [Command("me", "output the ids of items belonging to you.")]
        [Permission(MyPromoteLevel.Admin)]
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
                    if (item.Value.Status == ItemStatus.Sold)
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
        [Permission(MyPromoteLevel.Admin)]
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
