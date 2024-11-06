using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage;
using VRageMath;

namespace CrunchGroup.NexusStuff.V3
{
    public class NexusGlobalAPI
    {
        private const long MessageId = 20240902;

        public List<Cluster> Clusters { get; private set; }

        public List<Server> Servers { get; private set; }

        public List<Sector> Sectors { get; private set; }

        public byte CurrentServerID { get; private set; }

        public byte CurrentClusterID { get; private set; }

        /// <summary>
        /// True when the API has connected to the Nexus Plugin
        /// Will always be false on clients.
        /// </summary>
        public bool Enabled { get; private set; }
        private Action onEnabled;

        /// <summary>
        /// Call Unload() when done to unregister message handlers. 
        /// Check Enabled to see if the API is communicating with Nexus.
        /// Can only be used on the server, will not work on the client.
        /// </summary>
        /// <param name="onEnabled">Called once the API has connected to Nexus.</param>
        public NexusGlobalAPI(Action onEnabled = null)
        {
            this.onEnabled = onEnabled;

            MyAPIGateway.Utilities.RegisterMessageHandler(MessageId, ReceiveData);
        }

        /// <summary>
        /// Call this method to cleanup once you are done with the Nexus API.
        /// </summary>
        public void Unload()
        {
            Enabled = false;
            isPlayerOnline = null;
            getTargetServer = null;
            getTargetSector = null;
            isServerOnline = null;
            sendModMsgToServer = null;
            sendModMsgToAllServers = null;
            getAllOnlineServers = null;
            getAllOnlinePlayers = null;
            sendChatToDiscord = null;
            onEnabled = null;
            MyAPIGateway.Utilities.UnregisterMessageHandler(MessageId, ReceiveData);
        }

        private void ReceiveData(object obj)
        {
            try
            {
                var data = (MyTuple<byte[], Func<int, Func<object, object>>>)obj;
                var getMethod = data.Item2;

                isPlayerOnline = getMethod((int)Methods.IsPlayerOnline);
                getTargetServer = getMethod((int)Methods.GetTargetServer);
                getTargetSector = getMethod((int)Methods.GetTargetSector);
                isServerOnline = getMethod((int)Methods.IsServerOnline);
                sendModMsgToServer = getMethod((int)Methods.SendModMsgToServer);
                sendModMsgToAllServers = getMethod((int)Methods.SendModMsgToAllServers);
                getAllOnlineServers = getMethod((int)Methods.GetAllOnlineServers);
                getAllOnlinePlayers = getMethod((int)Methods.GetAllOnlinePlayers);
                sendChatToDiscord = getMethod((int)Methods.SendChatToDiscord);

                ServerDataMsgAPI serverData = MyAPIGateway.Utilities.SerializeFromBinary<ServerDataMsgAPI>(data.Item1);
                Clusters = serverData.clusters;
                Servers = serverData.servers;
                Sectors = serverData.sectors;
                CurrentServerID = serverData.thisServerID;
                CurrentClusterID = serverData.thisClusterID;

                Enabled = true;
                onEnabled?.Invoke();
            }
            catch (Exception e)
            {
                Core.Log.Error(e.ToString());
            }
        }

        /// <summary>
        /// Checks to see if a specific player is online
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public bool IsPlayerOnline(ulong playerId)
        {
            return Enabled && (bool)isPlayerOnline(playerId);
        }
        private Func<object, object> isPlayerOnline;


        /// <summary>
        /// Gets target serverID of a position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public byte GetTargetServer(Vector3D position)
        {
            if (Enabled)
                return (byte)getTargetServer(position);
            return byte.MinValue;
        }
        private Func<object, object> getTargetServer;

        /// <summary>
        /// Gets the target sectorID of a position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int GetTargetSector(Vector3D position)
        {
            if (Enabled)
                return (int)getTargetSector(position);
            return int.MinValue;
        }
        private Func<object, object> getTargetSector;

