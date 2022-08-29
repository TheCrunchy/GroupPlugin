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

        //[ProtoMember(4)]
        //public bool Sync;

        [ProtoMember(5)]
        public Vector3D _blockPos;

        [ProtoMember(6)]
        public string _claimedFaction;

        [ProtoMember(7)]
        public long _safeZoneEntity;

        [ProtoMember(8)]
        public bool _enabled;

        [ProtoMember(9)]
        public bool _isClaimed;

        [ProtoMember(10)]
        public int _toClaimTimer;

        //[ProtoIgnore]
        //public Dictionary<long, PlayerData> _playersInside;

        [ProtoMember(11)]
        public string _unclaimName;

        [ProtoMember(12)]
        public string _claimZoneName;

        [ProtoMember(13)]
        public List<long> _safeZones;

        [ProtoMember(14)]
        public int _timer;

        [ProtoMember(15)]
        public bool _isClaiming;

        [ProtoMember(16)]
        public double _distanceToClaim;

        [ProtoMember(17)]
        public string _detailInfo;

        [ProtoMember(18)]
        public long _jdClaimingId;

        [ProtoMember(19)]
        public long _playerClaimingId;

        [ProtoMember(20)]
        public int _recoverTimer;

        [ProtoMember(22)]
        public int _consumeTokenTimer;

        [ProtoMember(23)]
        public int _toSiegeTimer;

        [ProtoMember(24)]
        public int _tokensToClaim;

        [ProtoMember(25)]
        public int _tokensToSiege;

        [ProtoMember(26)]
        public bool _isSieging;

        [ProtoMember(27)]
        public bool _isSieged;

        [ProtoMember(28)]
        public int _zoneDeactivationTimer;

        [ProtoMember(29)]
        public int _gpsUpdateDelay;

        [ProtoMember(30)]
        public double _distanceToSiege;

        [ProtoMember(31)]
        public bool _triggerInit;

        [ProtoMember(32)]
        public long _playerSiegingId;

        [ProtoMember(33)]
        public long _jdSiegingId;

        [ProtoMember(34)]
        public int _siegeTimer;

        [ProtoMember(35)]
        public long _discordRoleId;

        [ProtoMember(36)]
        public string _blockOwner;

        [ProtoMember(37)]
        public int _siegeNoficationFreq;

        [ProtoMember(38)]
        public long _factionId;

        [ProtoMember(44)]
        public bool _isSiegingFinal;

        [ProtoMember(45)]
        public bool _isSiegedFinal;

        [ProtoMember(46)]
        public string _version;

        [ProtoMember(47)]
        public bool _isCooling;

        [ProtoMember(48)]
        public int _toSiegeFinalTimer;

        [ProtoMember(49)]
        public int _tokensToSiegeFinal;

        [ProtoMember(50)]
        public int _tokensToDelaySiege;

        [ProtoMember(51)]
        public int _siegeDelayed;

        [ProtoMember(52)]
        public int _timeToDelay;

        [ProtoMember(53)]
        public int _siegeDelayAllowed;

        [ProtoMember(54)]
        public int _cooldownTime;

        [ProtoMember(55)]
        public string _siegedBy;

        [ProtoMember(56)]
        public bool _readyToSiege;

        [ProtoMember(57)]
        public int _timeframeToSiege;

        [ProtoMember(58)]
        public bool _centerToPlanet;

        [ProtoMember(59)]
        public Vector3D _planetCenter;

        [ProtoMember(60)]
        public bool _adminAllowSafeZoneAllies;

        [ProtoMember(61)]
        public bool _adminAllowTerritoryAllies;

        [ProtoMember(62)]
        public bool _allowSafeZoneAllies;

        [ProtoMember(63)]
        public bool _allowTerritoryAllies;

        [ProtoMember(64)]
        public bool _allowTools;

        [ProtoMember(65)]
        public bool _allowDrilling;

        [ProtoMember(66)]
        public bool _allowWelding;

        [ProtoMember(67)]
        public bool _allowGrinding;

        [ProtoMember(68)]
        public string _planetName;

        [ProtoMember(69)]
        public string _consumptionItem;

        [ProtoMember(70)]
        public int _consumptionAmt;

        [ProtoMember(71)]
        public bool _isSiegeCooling;

        [ProtoMember(72)]
        public int _siegeCoolingTime;

        [ProtoMember(73)]
        public TerritoryStatus _territoryStatus;


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