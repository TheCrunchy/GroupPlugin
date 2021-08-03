using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Special_Designation
{
    [Category("mc")]
    public class MiningCommands : CommandModule
    {
        [Command("take", "Force take a contract")]
        [Permission(MyPromoteLevel.Admin)]
        public void TakeContract()
        {
            MiningContract contract = AlliancePlugin.GeneratedToPlayer(AlliancePlugin.GetPlayerContract());
            DrillPatch.playerWithContract.Remove(Context.Player.SteamUserId);
            DrillPatch.playerWithContract.Add(Context.Player.SteamUserId, contract);
            FileUtils utils = new FileUtils();
            utils.WriteToXmlFile<MiningContract>(AlliancePlugin.path + "//MiningStuff//PlayerData//" + Context.Player.SteamUserId + ".xml", contract);

        }

        [Command("quit", "Force quit a contract")]
        [Permission(MyPromoteLevel.None)]
        public void quitContract()
        {
            MiningContract contract = new MiningContract();
            contract.steamId = Context.Player.SteamUserId;
            DrillPatch.playerWithContract.Remove(Context.Player.SteamUserId);
            FileUtils utils = new FileUtils();
            utils.WriteToXmlFile<MiningContract>(AlliancePlugin.path + "//MiningStuff//PlayerData//" + Context.Player.SteamUserId + ".xml", contract);
            Context.Respond("Contract successfully quit.");
        }

        [Command("info", "show info")]
        [Permission(MyPromoteLevel.None)]
        public void infoContract()
        {
            if (DrillPatch.playerWithContract.ContainsKey(Context.Player.SteamUserId))
            {
                MiningContract contract = DrillPatch.playerWithContract[Context.Player.SteamUserId];
                if (String.IsNullOrEmpty(contract.OreSubType))
                {
                    Context.Respond("You dont currently have a contract.");

                }
                else
                {
                    StringBuilder contractDetails = new StringBuilder();
                    if (contract.minedAmount >= contract.amountToMine)
                    {
                        contract.DoPlayerGps(Context.Player.Identity.IdentityId);
                        contractDetails.AppendLine("Deliver " + contract.OreSubType + " Ore " + String.Format("{0:n0}", contract.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", contract.contractPrice) + " SC.");
                    }
                    else
                    {
                        contractDetails.AppendLine("Mine " + contract.OreSubType + " Ore " + String.Format("{0:n0}", contract.minedAmount) + " / " + String.Format("{0:n0}", contract.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", contract.contractPrice) + " SC.");
                    }
                  
                    DialogMessage m = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                 
                }
            }
            else
            {
                Context.Respond("You dont currently have a contract.");
            }
        }
    }
}

