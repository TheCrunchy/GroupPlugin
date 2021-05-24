﻿using Sandbox.Game.Entities;
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
        [Command("testjump", "Move a grid to input position")]
        [Permission(MyPromoteLevel.Admin)]
        public void BankLog(int x, int y, int z)
        {
            if (Context.Player.Controller.ControlledEntity is MyCockpit controller)
            {
                MyCubeGrid grid = controller.CubeGrid;
                MatrixD worldMatrix = MatrixD.CreateWorld(new Vector3(x, y, z), grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
                grid.Teleport(worldMatrix);
            }
        }

        [Command("create", "create a gate")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateGate(string name)
        {
            JumpGate gate = new JumpGate
            {
                Position = Context.Player.Character.GetPosition(),
                GateName = name
            };
            AlliancePlugin.AllGates.Add(gate.GateId, gate);
            gate.Save();
            Context.Respond("Gate created. To link to another gate use !jumpgate link gateName targetName");
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
    }
}