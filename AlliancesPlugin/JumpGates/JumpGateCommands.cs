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
using AlliancesPlugin.Alliances;
using Sandbox.Engine.Multiplayer;
using Sandbox.Common.ObjectBuilders;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.Entity;

namespace AlliancesPlugin.JumpGates
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
                RadiusToJump = radiusToJump,
                WorldName = MyMultiplayer.Static.HostName
            };
            gate.GeneratedZone2 = true;
            MyObjectBuilder_SafeZone objectBuilderSafeZone = new MyObjectBuilder_SafeZone();
            objectBuilderSafeZone.PositionAndOrientation = new MyPositionAndOrientation?(new MyPositionAndOrientation(gate.Position, Vector3.Forward, Vector3.Up));
            objectBuilderSafeZone.PersistentFlags = MyPersistentEntityFlags2.InScene;
            objectBuilderSafeZone.Shape = MySafeZoneShape.Sphere;
            objectBuilderSafeZone.Radius = (float)gate.RadiusToJump;
            objectBuilderSafeZone.Enabled = true;
            objectBuilderSafeZone.DisplayName = gate.GateName + " zone";
            objectBuilderSafeZone.ModelColor = Color.Green.ToVector3();
            objectBuilderSafeZone.AllowedActions = MySafeZoneAction.Drilling | MySafeZoneAction.Building | MySafeZoneAction.Damage | MySafeZoneAction.Grinding | MySafeZoneAction.Shooting | MySafeZoneAction.Welding | MySafeZoneAction.LandingGearLock;
            objectBuilderSafeZone.AccessTypeGrids = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypeFactions = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypePlayers = MySafeZoneAccess.Blacklist;
            MyEntity ent = Sandbox.Game.Entities.MyEntities.CreateFromObjectBuilderAndAdd((MyObjectBuilder_EntityBase)objectBuilderSafeZone, true);
            gate.SafeZoneEntityId = ent.EntityId;
            gate.Save();
            AlliancePlugin.AllGates.Add(gate.GateId, gate);
            Context.Respond("Gate created. To link to another gate use !jumpgate link gateName targetName");
            Context.Respond("Entry radius " + gate.RadiusToJump);
        }

        [Command("zone", "add a safezone to a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddSafeZone(string name)
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

            gate1.GeneratedZone2 = true;
            MyObjectBuilder_SafeZone objectBuilderSafeZone = new MyObjectBuilder_SafeZone();
            objectBuilderSafeZone.PositionAndOrientation = new MyPositionAndOrientation?(new MyPositionAndOrientation(gate1.Position, Vector3.Forward, Vector3.Up));
            objectBuilderSafeZone.PersistentFlags = MyPersistentEntityFlags2.InScene;
            objectBuilderSafeZone.Shape = MySafeZoneShape.Sphere;
            objectBuilderSafeZone.Radius = (float)gate1.RadiusToJump;
            objectBuilderSafeZone.Enabled = true;
            objectBuilderSafeZone.DisplayName = gate1.GateName + " zone";
            objectBuilderSafeZone.ModelColor = Color.Green.ToVector3();
            objectBuilderSafeZone.AllowedActions = MySafeZoneAction.Drilling | MySafeZoneAction.Building | MySafeZoneAction.Damage | MySafeZoneAction.Grinding | MySafeZoneAction.Shooting | MySafeZoneAction.Welding | MySafeZoneAction.LandingGearLock;
            objectBuilderSafeZone.AccessTypeGrids = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypeFactions = MySafeZoneAccess.Blacklist;
            objectBuilderSafeZone.AccessTypePlayers = MySafeZoneAccess.Blacklist;
            MyEntity ent = Sandbox.Game.Entities.MyEntities.CreateFromObjectBuilderAndAdd((MyObjectBuilder_EntityBase)objectBuilderSafeZone, true);
            gate1.SafeZoneEntityId = ent.EntityId;
            
            AlliancePlugin.AllGates[gate1.GateId] = gate1;
            Context.Respond("Added the zone?");
            gate1.Save();
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
                Context.Respond("Only members of an alliance can set a gate fee.");
                return;
            }
            if (alliance != null)
            {
                if (alliance.SupremeLeader == Context.Player.SteamUserId || alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.PayFromBank))
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

        [Command("rent", "rent a gate")]
        [Permission(MyPromoteLevel.None)]
        public void RentGate(string name)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may rent a gate.");
                return;
            }
            if (alliance != null)
            {
                if (alliance.SupremeLeader == Context.Player.SteamUserId || alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.PayFromBank))
                {
                    JumpGate gate1 = null;
                    JumpGate gate2 = null;
                    AlliancePlugin.LoadAllGates();
                    foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
                    {
                        if (gate.GateName.Equals(name) && gate.CanBeRented && DateTime.Now >= gate.NextRentAvailable)
                        {
                            gate1 = gate;
                            gate2 = AlliancePlugin.AllGates[gate.TargetGateId];
                            break;
                        }
                    }
                    if (gate1 == null || gate2 == null)
                    {
                        Context.Respond("Could not find one of those gates.");
                        return;
                    }
                    if (alliance.CurrentMetaPoints >= gate1.MetaPointRentCost)
                    {
                        alliance.CurrentMetaPoints -= gate1.MetaPointRentCost;
                        gate1.OwnerAlliance = alliance.AllianceId;
                        gate2.OwnerAlliance = alliance.AllianceId;
                        gate1.NextRentAvailable = DateTime.Now.AddDays(gate1.DaysPerRent);
                        gate2.NextRentAvailable = DateTime.Now.AddDays(gate1.DaysPerRent);
                        AlliancePlugin.AllGates[gate1.GateId] = gate1;
                        AlliancePlugin.AllGates[gate2.GateId] = gate2;
                        gate1.Save();
                        gate2.Save();
                        Context.Respond("Successfully rented gate for " + gate1.DaysPerRent + " Days. Fees can now be set with !jumpgate fee <gateName> <amount>");
                        Context.Respond("Gate names, " + gate1.GateName + ", " + gate2.GateName);
                        AlliancePlugin.SaveAllianceData(alliance);
                        return;
                    }
                    else
                    {
                        Context.Respond("Cannot afford the meta point cost of " + gate1.MetaPointRentCost);
                        return;
                    }
                }
                else
                {
                    Context.Respond("You dont have the rank to do this.");
                }

            }


        }

        [Command("rentable", "list all rentable gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputRentableGates()
        {
            string response = "";
            foreach (JumpGate gate in AlliancePlugin.AllGates.Values)
            {
             if (gate.CanBeRented && DateTime.Now >= gate.NextRentAvailable)
                {
                    response += "/n" + gate.GateName + " meta point cost " + gate.DaysPerRent;
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
