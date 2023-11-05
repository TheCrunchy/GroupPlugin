using CrunchGroup.Territories.Interfaces;
using Sandbox.Game.World;

namespace CrunchGroup.Territories.PointOwners
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
