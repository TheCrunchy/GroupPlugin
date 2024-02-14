using System;
using System.Text;
using GroupMiscellenious.Scripts;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace GroupMiscellenious.Commands
{
    [Category("jumpgate")]
    public class JumpGateCommands : CommandModule
    {
        [Command("refresh", "refresh the loaded gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateGate()
        {
            GateScript.LoadAllGates();
            Context.Respond("Refreshed the gates!");
            Context.Respond($"{GateScript.AllGates.Count} Gates Loaded");
        }

        [Command("create", "create a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateGate(string name, int radiusToJump = 75, bool generateZone = true)
        {
       
            foreach (JumpGate tempgate in GateScript.AllGates.Values)
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
            if (generateZone)
            {
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
            }
            gate.Save();
            GateScript.AllGates.Add(gate.GateId, gate);
            Context.Respond("Gate created. To link to another gate use !jumpgate link gateName targetName");
            Context.Respond("Entry radius " + gate.RadiusToJump);
        }

        [Command("zone", "add a safezone to a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddSafeZone(string name)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in GateScript.AllGates.Values)
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

            GateScript.AllGates[gate1.GateId] = gate1;
            Context.Respond("Added the zone?");
            gate1.Save();
        }

        [Command("toggle", "toggle activated state of a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void ToggleGate(string name)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in GateScript.AllGates.Values)
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
            GateScript.AllGates[gate1.GateId] = gate1;
            gate1.Save();
        }

        [Command("setradius", "toggle activated state of a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetRadiusGate(string name, int amount)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in GateScript.AllGates.Values)
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
            GateScript.AllGates[gate1.GateId] = gate1;
            gate1.Save();
        }

        [Command("delete", "delete a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteGate(string name)
        {
            JumpGate gate1 = null;


            foreach (JumpGate gate in GateScript.AllGates.Values)
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

            GateScript.AllGates.Remove(gate1.GateId);
            gate1.Delete();
        }
        
        [Command("list", "list all loaded gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputGates()
        {

            foreach (JumpGate gate in GateScript.AllGates.Values)
            {
                Context.Respond(gate.GateName);
            }

        }


        [Command("link", "link two gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void LinkGate(string name, string target)
        {
            JumpGate gate1 = null;
            JumpGate gate2 = null;

            foreach (JumpGate gate in GateScript.AllGates.Values)
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
            GateScript.AllGates[gate1.GateId] = gate1;
            GateScript.AllGates[gate2.GateId] = gate2;
            gate1.Save();
            gate2.Save();
        }

        [Command("unlink", "link two gates")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnLinkGate(string name, string target)
        {
            JumpGate gate1 = null;
            JumpGate gate2 = null;

            foreach (JumpGate gate in GateScript.AllGates.Values)
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
            GateScript.AllGates[gate1.GateId] = gate1;
            GateScript.AllGates[gate2.GateId] = gate2;
            gate1.Save();
            gate2.Save();
        }
    }
}