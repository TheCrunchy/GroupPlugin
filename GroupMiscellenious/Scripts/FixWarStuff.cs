using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Scripts
{
    [PatchShim]
    public static class FixWarStuff
    {
        private const int ReputationThreshold = -250;
        private const int FacToProcessPerUpdate = 10;

        private static Dictionary<long, DateTime> ProcessAgain = new Dictionary<long, DateTime>();


        public static void Patch(PatchContext ctx)
        {
            Core.UpdateCycle += Update;
        }

        private static int tickCount = 0;

        public static void Update()
        {
            tickCount++;

            if (tickCount % 1800 != 0)
                return;

            var factions = MySession.Static.Factions
                .GetAllFactions()
                .Where(f => !f.IsEveryoneNpc() && f.Tag.Length < 4)
                .ToList();
            var facsProcessed = 0;

            foreach (var firstFaction in factions)
            {
                if (ProcessAgain.TryGetValue(firstFaction.FactionId, out var reprocessTime))
                {
                    if (DateTime.Now < reprocessTime)
                    {
                        continue;
                    }
                }
                if (facsProcessed >= FacToProcessPerUpdate)
                {
                    return;
                }

                facsProcessed++;
                ProcessAgain[firstFaction.FactionId] = DateTime.Now.AddMinutes(30);

                foreach (var secondFaction in factions)
                {
                    if (firstFaction.FactionId == secondFaction.FactionId)
                        continue; 

                    if (!MySession.Static.Factions.AreFactionsNeutrals(firstFaction.FactionId, secondFaction.FactionId))
                        continue;

                    var relation = MySession.Static.Factions
                        .GetRelationBetweenFactions(firstFaction.FactionId, secondFaction.FactionId);

                    if (relation.Item2 > ReputationThreshold)
                        continue;

                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        MyFactionCollection.DeclareWar(firstFaction.FactionId, secondFaction.FactionId);
                    });
                }
            }
        }
    }
}
