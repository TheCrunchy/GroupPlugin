using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
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
            if (!OptinCore.config.EnableOptionalWar)
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
                if (EconUtils.getBalance(Context.Player.IdentityId) >= OptinCore.config.EnableWarCost)
                {
                    if (OptinCore.AddToWarParticipants(fac.FactionId))
                    {
                        EconUtils.takeMoney(Context.Player.IdentityId, OptinCore.config.EnableWarCost);
                        Context.Respond("War is now enabled.");
                    }
                    else
                    {
                        Context.Respond("War could not be enabled.");
                    }
                }
                else
                {
                    Context.Respond($"Cannot afford the cost of {OptinCore.config.EnableWarCost:C}");
                }
            }
            else
            {
                Context.Respond("Only founders and leaders can enable war.");
                return;
            }
        }

        [Command("disable", "disable war.")]
        [Permission(MyPromoteLevel.None)]
        public void DisableWar()
        {
            if (!OptinCore.config.EnableOptionalWar)
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
                if (EconUtils.getBalance(Context.Player.IdentityId) >= OptinCore.config.DisableWarCost)
                {
                    if (OptinCore.RemoveFromWarParticipants(fac.FactionId))
                    {
                        EconUtils.takeMoney(Context.Player.IdentityId, OptinCore.config.DisableWarCost);
                        Context.Respond("War is now disabled.");
                    }
                    else
                    {
                        Context.Respond("War could not be disabled.");
                    }
                }
                else
                {
                    Context.Respond($"Cannot afford the cost of {OptinCore.config.DisableWarCost:C}");
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
            if (!OptinCore.config.EnableOptionalWar)
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
                        OptinCore.DoNeutralUpdate(fac.FactionId, fac2.FactionId);
                    }
                }
                processed += 1;
            }
            Context.Respond($"Done, processed {processed} factions.");
        }
    }
}
