using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KamikazeTerritories
{
    public static class MessageHandler
    {
        public static List<KamikazeTerritory> Territories = new List<KamikazeTerritory>();


        public static void SaveFile()
        {
            AlliancePlugin.utils.WriteToJsonFile<List<KamikazeTerritory>>($"{AlliancePlugin.path}\\Kamikaze.json", Territories);
        }

        public static void LoadFile()
        {
            if (File.Exists($"{AlliancePlugin.path}\\Kamikaze.json"))
            {
                Territories = AlliancePlugin.utils.ReadFromJsonFile<List<KamikazeTerritory>>($"{AlliancePlugin.path}\\Kamikaze.json");
            }
        }

        public static void ReceiveTerritory(byte[] data)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                //  AlliancePlugin.Log.Info("got territory");
                if (package == null) return;

                //  AlliancePlugin.Log.Info("package isnt null");
                //  AlliancePlugin.Log.Info(package.Type.ToString());
                if (package.Type == DataType.AddTerritory)
                {
                    //   AlliancePlugin.Log.Info("is territory");
                    var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (encasedData == null) return;

                    //  AlliancePlugin.Log.Info("got past data check");
                    if (!Territories.Any(x => x.EntityId == encasedData.entityId))
                    {
                        //  AlliancePlugin.Log.Info("no territory with that entity id");
                        Territories.Add(new KamikazeTerritory()
                        {
                            EntityId = encasedData.entityId,
                            Radius = encasedData.settings._claimRadius,
                            Position = encasedData.settings._blockPos
                        });
                        SaveFile();
                    }
                    return;
                }
                if (package.Type == DataType.ResetTerritory)
                {
                    //   AlliancePlugin.Log.Info("is territory");
                    var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (encasedData == null) return;

                    //  AlliancePlugin.Log.Info("got past data check");
                    if (Territories.Any(x => x.EntityId == encasedData.entityId))
                    {
                        //  AlliancePlugin.Log.Info("no territory with that entity id");
                        var index = Territories.FindIndex(x => x.EntityId == encasedData.entityId);
                        Territories.RemoveAt(index);
                        SaveFile();
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
