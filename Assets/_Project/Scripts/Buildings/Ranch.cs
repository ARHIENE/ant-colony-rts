using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 목장 우리(2026-10-11 개편): 우리 표지를 놓은 방(벽·울타리·문으로 둘러싼 칸)이 우리다. 지붕은 필요 없고, 열린 문도 유효하다.
    // 벽이 부서지거나 문을 철거해 방이 사라지면 '뚫림': 먹기 외 생산·번식·돌봄·자동 치료·자동 채취를 멈추고 설정·진행도는 보존, 다시 막으면 재개.
    // 도축은 자동이 없고 개체별 수동 요청뿐(World/CritterSlaughter.cs). 목표 마릿수는 현황·초과 알림 용도.
    // 여러 종 합사·마릿수 무제한. 모든 개체의 필요 공간 합계로 과밀도를 계산해 생산·번식 효율만 줄인다.
    // 목장별 담당자는 없고 작업표(사육·운반·간호·요리)를 따른다. 우리를 철거해도 생물은 그 자리에 남는다(관리만 중단).
    public sealed class Ranch : BuildingBase
    {
        [Serializable] public class SpeciesSetting { public int species; public bool autoCare = true, overNotified; public int target = 6; public float breed; } // 옛 저장의 autoSlaughter는 읽지 않는다(폐기)
        [Serializable] public class State
        {
            public bool autoTreat = true, autoHarvest = true; public int acceptMask = -1;
            public List<SpeciesSetting> settings = new List<SpeciesSetting>();
            public int species = -1, count; // v16 옛 저장(한 종·마릿수) — 불러오면 개체로 풀어 놓는다
        }
        private State s = new State();
        public string Key => $"{Mathf.RoundToInt(Position.x * 10)}:{Mathf.RoundToInt(Position.z * 10)}";
        public static Ranch Find(string key) => FindObjectsByType<Ranch>(FindObjectsSortMode.None).FirstOrDefault(r => r.Key == key && !r.IsDead);
        // 한 방에 표지가 여럿이면 같은 우리다(키 순서로 첫 표지가 대표).
        public static Ranch InRoom(Room room) => room == null ? null : FindObjectsByType<Ranch>(FindObjectsSortMode.None)
            .Where(r => r.isActiveAndEnabled && !r.IsDead && RoomSystem.RoomAt(r.Position) == room).OrderBy(r => r.Key, StringComparer.Ordinal).FirstOrDefault();
        public Room Room => RoomSystem.RoomAt(Position);
        public bool Operating => isActiveAndEnabled && !IsDead && Room != null;
        public IEnumerable<Critter> Animals => Critter.All.Where(c => c != null && c.Pen == this && c.Clinic == null);
        public bool AutoTreat { get => s.autoTreat; set => s.autoTreat = value; }
        public bool AutoHarvest { get => s.autoHarvest; set => s.autoHarvest = value; }
        public bool Accepts(Species sp) => (s.acceptMask & (1 << (int)sp)) != 0 && isActiveAndEnabled && !IsDead;
        public void ToggleAccept(Species sp) => s.acceptMask ^= 1 << (int)sp;
        // 새 종은 자동 돌봄 켜짐. 빈 우리가 돼도 종 설정은 남는다.
        public SpeciesSetting Setting(Species sp)
        {
            var st = s.settings.Find(x => x.species == (int)sp);
            if (st == null) s.settings.Add(st = new SpeciesSetting { species = (int)sp });
            return st;
        }
        public IEnumerable<Species> KnownSpecies => s.settings.Select(x => (Species)x.species).Union(Animals.Select(a => a.Species)).Distinct().OrderBy(x => x);
        public float NeededSpace => Animals.Sum(a => a.Info.space * (a.Stage == CritterStage.Baby ? .5f : 1f));
        public int Cells => Room?.Cells.Count ?? 0;
        public float Crowding => Cells > 0 ? NeededSpace / Cells : 0;
        public float Efficiency => Crowding <= 1 ? 1 : 1 / Crowding; // 과밀이어도 받지만 생산·번식 효율이 준다

        // ---------- 번식(성체가 먹이·환경 조건을 갖추면 장수 작업 없이 자동) ----------
        private float notice;
        private void Update()
        {
            if (AntColony.Save.SaveSystem.Busy || Time.deltaTime <= 0) return;
            Tick(Time.deltaTime);
            if ((notice -= Time.unscaledDeltaTime) > 0) return; notice = 2;
            Notices();
        }
        public void Tick(float seconds)
        {
            if (!Operating || !(seconds > 0) || float.IsInfinity(seconds)) return;
            var month = seconds / GameCalendar.SecondsPerMonth; var eff = Efficiency;
            foreach (var group in Animals.Where(a => a.InOperatingPen).GroupBy(a => a.Species))
            {
                // 배고픈 개체는 번식하지 않고, 노령은 번식 속도가 서서히 준다. 목표 마릿수와 무관하게 번식한다.
                var parents = group.Where(a => a.Stage != CritterStage.Baby && a.Hunger >= GameBalance.CritterBreedHunger).ToList();
                if (parents.Count < 2) continue;
                var weight = parents.Sum(a => a.Stage == CritterStage.Old ? Mathf.Clamp01(1 - (a.Age - a.Info.oldMonths) / Mathf.Max(.1f, a.Info.lifeMonths - a.Info.oldMonths)) * .5f : 1f);
                var care = parents.Count(a => a.CareRemaining > 0) / (float)parents.Count;
                var st = Setting(group.Key);
                st.breed += month * GameBalance.CritterBreedPerMonth * weight / 2 * eff * (1 + (GameBalance.CritterCareBoost - 1) * care);
                while (st.breed >= 1)
                {
                    st.breed -= 1; var parent = parents[UnityEngine.Random.Range(0, parents.Count)];
                    var baby = Critter.Spawn(group.Key, parent.transform.position, false, 0); baby.Pen = this; // 새끼는 부모와 무관하게 야생으로 태어난다
                }
            }
        }

        // ---------- 목표 마릿수(현황·초과 알림) ----------
        public int CountOf(Species sp) => Animals.Count(a => a.Species == sp); // 성체 + 새끼

        // ---------- 수동 지시 대기·목표 초과 알림 ----------
        private void Notices()
        {
            var waiting = Operating && Animals.Any(a => a.InOperatingPen && AwaitingManual(a));
            ToastManager.SetCrisis("ranch:" + Key, waiting ? $"{Data.displayName}: 위험한 야생 개체 작업 또는 집도의 지정 대기 — 개체를 선택해 장수를 지정하세요." : null,
                Critter.FocusAction(Position), "목장으로 이동");
            foreach (var sp in KnownSpecies)
            {
                var st = Setting(sp); var n = CountOf(sp); var over = n > st.target;
                if (over && !st.overNotified) { st.overNotified = true; ToastManager.ShowAt($"{Data.displayName}: {SpeciesInfo.For(sp).name} {n}마리 — 목표 {st.target}마리를 넘었습니다. 필요하면 개체를 골라 도축을 요청하세요.", Critter.FocusAction(Position)); }
                if (!over) st.overNotified = false;
            }
        }
        protected override void OnDisable() { base.OnDisable(); ToastManager.SetCrisis("ranch:" + Key, null); }

        // ---------- 작업 후보(장수 작업표에서 부른다) ----------
        private static IEnumerable<CommanderAnt> Keepers => AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember);
        // 자동으로 맡을 수 있나: 안전·주의는 자동, 위험은 유저가 그 장수를 지정했을 때만(성공 보장 없음).
        public static bool MayHandle(Critter a, CommanderAnt c, CommanderActivity skill = CommanderActivity.Husbandry)
            => SpeciesInfo.Risk(SpeciesInfo.FailChance(a.Species, a.Tame || a.Downed, c, skill)) != HandlingRisk.Danger || a.ApprovedId == c.PersonalState.id
                || skill == CommanderActivity.Medicine && a.SurgeonId == c.PersonalState.id; // 유저가 지정한 집도의
        private static bool Free(Critter a) => a != null && a.Monster != null && !a.Monster.IsDead && a.Handler == null && a.Carrier == null && a.Clinic == null && !a.Agitated;
        public bool NeedsCare(Critter a) => Free(a) && a.InOperatingPen && !a.CareBlocked && a.CareRemaining <= 0 && (Setting(a.Species).autoCare || a.CareOrder);
        public bool NeedsHarvest(Critter a) => Free(a) && a.InOperatingPen && s.autoHarvest && a.DirectStock >= 1;
        public bool NeedsDose(Critter a) => Free(a) && a.InOperatingPen && (s.autoTreat || a.TreatOrder) && a.Injured && !a.HeavyInjury && a.Medicated <= 0;
        public bool NeedsSurgery(Critter a) => Free(a) && (a.InOperatingPen && s.autoTreat || a.TreatOrder) && a.HeavyInjury;
        // 위험 개체라 수동 지시를 기다리는 작업이 있나(돌봄 자동 꺼짐이면 위험 개체도 돌봄을 만들지 않는다).
        public bool AwaitingManual(Critter a)
        {
            if (NeedsSurgery(a) && a.AwaitingSurgeon) return true; // 고위험 첫 수술·재수술: 집도의 지정 대기
            if (a.ApprovedId != "") return false;
            bool Need(CommanderJobs job, CommanderActivity skill) => !Keepers.Any(c => c.AllowsJob(job) && SpeciesInfo.Risk(SpeciesInfo.FailChance(a.Species, a.Tame || a.Downed, c, skill)) != HandlingRisk.Danger);
            return (NeedsCare(a) && CareStationFor(a) != null || NeedsHarvest(a) || a.SlaughterOrdered && !a.SlaughterStarted) && Need(CommanderJobs.Husbandry, CommanderActivity.Husbandry)
                || (NeedsDose(a) || NeedsSurgery(a) && a.SurgeonId == "") && Need(CommanderJobs.Nursing, CommanderActivity.Medicine);
        }
        public RanchFacility CareStationFor(Critter a) => RanchFacility.All.FirstOrDefault(f => f.IsCareStation && f.Usable && f.Worker == null && RoomSystem.RoomAt(f.Position) == Room);

        public IEnumerable<(Component target, Func<bool> start)> HusbandryTasks(CommanderAnt c)
        {
            if (!Operating) yield break;
            // 돌봄: 효과가 끝난 개체 중 오래 기다린 순서. 먹는 중이거나 못 오는 개체는 건너뛴다(순서는 유지).
            foreach (var station in RanchFacility.All.Where(f => f.IsCareStation && f.Usable && f.Worker == null && RoomSystem.RoomAt(f.Position) == Room))
            {
                var a = Animals.Where(x => NeedsCare(x) && !x.Eating && x.Reachable(station.Position) && MayHandle(x, c)).OrderByDescending(x => x.Waiting).FirstOrDefault();
                if (a != null) yield return (station, () => c.StartAnimalTask(AnimalTask.Care, a, station));
            }
            foreach (var a in Animals.Where(x => NeedsHarvest(x) && MayHandle(x, c)).ToList())
                yield return (a, () => c.StartAnimalTask(AnimalTask.Harvest, a));
        }
        // 치료 순서: 직접 지시 → 부상이 심한 순 → 오래 기다린 순. 예약되지 않은 재고가 있어야 시작한다(AnimalMedicalStock).
        // 중상은 빈 치료대와 키트를 모두 확보할 수 있을 때만, 고위험·재수술은 유저가 지정한 집도의만.
        public IEnumerable<(Component target, Func<bool> start)> NursingTasks(CommanderAnt c)
        {
            foreach (var a in Critter.All.Where(x => x != null && x.Pen == this && (NeedsDose(x) || NeedsSurgery(x)) && MayHandle(x, c, CommanderActivity.Medicine))
                .OrderByDescending(x => x.TreatOrder).ThenBy(x => x.HealthFraction).ThenByDescending(x => x.Waiting).ToList())
            {
                if (a.HeavyInjury)
                {
                    if (!a.MayOperate(c)) continue;
                    var clinic = RanchFacility.FreeClinic();
                    if (clinic != null && AnimalMedicalStock.Available(ResourceType.SurgeryKit) >= 1) yield return (a, () => c.StartAnimalTask(AnimalTask.Surgery, a, clinic));
                }
                else if (AnimalMedicalStock.Available(ResourceType.AnimalMedicine) >= 1) yield return (a, () => c.StartAnimalTask(AnimalTask.Dose, a));
            }
        }

        // ---------- 저장 ----------
        internal State CaptureState() => JsonUtility.FromJson<State>(JsonUtility.ToJson(s));
        internal void RestoreState(State st)
        {
            s = st ?? new State(); s.settings ??= new List<SpeciesSetting>();
            s.settings.RemoveAll(x => x == null || !Enum.IsDefined(typeof(Species), x.species));
            foreach (var x in s.settings) { x.target = Mathf.Clamp(x.target, 0, 999); if (!(x.breed >= 0) || float.IsInfinity(x.breed)) x.breed = 0; }
            // v16 옛 우리: 한 종 마릿수를 길들인 성체로 풀어 놓는다(옛 자동 도축 설정은 폐기되어 옮기지 않는다).
            // 생물 복원(Critter.RestoreAll)이 건물 뒤에 오므로 첫 프레임에 풀어 놓는다.
            if (s.species >= 0 && Enum.IsDefined(typeof(Species), s.species) && s.count > 0) legacy = ((Species)s.species, Mathf.Min(s.count, 50));
            s.species = -1; s.count = 0;
        }
        private (Species sp, int n)? legacy;
        private void LateUpdate()
        {
            if (legacy == null || AntColony.Save.SaveSystem.Busy) return;
            var (sp, n) = legacy.Value; legacy = null;
            for (var i = 0; i < n; i++) { var a = Critter.Spawn(sp, Position + UnityEngine.Random.insideUnitSphere.Flat() * 1.5f, true); a.Pen = this; }
        }
    }
}
