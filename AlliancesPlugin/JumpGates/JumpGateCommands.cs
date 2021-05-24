using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin
{
    [Category("jumpgate")]
    public class JumpGateCommands : CommandModule
    {
        [Command("refresh", "refresh the loaded gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateGate()
        {
            AlliancePlugin.LoadAllGates();
            Context.Respond("Refreshed the gates!");
            Context.Respond(AlliancePlugin.AllGates.Count + " Gates Loaded");
        }
        [Command("create", "create a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateGate(string name, int radiusToJump = 75)
        {
            AlliancePlugin.LoadAllGates();
            foreach (JumpGate tempgate in AlliancePlugin.AllGates.Values)
            {
                if (tempgate.GateName.ToLower().Equals(name.ToLower()))
                {
                    Context.Respond("Gate with that name already exists!");
                    return;
                }
            }
            JumpGate gate = new JumpGate
            {
                Position = Context.Player.Character.GetPosition(),
                GateName = name,
                RadiusToJump = radiusToJump
            };
            AlliancePlugin.AllGates.Add(gate.GateId, gate);
            gate.Save();
            Context.Respond("Gate created. To link to another gate use !jumpgate link gateName targetName");
            Context.Respond("Entry radius " + gate.RadiusToJump);
        }
        [Command("toggle", "toggle activated state of a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void ToggleGate(string name)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
            }
            if (gate1 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }

            gate1.Enabled = !gate1.Enabled;
            Context.Respond("Gate toggled to " + gate1.Enabled);
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            gate1.Save();
        }
        [Command("setradius", "toggle activated state of a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetRadiusGate(string name, int amount)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
            }
            if (gate1 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }

            gate1.RadiusToJump = amount;
            Context.Respond("Changed radius to " + gate1.RadiusToJump + " metres.");
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            gate1.Save();
        }
        [Command("delete", "delete a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteGate(string name)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
            }
            if (gate1 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }

            AlliancePlugin.AllGates.Remove(gate1.GateId);
            gate1.Delete();
        }
        [Command("fee", "set the fee to use these gates")]
        [Permission(MyPromoteLevel.None)]
        public void SetFee(string name, string target, string inputAmount)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
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
            if (amount >= AlliancePlugin.config.MaximumGateFee)
            {
                Context.Respond("Amount exceeds the maximum of " + String.Format("{0:n0}", AlliancePlugin.config.MaximumGateFee) + " SC.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may access a bank.");
                return;
            }
            if (alliance != null)
            {
                if (alliance.admirals.Contains(Context.Player.SteamUserId) || alliance.SupremeLeader == Context.Player.SteamUserId)
                {
                    JumpGate gate1 = null;
                    JumpGate gate2 = null;

                    foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
                    {
                        if (gate.GateName.Equals(name) && gate.OwnerAlliance == alliance.AllianceId)
                        {
                            gate1 = gate;
                            continue;
                        }
                        if (gate.GateName.Equals(target) && gate.OwnerAlliance == alliance.AllianceId)
                        {
                            gate2 = gate;
                            continue;
                        }
                    }
                    if (gate1 == null || gate2 == null)
                    {
                        Context.Respond("Could not find one of those gates, does the alliance own it?.");
                        return;
                    }
                    Context.Respond("Fee updated.");
                    gate1.fee = amount;
                    gate2.fee = amount;
                    AlliancePlugin.AllGates[gate1.GateId] = gate1;
                    AlliancePlugin.AllGates[gate2.GateId] = gate2;
                    gate1.Save();
                    gate2.Save();

                }else {
                    Context.Respond("You dont have the rank to do this.");
                }

            }

        
        }
        [Command("list", "list all loaded gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputGates()
        {
            
            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                Context.Respond(gate.GateName);
            }
         
        }
        [Command("setowner", "set the owner of a gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void LinkGate(string name, string target, string alliance)
        {
            JumpGate gate1 = null;
            JumpGate gate2 = null;

            Alliance tempalliance = null;
            foreach (Alliance alliance1 in AlliancePlugin.AllAlliances.Values)
            {
                if (alliance1.name.Equals(alliance))
                {
                    tempalliance = alliance1;
                }
            }
            if (tempalliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }
            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
                if (gate.GateName.Equals(target))
                {
                    gate2 = gate;
                    continue;
                }
            }
            if (gate1 == null || gate2 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }
            Context.Respond("Gates linked!");
            gate1.OwnerAlliance = tempalliance.AllianceId;
            gate2.OwnerAlliance = tempalliance.AllianceId;
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            AlliancePlugin.AllGates[gate2.GateId] = gate2;
            gate1.Save();
            gate2.Save();
        }

        [Command("link", "link two gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void LinkGate(string name, string target)
        {
            JumpGate gate1 = null;
            JumpGate gate2 = null;

            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
                if (gate.GateName.Equals(target))
                {
                    gate2 = gate;
                    continue;
                }
            }
            if (gate1 == null || gate2 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }
            Context.Respond("Gates linked!");
            gate1.TargetGateId = gate2.GateId;
            gate2.TargetGateId = gate1.GateId;
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            AlliancePlugin.AllGates[gate2.GateId] = gate2;
            gate1.Save();
            gate2.Save();
        }
        [Command("unlink", "link two gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnLinkGate(string name, string target)
        {
            JumpGate gate1 = null;
            JumpGate gate2 = null;

            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
                if (gate.GateName.Equals(name))
                {
                    gate1 = gate;
                    continue;
                }
                if (gate.GateName.Equals(target))
                {
                    gate2 = gate;
                    continue;
                }
            }
            if (gate1 == null || gate2 == null)
            {
                Context.Respond("Could not find one of those gates.");
                return;
            }
            Context.Respond("Gates linked!");
            gate1.TargetGateId = gate1.GateId;
            gate2.TargetGateId = gate2.GateId;
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            AlliancePlugin.AllGates[gate2.GateId] = gate2;
            gate1.Save();
            gate2.Save();
        }
    }
}
