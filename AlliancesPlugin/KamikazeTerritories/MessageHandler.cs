using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.KamikazeTerritories
{
    public static class MessageHandler
    {
        public static List<KamikazeTerritory> Territories = new List<KamikazeTerritory>();


        public static void SaveFile()
        {

            AlliancePlugin.utils.WriteToJsonFile<List<KamikazeTerritory>>($"{AlliancePlugin.path}\\Kamikaze.json", Territories);
        }
        public static Random rand = new Random();
        public static long LongRandom(long min = 1, long max = 4354436)
        {
            long result = rand.Next((Int32)(min >> 32), (Int32)(max >> 32));
            result = (result << 32);
            result = result | (long)rand.Next((Int32)min, (Int32)max);
            return result;
        }
        public static void LoadFile()
        {
            if (File.Exists($"{AlliancePlugin.path}\\Kamikaze.json"))
            {
                Territories = AlliancePlugin.utils.ReadFromJsonFile<List<KamikazeTerritory>>($"{AlliancePlugin.path}\\Kamikaze.json");
            }
        }

        public static void AddOtherTerritory(Vector3D Position, int Radius, string Name)
        {
            LoadFile();
            Territories.Add(new KamikazeTerritory()
            {
                EntityId = LongRandom(),
                Position = Position,
                Radius = Radius,
                Name = Name
            });
            SaveFile();
        }

        public static void ReceiveTerritory(byte[] data)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                //  AlliancePlugin.Log.Info("got territory");
                if (package == null) return;

                switch (package.Type)
                {
                    //  AlliancePlugin.Log.Info("package isnt null");
                    //  AlliancePlugin.Log.Info(package.Type.ToString());
                    case DataType.InitSiege:
                    {
                        //   AlliancePlugin.Log.Info("is territory");
                        var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                        if (encasedData == null) return;

                        //  AlliancePlugin.Log.Info("got past data check");
                        if (Territories.Any(x => x.Position.Equals(encasedData.location))) return;
                        //  AlliancePlugin.Log.Info("no territory with that entity id");
                        Territories.Add(new KamikazeTerritory()
                        {
                            EntityId = encasedData.claimBlockId,
                            Radius = encasedData.settings._claimRadius,
                            Position = encasedData.settings._blockPos,
                            Name = encasedData.factionTag
                        });
                        SaveFile();
                        return;
                    }
                    case DataType.AddTerritory:
                    {
                        LoadFile();
                        //   AlliancePlugin.Log.Info("is territory");
                        var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                        if (encasedData == null) return;

                        //  AlliancePlugin.Log.Info("got past data check");
                        if (Territories.All(x => !x.Position.Equals(encasedData.location)))
                        {
                            //  AlliancePlugin.Log.Info("no territory with that entity id");
                            Territories.Add(new KamikazeTerritory()
                            {
                                EntityId = encasedData.claimBlockId,
                                Radius = encasedData.settings._claimRadius,
                                Position = encasedData.settings._blockPos
                            });
                            SaveFile();
                        }
                        else
                        {
                            Territories.RemoveAt(Territories.FindIndex(x => x.Position.Equals(encasedData.location)));
                            Territories.Add(new KamikazeTerritory()
                            {
                                EntityId = encasedData.claimBlockId,
                                Radius = encasedData.settings._claimRadius,
                                Position = encasedData.settings._blockPos,
                                settings = encasedData.settings
                            });
                            SaveFile();
                        }
                        return;
                    }
                }

                if (package.Type != DataType.ResetTerritory) return;
                {
                    //   AlliancePlugin.Log.Info("is territory");
                    var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (encasedData == null) return;

                    //  AlliancePlugin.Log.Info("got past data check");
                    if (Territories.All(x => !x.Position.Equals(encasedData.location))) return;
                    {
                        //  AlliancePlugin.Log.Info("no territory with that entity id");
                        var index = Territories.FindIndex(x => x.Position == encasedData.location);
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
