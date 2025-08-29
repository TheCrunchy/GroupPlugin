using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Screens.Helpers;
using VRage.Game;
using VRageMath;

namespace CrunchGroup
{
   public static class GPSHelper
    {
        public static MyGps ScanChat(string input, string desc = null)
        {
            var matchCollection = Regex.Matches(
                input,
                @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):(#?[0-9A-Fa-f]{8})?:"
            );

            var color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                var str = match.Groups[1].Value;
                try
                {
                    double x = Math.Round(double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), 2);
                    double y = Math.Round(double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), 2);
                    double z = Math.Round(double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture), 2);

                    if (!string.IsNullOrEmpty(match.Groups[5].Value))
                        color = new ColorDefinitionRGBA(match.Groups[5].Value);

                    var gps = new MyGps()
                    {
                        Name = str,
                        Description = desc,
                        Coords = new Vector3D(x, y, z),
                        GPSColor = color,
                        ShowOnHud = false
                    };
                    gps.UpdateHash();
                    return gps;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }


        public static MyGps CreateGps(Vector3D Position, Color gpsColor, String Name, String Reason)
        {

            MyGps gps = new MyGps
            {
                Coords = Position,
                Name = Name,
                DisplayName = Name,
                GPSColor = gpsColor,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, 10, 0),
                Description = "Radar Hit \n" + Reason,
            };
            gps.UpdateHash();


            return gps;
        }
    }

}
