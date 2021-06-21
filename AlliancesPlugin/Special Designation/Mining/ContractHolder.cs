using Sandbox.Game.Screens.Helpers;
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
    public class ContractHolder
    {
        public ulong steamId;
        public Guid Id = System.Guid.NewGuid();
        public MiningContract contract;
        public string DeliveryGPS;
        public int minedAmount;
        public int amountToMine;
        public void GenerateAmountToMine()
        {
            Random rnd = new Random();
            amountToMine = rnd.Next(contract.minimunAmount - 1, contract.maximumAmount + 1);
         
        }

        public int MiningRadius;
        public MyGps GetDeliveryLocation()
        {
            MyGps gps = ScanChat(DeliveryGPS);
            gps.Description = "Deliver " + amountToMine + " " + contract.subTypeId + " ore";
            return gps;
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
    }
}
