using Sandbox.Game.World;
using Territory.Territories.Interfaces;

namespace Territory.Territories.PointOwners
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
