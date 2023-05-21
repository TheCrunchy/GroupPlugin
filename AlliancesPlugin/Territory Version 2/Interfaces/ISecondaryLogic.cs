using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;

namespace AlliancesPlugin.Territory_Version_2.Interfaces
{
    public interface ISecondaryLogic
    {
        //return if the cap has succeeded, and if so an object of who now owns it
        Task DoSecondaryLogic(ICapLogic point, Territory territory);
        DateTime NextLoop { get; set; }
        int SecondsBetweenLoops { get; set; }
    }

}
