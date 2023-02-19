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
    }
}