        /// <summary>
        /// Checks to see if the Target ServerID is online
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public bool IsServerOnline(byte serverID)
        {
            return Enabled && (bool)isServerOnline(serverID);
        }
        private Func<object, object> isServerOnline;


        /// <summary>
        /// Send a specific Mod Message to a Target Nexus server
        /// </summary>
        /// <param name="data"></param>
        /// <param name="modChannelID"></param>
        /// <param name="targetServer"></param>
        /// <returns></returns>
        public bool SendModMsgToServer(byte[] data, long modChannelID, byte targetServer)
        {
            return Enabled && (bool)sendModMsgToServer(MyTuple.Create(data, modChannelID, targetServer));
        }
        private Func<object, object> sendModMsgToServer;

        /// <summary>
        /// Send a specific Mod Message to a ALL ONLINE nexus servers. =
        /// </summary>
        /// <param name="data"></param>
        /// <param name="modChannelID"></param>
        /// <returns></returns>
        public bool SendModMsgToAllServers(byte[] data, long modChannelID)
        {
            return Enabled && (bool)sendModMsgToAllServers(MyTuple.Create(data, modChannelID));
        }
        private Func<object, object> sendModMsgToAllServers;

        /// <summary>
        /// Gets a list of all online nexus servers tied to the controller
        /// </summary>
        /// <returns></returns>
        public List<byte> GetAllOnlineServers()
        {
            if (Enabled)
                return (List<byte>)getAllOnlineServers(null);
            return null;
        }
        private Func<object, object> getAllOnlineServers;

        /// <summary>
        /// Gets a list of all online players across the nexus controller
        /// </summary>
        /// <returns></returns>
        public List<ulong> GetAllOnlinePlayers()
        {
            if (Enabled)
                return (List<ulong>)getAllOnlinePlayers(null);
            return null;
        }
        private Func<object, object> getAllOnlinePlayers;

        /// <summary>
        /// Sends a message directly to discord.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="discordChannelID"></param>
        /// <param name="isEmbed"></param>
        /// <param name="EmbedTitle"></param>
        /// <param name="EmbedFooter"></param>
        public void SendChatToDiscord(string message, ulong discordChannelID, bool isEmbed = false, string EmbedTitle = "", string EmbedFooter = "")
        {
            if (Enabled)
                sendChatToDiscord(MyTuple.Create(message, discordChannelID, isEmbed, EmbedTitle, EmbedFooter));
        }
        private Func<object, object> sendChatToDiscord;

        private enum Methods
        {
            None = 0,
            IsPlayerOnline,
            GetTargetServer,
            GetTargetSector,
            IsServerOnline,
            SendModMsgToServer,
            SendModMsgToAllServers,
            GetAllOnlineServers,
            GetAllOnlinePlayers,
            SendChatToDiscord
        }

        #region ServerData
        [ProtoContract]
        private class ServerDataMsgAPI
        {
            /* ServerDataMsgAPI Requests all region data from the Nexus Plugin. This data only changes on restart.
             * 
             * 
             */

            [ProtoMember(10)]
            public List<Cluster> clusters;

            [ProtoMember(20)]
            public List<Server> servers;

            [ProtoMember(30)]
            public List<Sector> sectors;

            [ProtoMember(40)]
            public byte thisServerID;

            [ProtoMember(50)]
            public byte thisClusterID;


        }

        [ProtoContract]
        public class Cluster
        {
            public Cluster() { }

            [ProtoMember(5)] public byte ClusterID { get; set; }
            [ProtoMember(10), DefaultValue("New Cluster")] public string ClusterName { get; set; } = "New Cluster";
            [ProtoMember(15), DefaultValue("Cluster Description")] public string ClusterDescription { get; set; } = "Cluster Description";
            [ProtoMember(20)] public byte LobbyServerID { get; set; }
            [ProtoMember(25), DefaultValue((ushort)10)] public ushort GeneralSectorID { get; set; } = 10;

        }

        [ProtoContract]
        public class Sector
        {
            [ProtoMember(1), DefaultValue("NewSector")]
            public string SectorName { get; set; } = "NewSector";

