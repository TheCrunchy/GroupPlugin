using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Territory.Territory_Version_2.Models;

namespace Territory.Territory_Version_2.Interfaces
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
