using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Scripts
{
    [PatchShim]
    public class FixWarStuff : CommandModule
    {
        private const int ReputationThreshold = -10;
        private const int FacToProcessPerUpdate = 50;
        private const int MinutesBeforeRecheck = 60;

        private static Dictionary<long, DateTime> ProcessAgain = new Dictionary<long, DateTime>();

        public static void Patch(PatchContext ctx)
        {
            Core.UpdateCycle += Update;
        }

        private static int tickCount = 0;


        [Command("manualwarstuff", "create")]
        [Permission(MyPromoteLevel.Admin)]
        public void Manual(int factionAmount = 10000, int reputation = -10)
        {
            ProcessAgain.Clear();
            ProcessFactions(factionAmount, reputation);
        }

        public static void Update()
        {
            tickCount++;

            if (tickCount % 1800 != 0)
                return;

            ProcessFactions(FacToProcessPerUpdate, ReputationThreshold);
        }

        private static void ProcessFactions(int toProcess, int repThreshold)
        {
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

                if (facsProcessed >= toProcess)
                {
                    return;
                }

                facsProcessed++;
                ProcessAgain[firstFaction.FactionId] = DateTime.Now.AddMinutes(MinutesBeforeRecheck);

                foreach (var secondFaction in factions)
                {
                    if (firstFaction.FactionId == secondFaction.FactionId)
                        continue;

                    if (!MySession.Static.Factions.AreFactionsNeutrals(firstFaction.FactionId, secondFaction.FactionId))
                        continue;

                    var relation = MySession.Static.Factions
                        .GetRelationBetweenFactions(firstFaction.FactionId, secondFaction.FactionId);

                    if (relation.Item2 > repThreshold)
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
