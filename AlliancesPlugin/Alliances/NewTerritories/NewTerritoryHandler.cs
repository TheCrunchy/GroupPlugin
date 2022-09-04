using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Collections;
using VRageMath;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public static class NewTerritoryHandler
    {
        public static List<Territory> Territories = new List<Territory>();
        public static Dictionary<long, BlockDamageLog> BlockDamages = new Dictionary<long, BlockDamageLog>();

        public class BlockDamageLog
        {
            public long LastAttackerIdentityId;
            public DateTime ExpireAt = DateTime.Now;
        }

        public static void AddToBlockLog(MyCubeBlock block, long attackerIdentityId)
        {
            if (BlockDamages.TryGetValue(block.EntityId, out var log))
            {
                log.LastAttackerIdentityId = attackerIdentityId;
                log.ExpireAt = DateTime.Now.AddMinutes(10);
                return;
            }

            BlockDamages.Add(block.EntityId ,new BlockDamageLog()
            {
                ExpireAt = DateTime.Now.AddMinutes(10),
                LastAttackerIdentityId = attackerIdentityId
            });

        }

        public static void HandleTerritoryStuff()
        {
            var expiredBlocks = (from keyset in BlockDamages where DateTime.Now >= keyset.Value.ExpireAt select keyset.Key).ToList();

            foreach (var id in expiredBlocks)
            {
                BlockDamages.Remove(id);
            }
        }

        public static bool IsPositionInTerritory(Vector3D PlayerPos, Territory territory)
        {
            var distance = Vector3.Distance(PlayerPos, territory.Position);
            return !(distance > territory.Radius);
        }
        public static bool IsPositionInTerritoryCap(Vector3D PlayerPos, Territory territory)
        {
            var distance = Vector3.Distance(PlayerPos, territory.CapPosition);
            return !(distance > territory.Radius);
        }

        public static void TransferOwnership()
        {
            foreach (var territory in Territories.Where(x => x.IsUnderSiege && DateTime.Now >= x.SiegeEndTime))
            {
                var newOwner = territory.AlliancePoints.MaxBy(x => x.Value);
                var alliance = AlliancePlugin.GetAlliance(newOwner.Key);
                territory.Alliance = alliance.AllianceId;

            }
        }

        public static void HandleBlockDeath(MyCubeBlock block)
        {

        }

        public static void CheckForPeopleInSiegedTerritories()
        {
            Dictionary<MyPlayer, Vector3D> PlayerPositions = new Dictionary<MyPlayer, Vector3D>();
            Dictionary<MyPlayer, Guid> MappedAlliances = new Dictionary<MyPlayer, Guid>();
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId);
                if (faction == null)
                {
                    continue;
                }

                var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
                if (alliance == null)
                {
                    continue;
                }
                if (!PlayerPositions.ContainsKey(player))
                {
                    PlayerPositions.Add(player, player.GetPosition());
                    MappedAlliances.Add(player, alliance.AllianceId);
                }
            }
            foreach (var territory in Territories.Where(x =>
                         x.Enabled && x.IsUnderSiege && DateTime.Now >= x.NextSiegeCheck))
            {
                territory.NextSiegeCheck = DateTime.Now.AddMinutes(1);
                foreach (var player in PlayerPositions.Where(player => IsPositionInTerritoryCap(player.Value, territory)))
                {

                }
            }
        }

    }
}
