using CrunchGroup.Handlers.Interfaces;

namespace CrunchGroup.Handlers
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
