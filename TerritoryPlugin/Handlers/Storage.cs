using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Handlers
{
    public static class Storage
    {
        public static IStorageHandler StorageHandler { get; set; }

        public static void SetupStorage()
        {
            //if you want a database do shit to set the StorageHandler to something that implements IStorageHandler
            StorageHandler = new JsonStorageHandler();
        }
    }
}
