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
            var num = 0;
            var flag = true;
            var matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            var color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                var str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
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
