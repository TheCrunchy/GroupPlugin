using System;
using System.Threading.Tasks;

namespace Territory.Territories.Interfaces
{
    public interface ISecondaryLogic
    {
        bool Enabled { get; set; } 
        //if this task returns false, it will end the loop for that capture points secondary logics, example being failed upkeep
        Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory);
        DateTime NextLoop { get; set; }
        int SecondsBetweenLoops { get; set; }
        bool CanLoop();
        int Priority { get; set; }

    }

}
