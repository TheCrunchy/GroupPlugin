using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.NewTerritoryCapture
{
    public static class CaptureHandler
    {
        private static List<Territory> territories = new List<Territory>();

        public static async Task DoCaps()
        {
            foreach (var territory in AlliancePlugin.Territories)
            {
                foreach (var point in territory.Value.CapturePoints)
                {
                    ICapLogic CapLogic;

                    CapLogic = point;

                    try
                    {
                        var capResult = await CapLogic.ProcessCap(point, territory.Value);
                        if (capResult.Item1)
                        {
                            AlliancePlugin.Log.Info("Cap did succeed");
                        }
                        else
                        {
                            AlliancePlugin.Log.Info("Cap did not succeed");
                        }
                    }
                    catch (Exception e)
                    {
                        AlliancePlugin.Log.Error($"Error on capture logic loop of type {CapLogic.GetType()}, { e.ToString()}");
                    }
                    //mostly testing, i dont intend to do anything here if a cap is or isnt successful, other than change the territory owner if % is high enough 

                    if (CapLogic.SecondaryLogics != null)
                    {
                        foreach (var item in CapLogic.SecondaryLogics)
                        {
                            try
                            {
                                await item.DoSecondaryLogic(CapLogic, territory.Value);
                            }
                            catch (Exception e)
                            {
                                AlliancePlugin.Log.Error($"Error on secondary logic loop of type {item.GetType()} { e.ToString()}");
                            }
                        }
                    }
                }
                //calculate ownership

            }
        }
    }
}
