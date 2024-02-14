using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using GroupMiscellenious.Scripts;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRageMath;

namespace GroupMiscellenious.Scripts
{
    [PatchShim]
    public static class GateScript
    {
        private static int ticks;

        public static int MinGateOffset = -5000;
        public static int MaxGateOffset = 5000;

        public static Dictionary<Guid, JumpGate> AllGates { get; set; } = new Dictionary<Guid, JumpGate>();

        public static void UpdateExample()
        {
            ticks++;
            if (ticks % 128 == 0)
            {
                DoGateLogic();
            }

        }

        public static void Patch(PatchContext ctx)
        {
            Core.UpdateCycle += UpdateExample;
        }

        public static void LoadAllGates()
        {

        }

        public static void DoGateLogic()
        {
            var players = MySession.Static.Players.GetOnlinePlayers();

            if (GateScript.AllGates == null)
            {
                return;
            }

            foreach (var gate in GateScript.AllGates.Values)
            {
                if (!gate.Enabled)
                    continue;

                if (!gate.CanJumpFrom)
                    continue;

                if (gate.TargetGateId == gate.GateId)
                    continue;

                if (!GateScript.AllGates.TryGetValue(gate.TargetGateId, out var target))
                    continue;

                if (!target.Enabled || target == null)
                    continue;
                if (target.TargetGateId == target.GateId)
                    continue;

                if (!gate.WorldName.Equals(MyMultiplayer.Static.HostName))
                    continue;

                if (gate.RequirePilot)
                {
                    DoTravelWithPilot(players, gate, target);
                }
                else
                {
                    var sphere = new BoundingSphereD(gate.Position, 600);
                    var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>();
                    foreach (var player in players)
                    {
                        if (DoTravelWithoutPilot(player, gate, target))
                            continue;
                        else
                            break;
                    }

                    foreach (MyCubeGrid grid in entities.Where(x => x is MyCubeGrid grid))
                    {
                        var owner = FacUtils.GetOwner(grid);
                        var steamId = MySession.Static.Players.TryGetSteamId(owner);
                        var player = MySession.Static.Players.TryGetPlayerBySteamId(steamId);
                        if (player == null)
                            continue;
                        DoGridTravel(gate, grid, player);
                    }
                }
            }

        }

        private static bool DoTravelWithoutPilot(MyPlayer player, JumpGate gate, JumpGate target)
        {
            if (player?.Controller?.ControlledEntity is MyCockpit controller)
            {
                DoGridTravel(gate, controller.CubeGrid, player);
                return true;
            }

            //     AlliancePlugin.Log.Info("1");
            if (player.Character == null)
            {
                return true;
            }

            var Distance = Vector3.Distance(gate.Position, player.Character.PositionComp.GetPosition());
            if (Distance <= gate.RadiusToJump)
            {
                var rand = new Random();
                var offset = GetOffset(rand);
                var newPos = new Vector3D(target.Position + offset);
                var newPosition = MyEntities.FindFreePlace(newPos, 50);
                if (newPosition.Value == null)
                {
                    return false;
                }

                var worldMatrix = MatrixD.CreateWorld(newPosition.Value,
                    player.Character.WorldMatrix.Forward, player.Character.WorldMatrix.Up);
                player.Character.Teleport(worldMatrix);
                Core.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in suit");
            }
            else
            {
                if (gate.fee > 0 && Distance <= 500)
                {
                    GateScript.DoFeeMessage(player, gate, Distance);
                }
                else
                {
                    if (Distance <= 500)
                    {
                        GateScript.SendPlayerNotify(player, 1000,
                            "You will jump in " + Distance + " meters", "Green");
                    }
                }
            }

            return false;
        }

        private static void DoTravelWithPilot(ICollection<MyPlayer> players, JumpGate gate, JumpGate target)
        {
            foreach (var player in players)
            {
                if (!(player?.Controller?.ControlledEntity is MyCockpit controller)) continue;
                DoGridTravel(gate, controller.CubeGrid, player);
            }
        }

        private static Vector3 GetOffset(Random rand)
        {
            var offset = new Vector3(rand.Next(GateScript.MinGateOffset, GateScript.MaxGateOffset),
                rand.Next(GateScript.MinGateOffset, GateScript.MaxGateOffset),
                rand.Next(GateScript.MinGateOffset, GateScript.MaxGateOffset));
            return offset;
        }

