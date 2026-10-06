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
        // 종류 순서 = 저장된 질림 배열 순서(뒤에만 추가할 것). 2 책장(연구 경험치, 방 = 도서관) / 3 목욕통(위생, 방 = 목욕탕)
        // 2026-10-06 가구 5차: 놀이판(2인, 관계↑) / 장기판·바둑판(2인, 지휘 경험치) / 씨름판(2인, 근력 경험치, 라이벌은 관계↓)
        // 가시 다트(원거리 경험치) / 운동기구(근력 경험치) / 악기(오락 회복 ×1.5) / 거미줄 그네(싸다)
        private static readonly BuildingKind[] Kinds = { BuildingKind.Campfire, BuildingKind.GamblingDen, BuildingKind.Bookshelf, BuildingKind.Bathtub,
            BuildingKind.BoardGame, BuildingKind.Janggi, BuildingKind.Baduk, BuildingKind.WrestlingRing, BuildingKind.DartBoard, BuildingKind.ExerciseRig,
            BuildingKind.Instrument, BuildingKind.WebSwing, BuildingKind.HotSpring }; // 12 온천(가구 6차: 피로 회복, 방 = 목욕탕)
        private static readonly string[] Names = { "이야기 모닥불", "도박장", "책장", "목욕통", "놀이판", "장기판", "바둑판", "씨름판", "가시 다트", "운동기구", "악기", "거미줄 그네", "온천" };
        public static readonly int KindCount = Kinds.Length;
        public static bool IsKind(BuildingKind kind) => System.Array.IndexOf(Kinds, kind) >= 0;
        private static readonly List<RecreationSpot> Active = new List<RecreationSpot>();
        private readonly List<CommanderAnt> users = new List<CommanderAnt>();
        public static IReadOnlyList<RecreationSpot> All => Active;
        public IReadOnlyList<CommanderAnt> Users => users;
        public int KindIndex => Mathf.Max(0, System.Array.IndexOf(Kinds, Data.kind));
        public bool IsGambling => Data.kind == BuildingKind.GamblingDen;
        public int Seats => Data.kind switch
        {
            BuildingKind.Campfire => GameBalance.CampfireSeats, BuildingKind.GamblingDen => GameBalance.GamblingDenSeats,
            BuildingKind.BoardGame or BuildingKind.Janggi or BuildingKind.Baduk or BuildingKind.WrestlingRing => 2,
            BuildingKind.HotSpring => 4,
            _ => 1
        };
        public bool HasSeat => isActiveAndEnabled && !IsDead && users.Count < Seats;
        public bool NearDecoration => Decoration.CountNear(Position) > 0;
        public static string KindName(int kind) => kind >= 0 && kind < Names.Length ? Names[kind] : Names[0];

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
