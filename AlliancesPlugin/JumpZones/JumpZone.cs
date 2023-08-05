﻿using System;
using System.Collections.Generic;
using VRageMath;

namespace AlliancesPlugin.JumpZones
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
        public string ExcludedEntryDrives = "exampleDrive1PairName,ExampleDrive2PairName";
        public string ExcludedExitDrives = "exampleDrive1PairName,ExampleDrive2PairName";
        public string Name = "Fred";
        public List<String> GetExcludedEntry()
        {
            List<String> Drives = new List<string>();
            if (!ExcludedEntryDrives.Equals(""))
            {
                if (ExcludedEntryDrives.Contains(","))
                {
                    String[] split = ExcludedEntryDrives.Split(',');
                    foreach (String s in split)
                    {
                        Drives.Add(s);
                    }
                    return Drives;
                }
                else
                {
                    Drives.Add(ExcludedEntryDrives);
                    return Drives;
                }
            }
            return null;
        }
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
