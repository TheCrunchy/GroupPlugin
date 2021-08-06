using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    using Sandbox.Game.Screens.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VRageMath;


        public class HaulingContract
        {
            public Guid ContractId = System.Guid.NewGuid();
            public List<ContractItems> items;
            public double GpsX;
            public double GpsY;
            public double GpsZ;
            public List<ContractItems> getItemsInContract()
            {
                return this.items;
            }
            public Guid GetContractId()
            {
                return this.ContractId;
            }
            public MyGps GetDeliveryLocation()
            {
                MyGps gps = new MyGps
                {
                    Coords = new Vector3D(GpsX, GpsY, GpsZ),
                    Name = "Delivery Location, bring hauling vehicle within 300m",
                    DisplayName = "Delivery Location, bring hauling vehicle within 300m",
                   Description = HaulingCore.MakeContractDetails(items).ToString(),
                    GPSColor = Color.Orange,
                    IsContainerGPS = true,
                    ShowOnHud = true,
                    DiscardAt = new TimeSpan(50000)
                };
                gps.UpdateHash();
                return gps;
            }
            public void SetDeliveryLocation(double x, double y, double z)
            {
                this.GpsX = x;
                this.GpsY = y;
                this.GpsZ = z;
            }
        }
    

}
