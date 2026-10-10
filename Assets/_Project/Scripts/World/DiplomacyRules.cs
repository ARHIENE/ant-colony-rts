using System;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public enum LeaderAgenda { Trader, Conqueror, Recluse, Scientist, Warrior, Pacifist }
    public enum TreatyKind { Trade, NonAggression, Alliance }
    public static class DiplomacyRules
    {
        public const float Month = 300, Year = 12 * Month, PeaceDelay = 600, CaravanSeconds = 90;
        public const int BlueprintPrice = 150, SurprisePenalty = -30, MonthlyTradeAmount = 10;
        public const float OfferChance = .25f, TraderOfferChance = .5f, HostileWarChance = .3f, ConquerorWarChance = .2f, AirshipRaidChance = .2f;
        public static readonly int[] EquipmentValues = { 40, 80, 160, 320 };
        public static readonly int[] TreatyValues = { 100, 150, 300 };
        public static readonly string[] AgendaNames = { "상인", "정복자", "은둔자", "과학자", "무인", "평화주의" };
        public static double AcceptanceMultiplier(int affinity) => 1.2 - Mathf.Clamp(affinity, -100, 100) / 250d;
        public static float PriceMultiplier(int affinity) => 1 - Mathf.Clamp(affinity, -100, 100) * .003f;
        public static bool Accepts(double received, double given, int affinity) => received >= given * AcceptanceMultiplier(affinity);
    }

    [Serializable] public sealed class Civilization
    {
        public string id, name, leader;
        public Color color;
        public LeaderAgenda agenda, hiddenAgenda;
        public bool contacted, hiddenRevealed, war, rebel, extinct;
        public int affinity, playerScore, enemyScore;
        public int threatCred; // 이 세력이 보는 우리 위협 신뢰(-100~100). 최후통첩 실행 시 상승, 철회 시 하락, 매달 0으로 서서히 회복.
        public float talksCooldown; // 협상 결렬 뒤 재협상 가능 시각(조건을 크게 개선하면 더 일찍 가능).
        public float warStarted, founded, nextRaid, nextOffer, offerExpires;
        public TreatyKind offeredTreaty;
        public float[] treaties = new float[3];
        public List<string> reasons = new List<string>();
        public int[] resources = { 300, 300, 100 };
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public List<Prisoner> prisoners = new List<Prisoner>(); // 우리가 돌려보내 귀환 도착한 이 세력 장수(전력 반영은 기획 미정). 필드 이름은 저장 호환용
        public List<Prisoner> rebels = new List<Prisoner>();
        public bool HasTreaty(TreatyKind kind, float now) => !war && treaties[(int)kind] > now;
        public void ChangeAffinity(int delta, string reason)
        {
            affinity = Mathf.Clamp(affinity + delta, -100, 100);
            reasons.Insert(0, $"{reason} {delta:+0;-0;0}");
            if (reasons.Count > 20) reasons.RemoveAt(20);
        }
    }
}
