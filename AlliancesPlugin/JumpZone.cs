using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin
{
   public class JumpZone
    {
        public double x = 0;
        public double y = 0;
        public double z = 0;
        public int Radius = 25000;
        public bool AllowEntry = false;
        public bool AllowExit = false;
        public bool AllowExcludedExit = false;
        public bool AllowExcludedEntry = false;
        public string ExcludedExitDrives = "exampleDrive1PairName,ExampleDrive2PairName";
        public string Name = "Fred";

        public List<String> GetExcludedExit()
        {
            List<String> Drives = new List<string>();
            if (!ExcludedExitDrives.Equals(""))
            {
                if (ExcludedExitDrives.Contains(","))
                {
                    String[] split = ExcludedExitDrives.Split(',');
                    foreach (String s in split)
                    {
                        Drives.Add(s);
                    }
                    return Drives;
                }
                else
                {
                    Drives.Add(ExcludedExitDrives);
                    return Drives;
                }
            }
            return null;
        }
        public Vector3 GetPosition()
        {
            return new Vector3(x, y, z);
        }
    }
}