            [ProtoMember(2), DefaultValue("NewSectorDescription")]
            public string SectorDescription { get; set; } = "NewSectorDescription";

            [ProtoMember(3)]
            public byte OnServerID { get; set; }

            [ProtoMember(4), DefaultValue(SectorShape.Sphere)]
            public SectorShape SectorShape { get; set; } = SectorShape.Sphere;


            [ProtoMember(5)]
            public double X { get; set; }

            [ProtoMember(6)]
            public double Y { get; set; }

            [ProtoMember(7)]
            public double Z { get; set; }


            [ProtoMember(8)]
            public double DX { get; set; }

            [ProtoMember(9)]
            public double DY { get; set; }

            [ProtoMember(10)]
            public double DZ { get; set; }



            [ProtoMember(11)]
            public float RadiusKM { get; set; }

            [ProtoMember(12)]
            public float RingRadiusKM { get; set; }

            [ProtoMember(13)]
            public ushort SectorID { get; set; }

            [ProtoMember(14)]
            public string SectorBoundaryScript { get; set; }

            [ProtoMember(16)]
            public bool HiddenSector { get; set; }

            [ProtoMember(17), DefaultValue(true)]
            public bool EnableSectorInfoProvider { get; set; } = true;

            [ProtoMember(18)]
            public SectorBorderTexture BorderTexture { get; set; }

            [ProtoMember(19, IsRequired = true)]
            public Color BorderColor { get; set; } = Color.White;

            public Sector()
            {

            }
        }

        [ProtoContract]
        public class Server
        {
            public Server() { }

            [ProtoMember(1), DefaultValue("NewServer")] public string Name { get; set; } = "NewServer";
            [ProtoMember(2), DefaultValue((byte)1)] public byte ServerID { get; set; } = 1;
            [ProtoMember(3)] public byte OnClusterID { get; set; }
            [ProtoMember(4), DefaultValue("127.0.0.1")] public string GameIPAddress { get; set; } = "127.0.0.1";
            [ProtoMember(5), DefaultValue((ushort)27018)] public ushort GamePort { get; set; } = 27018;
            [ProtoMember(6), DefaultValue(ServerType.SyncedSectored)] public ServerType SectorType { get; set; } = ServerType.SyncedSectored;
            [ProtoMember(7)] public ushort SelectedConfigGroup { get; set; }
            [ProtoMember(8), DefaultValue("XYZ")] public string ServerAbbreviation { get; set; } = "XYZ";
            [ProtoMember(9), DefaultValue("127.0.0.1")] public string NexusBoxIP { get; set; } = "127.0.0.1";
            [ProtoMember(10), DefaultValue((ushort)5000)] public ushort DirectCommsPort { get; set; } = 5000;
        }

        public enum SectorShape
        {
            Sphere, //Center and radius
            Cuboid, //Two points opposite corners define space
            Torus //Define Center, Radius, Ring Radius, Direction Vector (Perpendicular to Radius)
        }

        public enum ServerType
        {
            //Default Synced Sectored instance
            SyncedSectored,

            //Synced Non-Sectored Instance (Similar to existing plugins. Pure sync, no boundaries)
            SyncedNonSectored,

            //Non-Synced, Non-Sectored Instance. Basically no nexus features. Pure event server type. Still
            //accessible via lobby spawn script system.
            NonSyncedNonSectored,

            //Starts the server synced, then disables sync, be-coming a nonsynced, nonsectored instance
            StartSyncedNonSectored
        }

        public enum SectorBorderTexture
        {
            Circle,
            Cross,
            Hex,
        }


        [ProtoContract]
        //This is the datamsg you get back on your custom nexusmod channel you registered with keen
        public class ModAPIMsg
        {
            [ProtoMember(10)]
            public byte fromServerID;

            [ProtoMember(20)]
            public byte toServerID;

            [ProtoMember(25)]
            public long targetModMessageID;

            [ProtoMember(30)]
            public byte[] msgData;
        }
        #endregion
    }
}