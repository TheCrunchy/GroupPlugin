using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin
{
    public class PrintQueueItem
    {
        public string name;
        public DateTime startTime;
        public DateTime endTime;
        public long ownerSteam;
        public long ownerIdentity;
        public string ownerName;
        public double x, y, z;

        public PrintQueueItem(String name, long steam, long identity, string ownerName, DateTime start, DateTime end, double x, double y, double z)
        {
            this.name = name;
            this.ownerSteam = steam;
            this.ownerIdentity = identity;
            this.ownerName = ownerName;
            this.startTime = start;
            this.endTime = end;
            this.x = x;
            this.z = z;
            this.y = y;
        }
        public MyGps GetGps()
        {

            MyGps gps = new MyGps
            {
                Coords = new VRageMath.Vector3D(x, y, z),
                Name = "Shipyard Position",
                DisplayName = "Shipyard Position",
                GPSColor = Color.OrangeRed,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, 180, 0),
                Description = "Shipyard Position",
            };
            gps.UpdateHash();


            return gps;

        }
    }
}
