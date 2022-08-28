using Sandbox.Game.World;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.WarOptIn
{
    [Category("war")]
    public class WarCommands : CommandModule
    {

        [Command("addterritory", "add territory at current position to exclude pvp protections")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddTerritory(int Radius)
        {
            KamikazeTerritories.MessageHandler.AddOtherTerritory(Context.Player.GetPosition(), Radius);
            Context.Respond("Done");
        }

        [Command("enable", "Enable war.")]
        [Permission(MyPromoteLevel.None)]
        public void EnableWar()
        {
            if (!AlliancePlugin.warcore.config.EnableOptionalWar)
            {
                Context.Respond("Optional war is not enabled.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can enable war.");
                return;
            }

            if (fac.IsFounder(Context.Player.IdentityId) || fac.IsLeader(Context.Player.IdentityId)) {
                if (EconUtils.getBalance(Context.Player.IdentityId) >= AlliancePlugin.warcore.config.EnableWarCost)
                {
                    if (AlliancePlugin.warcore.AddToWarParticipants(fac.FactionId))
                    {
                        EconUtils.takeMoney(Context.Player.IdentityId, AlliancePlugin.warcore.config.EnableWarCost);
                        Context.Respond("War is now enabled.");
                    }
                    else
                    {
                        Context.Respond($"War could not be enabled. Current status: {AlliancePlugin.warcore.GetStatus(fac.FactionId)}");
                    }
                }
                else
                {
                    Context.Respond($"Cannot afford the cost of {AlliancePlugin.warcore.config.EnableWarCost:C}");
                }
            }
            else
            {
                Context.Respond("Only founders and leaders can enable war.");
                return;
            }
        }

        [Command("list", "List factions with war enabled.")]
        [Permission(MyPromoteLevel.None)]
        public void ListWar()
        {
            if (!AlliancePlugin.warcore.config.EnableOptionalWar)
            {
                Context.Respond("Optional war is not enabled.");
                return;
            }
            StringBuilder sb = new StringBuilder();

            foreach (long id in AlliancePlugin.warcore.participants.FactionsAtWar)
            {
                var fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine($"{fac.Name} - {fac.Tag}");
                }
            }
            DialogMessage m = new DialogMessage("Factions Opted in", "", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }

        [Command("search", "Check if a faction has enabled war")]
        [Permission(MyPromoteLevel.None)]
        public void ListWar(string tag)
        {
            if (!AlliancePlugin.warcore.config.EnableOptionalWar)
            {
                Context.Respond("Optional war is not enabled.");
                return;
            }
            
            StringBuilder sb = new StringBuilder();
   
            foreach (long id in AlliancePlugin.warcore.participants.FactionsAtWar)
            {
                var fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    if (fac.Tag == tag)
                    {
                        Context.Respond("Faction has enabled war.");
                        return;
                    }
                }
            }
            Context.Respond("Faction has not enabled war.");
        }

        [Command("disable", "disable war.")]
        [Permission(MyPromoteLevel.None)]
        public void DisableWar()
        {
            if (!AlliancePlugin.warcore.config.EnableOptionalWar)
            {
                Context.Respond("Optional war is not enabled.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can disable war.");
                return;
            }

            if (fac.IsFounder(Context.Player.IdentityId) || fac.IsLeader(Context.Player.IdentityId))
            {
                if (EconUtils.getBalance(Context.Player.IdentityId) >= AlliancePlugin.warcore.config.DisableWarCost)
                {
                    if (AlliancePlugin.warcore.RemoveFromWarParticipants(fac.FactionId))
                    {
                        EconUtils.takeMoney(Context.Player.IdentityId, AlliancePlugin.warcore.config.DisableWarCost);
                        Context.Respond("War is now disabled.");
                    }
                    else
                    {
                        Context.Respond($"War could not be disabled. Current status: {AlliancePlugin.warcore.GetStatus(fac.FactionId)}");
                    }
                }
                else
                {
                    Context.Respond($"Cannot afford the cost of {AlliancePlugin.warcore.config.DisableWarCost:C}");
                }
            }
            else
            {
                Context.Respond("Only founders and leaders can enable war.");
                return;
            }
        }


        [Command("forceneutral", "force all neutrals")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceAllNeutrals()
        {
            var processed = 0;
            foreach (var fac in MySession.Static.Factions.GetAllFactions())
            {
                if (fac.Tag.Length > 3)
                    continue;
                foreach (var fac2 in MySession.Static.Factions.GetAllFactions())
                {
                    if (fac2.Tag.Length > 3)
                        continue;
                    if (fac != fac2)
                    {
                        AlliancePlugin.warcore.DoNeutralUpdate(fac.FactionId, fac2.FactionId);
                    }
                }
                processed += 1;
            }
            Context.Respond($"Done, processed {processed} factions.");
        }
    }
}
