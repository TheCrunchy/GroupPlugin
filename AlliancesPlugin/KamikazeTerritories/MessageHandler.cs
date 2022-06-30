using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KamikazeTerritories
{
    public static class MessageHandler
    {
        public static List<KamikazeTerritory> Territories = new List<KamikazeTerritory>();

        public static void ReceiveTerritory(byte[] data)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                if (package == null) return;
              
                if (package.Type == DataType.AddTerritory)
                {
           
                    var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (encasedData == null) return;

                    if (!Territories.Any(x => x.EntityId == encasedData.entityId))
                    {
                        Territories.Add(new KamikazeTerritory()
                        {
                            EntityId = encasedData.entityId,
                            Radius = encasedData.settings._claimRadius,
                            Position = encasedData.settings._blockPos
                        });
                    }
                    return;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
