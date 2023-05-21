using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using Sandbox.Game.World;

namespace AlliancesPlugin.Territory_Version_2.PointOwners
{
    public class FactionPointOwner : IPointOwner
    {
        public long FactionId;
        public object GetOwner()
        {
            var faction = MySession.Static.Factions.TryGetFactionById(FactionId);
            return faction;
        }
    }
}
