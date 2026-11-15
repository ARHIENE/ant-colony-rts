using System;
using UnityEngine;

namespace AntColony.World
{
    // 저장 ID: 순서를 바꾸거나 재사용하지 않는다. 0은 기존 적/동물이다.
    public enum OrganicEnemyKind
    {
        None, WallRam, NeedleStalker, BloomMender, ClawHunter, BroodAlien,
        BreachBeast, AcidSpitter, HuntRider, SiegeRider, WallMelter, SiegeBrooder,
        BroodCarrier, HookMaw, Bulwark, BurrowLurker, CarrionReclaimer, VeilWeaver
    }

    public static class OrganicEnemyRules
    {
        public static bool Valid(OrganicEnemyKind kind) => kind > OrganicEnemyKind.None && kind <= OrganicEnemyKind.VeilWeaver;
        public static string Label(OrganicEnemyKind kind) => Labels[(int)kind];
        static readonly string[] Labels = { "기존 적", "충각수", "침사냥꾼", "개화 치유체", "발톱 사냥꾼", "군체 외계인",
            "돌파수", "산성 분사체", "사냥 기수", "공성 기수", "부식붙이", "공성 포낭수", "포낭 운반체", "갈고리 포식자", "갑각 수호체", "굴착 잠복체", "회수 생물", "안개 분비체" };
        public static bool Ranged(OrganicEnemyKind kind) => kind == OrganicEnemyKind.NeedleStalker || kind == OrganicEnemyKind.AcidSpitter
            || kind == OrganicEnemyKind.SiegeRider || kind == OrganicEnemyKind.SiegeBrooder || kind == OrganicEnemyKind.HookMaw;
        public static float Range(OrganicEnemyKind kind) => kind == OrganicEnemyKind.SiegeRider || kind == OrganicEnemyKind.SiegeBrooder ? 9
            : kind == OrganicEnemyKind.HookMaw ? 5 : Ranged(kind) ? 7 : 1.5f;
        public static float Width(OrganicEnemyKind kind) => kind == OrganicEnemyKind.WallRam || kind == OrganicEnemyKind.BreachBeast
            || kind == OrganicEnemyKind.SiegeRider || kind == OrganicEnemyKind.SiegeBrooder ? 2.1f : kind == OrganicEnemyKind.BurrowLurker ? 2.3f : 1.4f;
        public static float StructureMultiplier(OrganicEnemyKind kind) => kind == OrganicEnemyKind.WallRam ? 2 : kind == OrganicEnemyKind.BreachBeast ? 1.6f : 1;
        public static float Splash(OrganicEnemyKind kind) => kind == OrganicEnemyKind.SiegeBrooder ? 2 : kind == OrganicEnemyKind.SiegeRider ? 1.6f : kind == OrganicEnemyKind.AcidSpitter ? .8f : 0;
        public static OrganicEnemyKind Select(int wave, int slot, EnemyAim aim)
        {
            // 초기 세 웨이브는 기존 근접 전투 수치·목표 우선순위를 유지한다.
            if (wave <= 3) return slot % 2 == 0 ? OrganicEnemyKind.ClawHunter : OrganicEnemyKind.BroodAlien;
            if (aim == EnemyAim.Wall) return slot % 2 == 0 ? OrganicEnemyKind.WallRam : OrganicEnemyKind.WallMelter;
            if (aim == EnemyAim.Defense) return slot % 2 == 0 ? OrganicEnemyKind.SiegeBrooder : OrganicEnemyKind.SiegeRider;
            if (aim == EnemyAim.Building) return OrganicEnemyKind.BreachBeast;
            int available = wave < 6 ? 10 : wave < 8 ? 14 : 17;
            return (OrganicEnemyKind)(1 + (slot + wave * 3) % available);
        }
    }

    [Serializable]
    public sealed class OrganicCombatState
    {
        public int kind, phase, spawnCharges = 2;
        public float phaseLeft, abilityCooldown, healLock, mistLeft, burrowCooldown;
        public string target;
        public Vector3 aim, corpsePosition;
    }
}
