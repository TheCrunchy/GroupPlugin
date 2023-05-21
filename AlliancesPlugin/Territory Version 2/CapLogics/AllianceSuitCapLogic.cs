﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{

    public class AllianceSuitCapLogic : ICapLogic
    {
        public AllianceSuitCapLogic(string Test1)
        {
            this.Test1 = Test1;
        }

        public string Test1 = "Test string";
        public Task<Tuple<bool, object>> ProcessCap()
        {
            if (CanLoop())
            {
                AlliancePlugin.Log.Info(Test1);
                //do capture logic for suits in alliances

                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            }

            return Task.FromResult(Tuple.Create<bool, Object>(false, null));
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public IPointOwner PointOwner { get; set; }
    }

}
