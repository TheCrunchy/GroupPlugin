using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.KamikazeTerritories
{
    public enum TerritoryStatus
    {
        Offline,
        Neutral,
        Claimed,
        InitialSieging,
        AwaitingFinalSiege,
        FinalSieging,
        FailedSiegeCooldown,
        SuccessfulSiegeCooldown
    }
    public enum DataType
    {
        MyString,
        ClaimSettings,
        InitClaim,
        InitSiege,
        Sync,
        SingleSync,
        ColorGps,
        RemoveClaimSettings,
        SendClientSettings,
        RequestSettings,
        UpdateDetailInfo,
        UpdateBlockText,
        CreateTrigger,
        RemoveTrigger,
        SendGps,
        AddTerritory,
        SendAudio,
        UpdateEmissives,
        ResetTerritory,
        SyncBillboard,
        SyncParticle,
        UpdateProduction,
        SyncProduction,
        AddProduction,
        RemoveProduction,
        ManualTerritory,
        InitFinalSiege,
        ConsumeDelayTokens,
        UpdateSafeZoneAllies,
        DisableSafeZone,
        EnableSafeZone,
        ResetModData,
        PBMonitor
    }
    public enum NexusDataType
    {
        FactionJoin,
        FactionLeave,
        FactionRemove,
        FactionEdited,
        Chat,
        SettingsRequested,
        SettingsSent
    }

    public enum SyncType
    {
        DetailInfo,
        Timer,
        SiegeTimer,
        Emissive,
        AddProductionPerk,
        EnableProductionPerk,
        DisableProductionPerk,
        SyncProductionAttached,
        SyncProductionRunning

    }
    [ProtoContract]
    public class ObjectContainer
    {
        [ProtoMember(1)]
        public long entityId = 0;
        [ProtoMember(2)]
        public long playerId = 0;
        [ProtoMember(3)]
        public long claimBlockId = 0;
        [ProtoMember(4)]
        public ClaimBlockSettings settings = new ClaimBlockSettings();
        [ProtoMember(5)]
        public string stringData;
        [ProtoMember(6)]
        public Vector3D location = new Vector3D(0, 0, 0);
        [ProtoMember(7)]
        public string factionTag;
        [ProtoMember(8)]
        public ulong steamId;
        [ProtoMember(9)]
        public SyncType syncType;
        [ProtoMember(10)]
        public float floatingNum;
        [ProtoMember(11)]
        public long fromFaction;
        [ProtoMember(12)]
        public long toFaction;
    }

    [ProtoContract]
    public class CommsPackage
    {
        [ProtoMember(1)]
        public DataType Type;

        [ProtoMember(2)]
        public byte[] Data;


        public CommsPackage()
        {
            Type = DataType.MyString;
            Data = new byte[0];
        }

        public CommsPackage(DataType type, ObjectContainer objectContainer)
        {
            Type = type;
            Data = MyAPIGateway.Utilities.SerializeToBinary(objectContainer);
        }
    }

    [ProtoContract(IgnoreListHandling = true)]
    public class ClaimBlockSettings
    {
        public static readonly string SettingsVersion = "1.00";

        [ProtoMember(1)]
        public long _entityId;

        [ProtoMember(2)]
        public float _safeZoneSize;

        [ProtoMember(3)]
        public float _claimRadius;


        [ProtoMember(5)]
        public Vector3D _blockPos;


        public ClaimBlockSettings()
        {
            _entityId = 0;
            _safeZoneSize = 0;
            _claimRadius = 0;
            _blockPos = new Vector3D();
        }

        public ClaimBlockSettings(long blockId, Vector3D pos, IMyTerminalBlock block)
        {
            _entityId = blockId;
            _safeZoneSize = 1000f;
            _claimRadius = IsOnPlanet(pos);
            _blockPos = pos;
        }

        private float IsOnPlanet(Vector3D pos)
        {
            if (MyVisualScriptLogicProvider.IsPlanetNearby(pos))
                return 25000f;
            else
                return 50000f;
        }

        public enum GridChangeType
        {
            Power,
            Controller,
            Both
        }
    }
}