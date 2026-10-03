using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 오락 시설(2026-09-28): 소형 건물 여러 종류를 따로 배치한다. 같은 종류만 하면 질린다(CommanderJoy).
    // 이용자 목록은 저장하지 않는다. 불러오면 노는 중인 장수는 제자리에서 마저 논다.
    public sealed class RecreationSpot : BuildingBase
    {
        public const int KindCount = 2;
        private static readonly List<RecreationSpot> Active = new List<RecreationSpot>();
        private readonly List<CommanderAnt> users = new List<CommanderAnt>();
        public static IReadOnlyList<RecreationSpot> All => Active;
        public IReadOnlyList<CommanderAnt> Users => users;
        public int KindIndex => Data.kind == BuildingKind.GamblingDen ? 1 : 0;
        public bool IsGambling => Data.kind == BuildingKind.GamblingDen;
        public int Seats => IsGambling ? GameBalance.GamblingDenSeats : GameBalance.CampfireSeats;
        public bool HasSeat => isActiveAndEnabled && !IsDead && users.Count < Seats;
        public bool NearDecoration => Decoration.CountNear(Position) > 0;
        public static string KindName(int kind) => kind == 1 ? "도박장" : "이야기 모닥불";

        protected override void OnEnable()
        {
            base.OnEnable(); Active.Add(this);
            // 모닥불은 밤에 주변을 따뜻하게 밝힌다.
            if (Data != null && Data.kind == BuildingKind.Campfire && GetComponent<Light>() == null)
            { var light = gameObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 8; light.color = new Color(1, .6f, .25f); light.intensity = 2; }
        }
        protected override void OnDisable() { Active.Remove(this); users.Clear(); base.OnDisable(); }

        public bool Join(CommanderAnt c)
        {
            users.RemoveAll(u => u == null || u.IsDead || u.PlaySpot != this);
            if (users.Contains(c)) return true;
            if (!HasSeat) return false;
            users.Add(c); return true;
        }
        public void Leave(CommanderAnt c) => users.Remove(c);
        public CommanderAnt Partner(CommanderAnt c)
        {
            var others = users.Where(u => u != c && u != null && !u.IsDead && u.PlaySpot == this).ToList();
            return others.Count == 0 ? null : others[Random.Range(0, others.Count)];
        }
    }
}