        public static void DoGridTravel(JumpGate gate, MyCubeGrid grid, MyPlayer player)
        {
            if (grid.IsStatic)
                return;

            var Distance = Vector3.Distance(gate.Position, grid.PositionComp.GetPosition());

            if (Distance <= gate.RadiusToJump)
            {

                if (GetTargetPosition(gate, grid, out var newPosition)) return;

                var worldMatrix = MatrixD.CreateWorld(newPosition.Value, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
                grid.Teleport(worldMatrix);

                Core.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in " +
                                        grid.DisplayName);
            }
            else
            {
                if (gate.fee > 0 && Distance <= 500)
                {
                    GateScript.DoFeeMessage(player, gate, Distance);
                }
                else
                {
                    if (Distance <= 500)
                    {
                        GateScript.SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters",
                            "Green");
                    }
                }
            }
        }

        private static bool GetTargetPosition(JumpGate gate, MyCubeGrid grid, out Vector3D? newPosition)
        {
            var target = GateScript.AllGates[gate.TargetGateId];
            var rand = new Random();
            var offset = GetOffset(rand);
            var newPos = new Vector3D(target.Position + offset);
            newPosition = MyEntities.FindFreePlace(newPos, (float)GridManager.FindBoundingSphere(grid).Radius);
            if (newPosition.Value == null || newPosition.Value == Vector3D.Zero)
            {
                return true;
            }

            return false;
        }

        public static Boolean SendPlayerNotify(MyPlayer player, int milliseconds, string message, string color)
        {
            var message2 = new NotificationMessage();
            if (messageCooldowns.TryGetValue(player.Identity.IdentityId, out var cooldown))
            {
                if (DateTime.Now < cooldown)
                    return false;

                message2 = new NotificationMessage(message, milliseconds, color);
                //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                messageCooldowns[player.Identity.IdentityId] = DateTime.Now.AddMilliseconds(milliseconds / 2);
                return false;
            }

            message2 = new NotificationMessage(message, milliseconds, color);
            ModCommunication.SendMessageTo(message2, player.Id.SteamId);
            messageCooldowns.Add(player.Identity.IdentityId, DateTime.Now.AddMilliseconds(milliseconds / 2));
            return false;
        }

        public static Dictionary<long, DateTime> messageCooldowns = new Dictionary<long, DateTime>();
        public static Boolean DoFeeMessage(MyPlayer player, JumpGate gate, float Distance)
        {
            //if (gate.itemCostsForUse)
            //{
            //    SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
            //    foreach (var item in gate.itemCostsList)
            //    {
            //        SendPlayerNotify(player, 1000, $"It costs {item.SubTypeId} {item.TypeId} SC to jump.", "Green");
            //        return true;
            //    }
            //}

            //if (gate.fee > 0)
            //{
            //    if (EconUtils.getBalance(player.Identity.IdentityId) >= gate.fee)
            //    {
            SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
            //      SendPlayerNotify(player, 1000, "It costs " + String.Format("{0:n0}", gate.fee) + " SC to jump.", "Green");
            return true;
            //  }
            //  }
            return false;
        }
    }
}


public class JumpGate
{
    public Guid GateId = System.Guid.NewGuid();
    public bool RequirePilot = true;
    public bool CanJumpFrom = true;
    public string WorldName = "";
    public Boolean RequireDrive = true;
    public Boolean UseSafeZones = true;
    public long SafeZoneEntityId = 1;
    public Boolean GeneratedZone2 = false;
    public Guid TargetGateId;
    public Vector3 Position;
    public string GateName;
    public Boolean Enabled = true;
    public int RadiusToJump = 75;
    private FileUtils utils = new FileUtils();
    public Guid OwnerAlliance;
    public string LinkedKoth = "";
    public long fee = 0;
    public long upkeep = 100000000;
    public Boolean CanBeRented = false;
    public int MetaPointRentCost = 100;
    public DateTime NextRentAvailable = DateTime.Now;
    public int DaysPerRent = 7;
    public void Save()
    {
        utils.WriteToXmlFile<JumpGate>($"{Core.path}//JumpGates//{GateId}.xml", this);
        GateScript.AllGates[this.GateId] = this;
    }
    public void Delete()
    {
        File.Delete($"{Core.path}//JumpGates//{GateId}.xml");
    }

    public Boolean itemCostsForUse = false;
    public List<ItemCost> itemCostsList = new List<ItemCost>();

    public class ItemCost
    {
        public int BaseItemAmount = 100;
        public int BlockCountDivision = 1000;
        public string TypeId = "Ore";
        public string SubTypeId = "Iron";
    }
}

