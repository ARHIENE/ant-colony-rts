using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Save
{
    // 저장 파일 스키마. JsonUtility가 다룰 수 있는 형태(구체 클래스 + List)만 쓴다.
    // 리플렉션 범용 직렬화를 쓰지 않으므로, 새 상태를 저장하려면 여기에 필드를 명시적으로 늘려야 한다.

    [Serializable]
    public class Vec3Dto
    {
        public float x, y, z;
        public Vec3Dto() { }
        public Vec3Dto(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
        public bool IsFinite() => !(float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z)
            || float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z));
    }

    [Serializable]
    public class OptionsDto
    {
        public int mapSize;
        public int difficulty;
        public int commanderDeath = 1;
        public int seed;
    }

    [Serializable]
    public class ColonyDto
    {
        public int food, soil, special;
        public int foodCapacity, soilCapacity, specialCapacity;
        public bool storageResearchApplied;
        public int antsFree, antsAssigned, antsReserved;
        public bool fishingUnlocked;
    }

    [Serializable]
    public class TraitsDto
    {
        public int personality;
        public int loyalty;
        public List<Units.CommanderTrait> values = new List<Units.CommanderTrait>();
        public List<Units.CommanderPassion> passions = new List<Units.CommanderPassion>();
        public List<string> loyaltyReasons = new List<string>();
    }

    [Serializable]
    public class CommanderDto
    {
        public int id;
        public string name;
        public int rank;
        public List<int> allowedRoles = new List<int>();
        public int role;
        public int troopCount;
        public float pendingDamage;
        public bool strikeArmed;
        public float strikeCooldown, stanceCooldown, stanceTime;
        public int level;
        public int xp;
        public Units.CommanderTalents talents;
        public TraitsDto traits = new TraitsDto();
        public Units.CommanderPersonalState personalState;
        public int workLevel;
        public float workProgress;
        public int labAttackLevel;
        public int labArmorLevel;
        public Vec3Dto position = new Vec3Dto();
        public bool activeInScene = true;

        // 소속. 0 = 본거지, 1 = 수송수단 탑승(transportIndex), 2 = 편입 거점 주둔(siteIndex), 3 = 포로(siteIndex)
        public int location;
        public int transportIndex = -1;
        public int siteIndex = -1;
    }

    [Serializable]
    public class PrisonerDto
    {
        public Units.CommanderTalents talents;
        public string name;
        public int rank;
        public List<int> roles = new List<int>();
        public TraitsDto traits = new TraitsDto();
        public int persuadeAttempts;
    }

    [Serializable]
    public class AffinityDto
    {
        public int firstCommanderId;
        public int secondCommanderId;
        public float value;
    }

    [Serializable]
    public class BuildingDto
    {
        public string key;
        public List<ResourceNodeDto> nodes = new List<ResourceNodeDto>();
        public string kind;              // BuildingKind 또는 "QueenChamber"/"DigSite"/"Storage" 같은 고정 건물 이름
        public int role;                 // 병영/연구소만 의미가 있다
        public bool runtimeBuilt;        // true면 불러올 때 템플릿에서 다시 짓는다
        public int sceneIndex;           // 씬 원본 건물 식별용(같은 kind+role 안에서의 순번)
        public Vec3Dto position = new Vec3Dto();
        public float rotationY;
        public float health;

        // 종류별 상태. 해당하지 않는 건물에서는 무시된다.
        public int barracksTier = 1;
        public float barracksUpgradeRemaining;
        public float towerCooldown;
        public float labResearchRemaining;
        public int labResearchCommanderId = -1;
        public bool labResearchAttack;
        public float queenProductionRemaining;
        public float queenFishingRemaining;
        public float scienceRemaining;
        public bool scienceAircraft;
        public bool scienceConstructing;
        public int scienceTier = 1;
        public int scientist = -1;
        public Buildings.AirshipYard.State airship;
        public List<int> patients = new List<int>();
        public Vec3Dto scienceSpawn = new Vec3Dto();
        public float scoutRemaining;
        public bool scoutDispatched;
        public int scoutDispatchedAnts;
        public int scoutSuccess;
        public int scoutFailure;
        public float prisonEscapeTimer;
        public int prisonRecruited, prisonExecuted, prisonEscaped;
        public List<PrisonerDto> prisoners = new List<PrisonerDto>();
        public int nurseryBirths;
        public List<AffinityDto> nurseryAffinity = new List<AffinityDto>();
        public bool digExpanded;
    }

    [Serializable]
    public class ResourceNodeDto
    {
        public string key;
        public bool exists = true;
        public int type;
        public Vec3Dto position = new Vec3Dto();
        public int index;
        public float amount;
        public float regrowTimer;
    }

    [Serializable]
    public class EnemyColonyDto
    {
        public Vec3Dto rootPosition = new Vec3Dto();
        public List<float> buildingHealth = new List<float>();
        public List<Vec3Dto> extraBuildings = new List<Vec3Dto>();
        public List<float> extraBuildingHealth = new List<float>();
        public List<ResourceNodeDto> nodes = new List<ResourceNodeDto>();
        public float waveTimer;
        public float economyTimer;
        public int waveIndex;
    }

    [Serializable]
    public class SiteDto
    {
        public bool rewardsClaimed;
        public float growthTimer;
        public EnemyColonyDto colony;
        public int index;
        public bool cleared;
        public int disposition;
        public float bossHp;
        public List<ResourceNodeDto> nodes = new List<ResourceNodeDto>();
        public List<float> colonyBuildingHealth = new List<float>();
        public List<Vec3Dto> colonyExtraBuildings = new List<Vec3Dto>();
        public List<float> guardHealth = new List<float>();
        public List<bool> guardActive = new List<bool>();
        public float settlementElapsed;
        public float defenseRemaining;
        public float defenseCaptureProgress;
    }

    [Serializable]
    public class RouteDto
    {
        public bool running;
        public int destinationIndex = -1;
        public float waitSeconds;
        public string status = "Off";
    }

    [Serializable]
    public class TransportDto
    {
        public bool blueprintCargo;
        public List<Units.EquipmentItem> equipmentCargo = new List<Units.EquipmentItem>();
        public bool aircraft;
        public int state;
        public float remaining;
        public int siteIndex = -1;
        public Vec3Dto position = new Vec3Dto();
        public Vec3Dto homePosition = new Vec3Dto();
        public int cargoFood, cargoSoil, cargoSpecial;
        public RouteDto route = new RouteDto();
    }

    [Serializable]
    public class WorldDto
    {
        public bool unlocked;
        public bool vehicleResearched;
        public bool aircraftResearched;
        public string notice = "";
        public List<SiteDto> sites = new List<SiteDto>();
        public List<TransportDto> transports = new List<TransportDto>();
        public EnemyColonyDto homeColony = new EnemyColonyDto();
        public bool hadHomeColony;
    }

    [Serializable]
    public class CameraDto
    {
        public float yaw;
        public int viewedSite = -1;
        public Vec3Dto focus = new Vec3Dto();
        public float orthoSize = 18f;
    }

    [Serializable]
    public class DiscoveryDto
    {
        public string category;
        public string key;
        public string title;
        public string body;
    }

    [Serializable]
    public class SaveFileV1
    {
        public List<ResourceNodeDto> nodes = new List<ResourceNodeDto>();
        public List<MonsterDto> monsters = new List<MonsterDto>();
        public float upkeepTimer, incursionTimer;
        public bool loopCompleted, bossDefeated, defeated;
        public string randomState;
        public const int CurrentVersion = 3;

        public int version = CurrentVersion;
        public string gameId = "AntColony";
        public string label = "";
        public string savedAtUtc = "";
        public float playSeconds;
        public float gameSeconds;
        public List<Units.EquipmentItem> equipmentInventory = new List<Units.EquipmentItem>();
        public int upkeepFailures;
        public Core.CampaignResearch.State campaign = new Core.CampaignResearch.State();
        public string unityVersion = "";

        public OptionsDto options = new OptionsDto();
        public ColonyDto colony = new ColonyDto();
        public List<CommanderDto> commanders = new List<CommanderDto>();
        public List<BuildingDto> buildings = new List<BuildingDto>();
        public WorldDto world = new WorldDto();
        public CameraDto camera = new CameraDto();
        public List<DiscoveryDto> discoveries = new List<DiscoveryDto>();

        // 저장 시점에 복원할 수 없다고 미리 밝힌 항목들(진행 중인 침공 부대 등).
        public List<string> notRestored = new List<string>();
    }

    [Serializable]
    public class MonsterDto
    {
        public Units.CommanderTalents talents;
        public string key;
        public float health;
        public Vec3Dto position = new Vec3Dto();
        public TraitsDto traits;
    }
}
