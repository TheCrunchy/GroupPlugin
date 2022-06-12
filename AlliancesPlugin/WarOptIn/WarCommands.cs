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
                        Context.Respond("War could not be enabled.");
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
            foreach (long id in OptinCore.participants.FactionsAtWar)
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
            foreach (long id in OptinCore.participants.FactionsAtWar)
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
                        Context.Respond("War could not be disabled.");
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
            if (!AlliancePlugin.warcore.config.EnableOptionalWar)
            {
                Context.Respond("Optional war is not enabled.");
                return;
            }
            int processed = 0;
            foreach (MyFaction fac in MySession.Static.Factions.GetAllFactions())
            {
                foreach (MyFaction fac2 in MySession.Static.Factions.GetAllFactions())
                {
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
