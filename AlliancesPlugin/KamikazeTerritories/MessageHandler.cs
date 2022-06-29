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
        public static void ReceiveTerritory(byte[] data)
        {
            AlliancePlugin.Log.Info("GOT A TERRITORY MESSAGE");
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                if (package == null) return;
                AlliancePlugin.Log.Info("GOT A TERRITORY MESSAGE 1");
                if (package.Type == DataType.AddTerritory)
                {
                    AlliancePlugin.Log.Info("ADD A TERRITORY");
                    var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (encasedData == null) return;
                    AlliancePlugin.Log.Info(encasedData.settings._claimRadius + " RADIUS");
                   



                    return;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
