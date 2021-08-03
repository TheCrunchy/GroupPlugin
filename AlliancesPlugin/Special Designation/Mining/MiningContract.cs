using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Special_Designation
{
    public class MiningContract
    {
        public ulong steamId;

        public long contractPrice = 0;

      //  public int reputation = 0;

        public string OreSubType;

        public int minedAmount = 0;

        public int amountToMine = 0;
        public void DoPlayerGps(long identityId)
        {
            MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
            if (ScanChat(DeliveryLocation) != null)
            {
                MyGps gpsRef = ScanChat(DeliveryLocation);
                gpsRef.GPSColor = Color.DarkOrange;
                gpsRef.ShowOnHud = true;
               gpsRef.Description = "Deliver " + amountToMine + " " + OreSubType + " Ore. !mc info";
              gpsRef.DisplayName = "Ore Delivery Location.";
                gpsRef.Name = "Ore Delivery Location.";
               gpsRef.DiscardAt = new TimeSpan(600);
                gpscol.SendAddGps(identityId, ref gpsRef);
            }
        }
        public Vector3 getCoords()
        {
            if (ScanChat(DeliveryLocation) != null)
            {
                MyGps gpsRef = ScanChat(DeliveryLocation);
                gpsRef.GPSColor = Color.DarkOrange;
                gpsRef.ShowOnHud = true;
                gpsRef.Description = "Deliver " + amountToMine + " " + OreSubType + " Ore. !mc info";
                gpsRef.DisplayName = "Ore Delivery Location.";
                gpsRef.Name = "Ore Delivery Location.";
                gpsRef.DiscardAt = new TimeSpan(600);


                return gpsRef.Coords;
            }

            return new Vector3(0,0,0);
        }
        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
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
                MyGps gps = new MyGps()
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

        public void GenerateAmountToMine(int min, int max)
        {
            Random rnd = new Random();
            amountToMine = rnd.Next(min - 1, max + 1);
         
        }

        public Boolean AddToContractAmount(int amount)
        {
            minedAmount += amount;
            if (minedAmount >= amountToMine)
            {
                return true;
            }
            return false;
        }

        public String DeliveryLocation;
       
    }
}
