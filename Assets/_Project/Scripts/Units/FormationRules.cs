using System;
using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 저장 번호: 평진=0, 삼국지풍=1~7, 현대식=8~13. 끝에만 추가한다.
    public enum Formation { Plain, FishScale, Arrow, Circle, Swift, Geese, Crane, Snake, Line, Column, Wedge, Dispersed, Cover, Focus }

    [Serializable]
    public sealed class FormationState
    {
        public Formation selected;
        public List<Formation> affinities = new List<Formation>();
        public int awarded;
        public Vector3 forward;
        public string hitTarget;
        public int hits;
        public float sinceHit;
        public bool Valid => FormationRules.Defined(selected) && affinities != null && affinities.Count <= 3
            && new HashSet<Formation>(affinities).Count == affinities.Count
            && affinities.TrueForAll(FormationRules.Traditional) && awarded == affinities.Count && awarded <= 3
            && (hits == 0 || !string.IsNullOrEmpty(hitTarget)) && hits >= 0 && hits <= 4 && Finite(sinceHit) && sinceHit >= 0 && (hits == 0 || sinceHit < FormationRules.FocusTimeout)
            && Finite(forward.x) && Finite(forward.y) && Finite(forward.z);
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        public void ResetHits() { hits = 0; hitTarget = null; sinceHit = 0; }
        public void Tick(float seconds)
        {
            if (hits == 0) return;
            sinceHit += seconds;
            if (sinceHit >= FormationRules.FocusTimeout) ResetHits();
        }
        public void Target(string key) { if (hitTarget != key) ResetHits(); }
        public void Hit(string key)
        {
            Target(key); hitTarget = key; sinceHit = 0; hits = Mathf.Min(4, hits + 1);
        }
    }

    public readonly struct FormationModifiers
    {
        public readonly float Attack, Armor, Move, Range, AttackSpeed, WorldMove, RangedReduction, AreaReduction, RearIncrease;
        public FormationModifiers(float attack = 0, float armor = 0, float move = 0, float range = 0,
            float attackSpeed = 0, float worldMove = 0, float rangedReduction = 0, float areaReduction = 0, float rearIncrease = 0)
        {
            Attack = attack; Armor = armor; Move = move; Range = range; AttackSpeed = attackSpeed;
            WorldMove = worldMove; RangedReduction = rangedReduction; AreaReduction = areaReduction; RearIncrease = rearIncrease;
        }
    }

    public static class FormationRules
    {
        public const float FocusTimeout = 3;
        public static readonly Formation[] All = (Formation[])Enum.GetValues(typeof(Formation));
        public static bool Defined(Formation f) => f >= Formation.Plain && f <= Formation.Focus;
        public static bool Traditional(Formation f) => f >= Formation.FishScale && f <= Formation.Snake;
        public static string Name(Formation f) => f switch {
            Formation.Plain => "평진", Formation.FishScale => "어린진", Formation.Arrow => "봉시진", Formation.Circle => "방원진",
            Formation.Swift => "추행진", Formation.Geese => "안행진", Formation.Crane => "학익진", Formation.Snake => "장사진",
            Formation.Line => "횡대", Formation.Column => "종대", Formation.Wedge => "쐐기", Formation.Dispersed => "산개",
            Formation.Cover => "엄호", Formation.Focus => "화력집중", _ => "알 수 없는 진형" };
        public static float Favorable(float value, bool legion) => value > 0 && legion ? value * 1.5f : value;
        public static int CommandLimit(int command, int equipment, float health, bool legion) =>
            Mathf.Max(1, Mathf.FloorToInt((10 + 2 * command + equipment) * health * (legion ? 1.5f : 1)));
        // D04는 초과 병력을 감쇠/삭제하지 않는다. 신규 편성/증원만 한도로 제한한다.
        public static bool CanAllocate(int troops, int limit) => troops >= 0 && troops <= limit;
        public static bool Front(Vector3 forward, Vector3 delta) => Angle(forward, delta) <= 45f;
        public static bool Rear(Vector3 forward, Vector3 delta) => Angle(forward, delta) > 90f;
        private static float Angle(Vector3 forward, Vector3 delta)
        {
            forward.y = delta.y = 0;
            return forward.sqrMagnitude < .000001f || delta.sqrMagnitude < .000001f ? 90 : Vector3.Angle(forward, delta);
        }
        public static FormationModifiers Modifiers(Formation f, bool legion = false, bool ranged = false,
            bool forestOrMountain = false, bool front = false, bool withdrawing = false, int hits = 0)
        {
            var m = f switch {
                Formation.FishScale => new FormationModifiers(.15f, .15f, -.1f),
                Formation.Arrow => new FormationModifiers(.2f, -.1f, .15f),
                Formation.Circle => new FormationModifiers(armor: .2f, move: -.1f),
                Formation.Swift => new FormationModifiers(-.1f, -.1f, .25f),
                Formation.Geese => new FormationModifiers(ranged ? .15f : 0, -.1f, range: ranged ? .15f : 0),
                Formation.Crane => new FormationModifiers(-.1f, .15f, rangedReduction: .15f),
                Formation.Snake => new FormationModifiers(forestOrMountain ? .2f : -.1f, forestOrMountain ? .2f : -.1f),
                Formation.Line => new FormationModifiers(.15f, rearIncrease: .15f),
                Formation.Column => new FormationModifiers(-.1f, move: .2f, worldMove: .2f),
                Formation.Wedge => new FormationModifiers(front ? .3f : 0, -.1f),
                Formation.Dispersed => new FormationModifiers(-.1f, areaReduction: .2f),
                Formation.Cover => new FormationModifiers(-.15f, .15f, rangedReduction: withdrawing ? .3f : 0),
                Formation.Focus => new FormationModifiers(Mathf.Clamp(hits, 0, 4) * .05f, attackSpeed: -.1f),
                _ => new FormationModifiers() };
            return new FormationModifiers(Favorable(m.Attack, legion), Favorable(m.Armor, legion), Favorable(m.Move, legion),
                Favorable(m.Range, legion), Favorable(m.AttackSpeed, legion), Favorable(m.WorldMove, legion),
                Favorable(m.RangedReduction, legion), Favorable(m.AreaReduction, legion), m.RearIncrease);
        }
        public static float ReceivedMultiplier(float increase, float reduction) => 1 + increase - Mathf.Min(.75f, reduction);
        // 분노/탈진 제공자는 서로 배타적인 상태 배율을 전달한다(역린 구현과 공유).
        public static float Stat(float basis, float flatBonus, float formationBonus, float health = 1, float state = 1) =>
            (basis + flatBonus + basis * formationBonus) * health * state;
    }
}
