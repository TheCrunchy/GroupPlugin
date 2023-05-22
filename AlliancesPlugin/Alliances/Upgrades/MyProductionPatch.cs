using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.KOTH;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    [PatchShim]
    public static class MyProductionPatch
    {
      
        public static Dictionary<int, RefineryUpgrade> upgrades = new Dictionary<int, RefineryUpgrade>();
        public static Dictionary<int, AssemblerUpgrade> assemblerupgrades = new Dictionary<int, AssemblerUpgrade>();
        public static Dictionary<long, bool> IsInsideTerritory = new Dictionary<long, bool>();
        public static Dictionary<long, DateTime> TimeChecks = new Dictionary<long, DateTime>();
        public static Dictionary<long, Guid> InsideHere = new Dictionary<long, Guid>();

        public static double GetRefineryYieldMultiplier(long PlayerIdentityId, MyRefinery refin)
        {
            var faction = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);

            if (faction == null)
            {
                if (AlliancePlugin.config.EnableOptionalWar)
                {
                    return AlliancePlugin.config.RefineryYieldMultiplierIfDisabled;
                }
            }
            else
            {
                double buff = 1;
                double EndMultiplier = 1;
                if (AlliancePlugin.config.EnableOptionalWar)
                {
                    EndMultiplier = AlliancePlugin.warcore.participants.FactionsAtWar.Contains(faction.FactionId)
                        ? AlliancePlugin.config.RefineryYieldMultiplierIfEnabled
                        : AlliancePlugin.config.RefineryYieldMultiplierIfDisabled;
                }

                var alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetFactionByTag(faction.Tag));
                if (alliance == null || alliance.AssemblerUpgradeLevel <= 0) return buff * EndMultiplier;
                if (!upgrades.TryGetValue(alliance.RefineryUpgradeLevel, out var upgrade)) return buff * EndMultiplier;
                if (TimeChecks.TryGetValue(refin.EntityId, out var time))
                {
                    if (DateTime.Now >= time)
                    {
                        TimeChecks[refin.EntityId] = DateTime.Now.AddMinutes(1);
                        if (InsideHere.TryGetValue(refin.EntityId, out var terId))
                        {
                            if (AlliancePlugin.Territories.TryGetValue(terId, out var ter))
                            {
                                var distance = Vector3.Distance(refin.CubeGrid.PositionComp.GetPosition(),
                                    ter.Position);
                                if (distance <= ter.Radius)
                                {
                                    IsInsideTerritory.Remove(refin.EntityId);
                                    IsInsideTerritory.Add(refin.EntityId, true);
                                }
                                else
                                {
                                    InsideHere.Remove(refin.EntityId);
                                    IsInsideTerritory.Remove(refin.EntityId);
                                }
                            }
                        }
                        else
                        {
                            foreach (var ter in from ter in AlliancePlugin.Territories.Values
                                     let distance = Vector3.Distance(refin.CubeGrid.PositionComp.GetPosition(),
                                        ter.Position)
                                     where distance <= ter.Radius
                                     select ter)
                            {
                                IsInsideTerritory.Remove(refin.EntityId);
                                IsInsideTerritory.Add(refin.EntityId, true);
                                InsideHere.Remove(refin.EntityId);

                                InsideHere.Add(refin.EntityId, ter.Id);
                            }
                        }
                    }
                }
                else
                {
                    TimeChecks.Add(refin.EntityId, DateTime.Now.AddMinutes(0.01));
                }

                //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);
                if (IsInsideTerritory.TryGetValue(refin.EntityId, out var isInside))
                {
                    if (isInside)
                    {
                        //   AlliancePlugin.Log.Info("inside territory");
                        buff += upgrade.getRefineryBuffTerritory(refin.BlockDefinition.Id.SubtypeName);
                    }
                    else
                    {
                        //   AlliancePlugin.Log.Info("not inside territory");
                        buff += upgrade.getRefineryBuff(refin.BlockDefinition.Id.SubtypeName);
                    }
                }
                else
                {
                    //   AlliancePlugin.Log.Info("not inside territory");
                    buff += upgrade.getRefineryBuff(refin.BlockDefinition.Id.SubtypeName);
                }

                return buff * EndMultiplier;
            }

            return 1;
        }
        public static double GetAssemblerSpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            var faction = MySession.Static.Factions.GetPlayerFaction(PlayerId);
            double buff = 1;
            double endMultiplier = 1;
            if (faction == null)
            {
                if (AlliancePlugin.config.EnableOptionalWar)
                {
                    return AlliancePlugin.config.AssemblerSpeedMultiplierIfDisabled;
                }
            }
            else
            {
                if (AlliancePlugin.config.EnableOptionalWar)
                {
                    endMultiplier = AlliancePlugin.warcore.participants.FactionsAtWar.Contains(faction.FactionId)
                        ? AlliancePlugin.config.AssemblerSpeedMultiplierIfEnabled
                        : AlliancePlugin.config.AssemblerSpeedMultiplierIfDisabled;
                }

                if (Assembler.GetOwnerFactionTag().Length <= 0) return buff * endMultiplier;

                var alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetFactionByTag(Assembler.GetOwnerFactionTag()));
                if (alliance == null)
                {
                    return buff * endMultiplier;
                }
                if (alliance.AssemblerUpgradeLevel == 0)
                {
                    return buff * endMultiplier;
                }
                if (assemblerupgrades.TryGetValue(alliance.AssemblerUpgradeLevel, out var upgrade))
                {
                    if (TimeChecks.TryGetValue(Assembler.EntityId, out var time))
                    {
                        if (DateTime.Now >= time)
                        {
                            TimeChecks[Assembler.EntityId] = DateTime.Now.AddMinutes(1);
                            if (InsideHere.TryGetValue(Assembler.EntityId, out var terId))
                            {
                                if (AlliancePlugin.Territories.TryGetValue(terId, out var ter))
                                {
                                    var distance = Vector3.Distance(
                                        Assembler.CubeGrid.PositionComp.GetPosition(),
                                       ter.Position);
                                    if (distance <= ter.Radius)
                                    {
                                        IsInsideTerritory.Remove(Assembler.EntityId);
                                        IsInsideTerritory.Add(Assembler.EntityId, true);
                                    }
                                    else
                                    {
                                        InsideHere.Remove(Assembler.EntityId);
                                        IsInsideTerritory.Remove(Assembler.EntityId);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var ter in from ter in AlliancePlugin.Territories.Values
                                         let distance = Vector3.Distance(
                                             Assembler.CubeGrid.PositionComp.GetPosition(),
                                             ter.Position)
                                         where distance <= ter.Radius
                                         select ter)
                                {
                                    IsInsideTerritory.Remove(Assembler.EntityId);
                                    IsInsideTerritory.Add(Assembler.EntityId, true);
                                    InsideHere.Remove(Assembler.EntityId);

                                    InsideHere.Add(Assembler.EntityId, ter.Id);
                                }
                            }
                        }
                    }
                    else
                    {
                        TimeChecks.Add(Assembler.EntityId, DateTime.Now.AddMinutes(0.01));
                    }
                }

                //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);
                if (IsInsideTerritory.TryGetValue(Assembler.EntityId, out var isInside))
                {
                    if (isInside)
                    {

                        buff += (float)upgrade.getAssemblerBuffTerritory(Assembler.BlockDefinition.Id.SubtypeName);
                    }
                    else
                    {
                        buff += (float)upgrade.getAssemblerBuff(Assembler.BlockDefinition.Id.SubtypeName);
                    }
                }
                else
                {
                    buff += (float)upgrade.getAssemblerBuff(Assembler.BlockDefinition.Id.SubtypeName);
                }
            }

            return buff * endMultiplier;
        }
    }
}
