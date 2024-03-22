using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRageMath;

namespace CrunchGroup.Territories.Interfaces
{
    public interface ICapLogic
    {
        //return if the cap has succeeded, and if so an object of who now owns it
        Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Models.Territory territory);
        bool CanLoop();
        List<ISecondaryLogic> SecondaryLogics { get; set; }
        DateTime NextLoop { get; set; }
        int SecondsBetweenLoops { get; set; }
        IPointOwner PointOwner { get; set; }
        string PointName { get; set; }
        void AddSecondaryLogic(ISecondaryLogic logic);
        public void SetPosition(Vector3 position);

        public Vector3 GetPointsLocationIfSet();
    }
}
