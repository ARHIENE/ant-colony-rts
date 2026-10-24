using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public enum CritterStage { Baby, Adult, Old }

    // 목장 생물 개체(2026-10-11 개편): 야생·사육 모두 개체 하나하나가 나이·배고픔·길들임·돌봄·부상·생산을 가진다.
    // 소속 우리는 위치로 정한다(우리 표지가 있는 방 안에 있으면 그 우리 관리 대상, 열린 문으로 스스로 들어와도 등록).
    // 행동(먹이 찾기·배회·포식)은 CritterBehaviour.cs, 장수 작업은 Units/CommanderHusbandry.cs.
    public sealed partial class Critter : MonoBehaviour
    {
        private static readonly List<Critter> AllList = new List<Critter>();
        public static IReadOnlyList<Critter> All => AllList;

        [Serializable] public class State
        {
            public int species, id; public bool tame, designated, careBlocked, treatOrder, careOrder, surgeryStarted;
            public float tameProgress, age, hunger = 80, care, waiting, medicated, direct, floorProgress, bind, surgery, health;
            public string pen = "", transport = "", clinic = "", returnPen = "", approved = "";
            // 2026-10-11 수정: 수동 도축 요청, 수술 기여도·후유증·회복·실패 후 선택 대기, 투약 당시 경상량(옛 저장은 기본값).
            public AnimalDiseaseState disease = new AnimalDiseaseState();
            public bool slaughterOrder, slaughterStarted, awaitDecision; public int sequelae;
            public float surgeryWork, surgerySkillWork, recovery, preHealth = 1, medBudget; public string surgeon = "", custom = ""; // custom = 유저가 바꾼 이름(없으면 종 이름)
            public Vec3Dto position, home;
            public float excrete; // 2026-10-15 배설 누적(1이 되면 배설물 1개)
        }
        private State s = new State();
        public Species Species => (Species)s.species;
        public SpeciesInfo Info => SpeciesInfo.For(Species);
        public WildMonster Monster { get; private set; }
        public bool Tame => s.tame;
        public float TameProgress => s.tameProgress;
        public float Age => s.age;
        public float Hunger => s.hunger;
        public float CareRemaining => s.care;
        public float Waiting => s.waiting;
        public float Medicated => s.medicated;
        public float DirectStock => s.direct;
        public float SurgeryProgress => s.surgery;
        public bool SurgeryStarted => s.surgeryStarted;
        public bool CaptureDesignated { get => s.designated; set => s.designated = value; }
        public bool CareBlocked { get => s.careBlocked; set => s.careBlocked = value; }
        public bool TreatOrder { get => s.treatOrder; set => s.treatOrder = value; }
        public bool CareOrder { get => s.careOrder; set => s.careOrder = value; }
        public string ApprovedId { get => s.approved; set => s.approved = value ?? ""; }
        public CritterStage Stage => s.age < Info.babyMonths ? CritterStage.Baby : s.age < Info.oldMonths ? CritterStage.Adult : CritterStage.Old;
        public string StageName => Stage == CritterStage.Baby ? "새끼" : Stage == CritterStage.Adult ? "성체" : "노령";
        public float HealthFraction => Monster != null ? Monster.CurrentHealth / Mathf.Max(1, Info.health) : 0;
        public bool Injured => HealthFraction < .999f;
        public bool HeavyInjury => HealthFraction < GameBalance.CritterHeavyInjury;
        public bool Downed => HealthFraction < GameBalance.CritterDownedHealth; // 쓰러져 움직이지 못함: 결박 없이 운반
        public bool Starving => s.hunger <= 0;
        public bool Bound => s.bind > 0;
        // 운반할 수 있는 상태: 결박·쓰러짐·길들임(저항 없음)·치료 시설에서 복귀 대기(수술·회복·실패 후 선택 대기 중이 아닐 때).
        // 운반은 결박되거나 쓰러진 개체만(2026-10-11 사용자 확정: 길들인 개체도 먼저 포획 — 저항·실패 없이 결박). 시설 안에서는 회복·선택 대기가 끝나야 데려간다.
        public bool Carriable => Clinic != null ? ReadyToLeaveClinic : Bound || Downed;
        public int Id => s.id;
        // 기본 이름은 종 이름, 유저가 바꾼 이름은 유지(코어키퍼 방식). 알림·목록은 '이름(종)' 또는 '종'.
        public string DisplayName => s.custom != "" ? $"{s.custom}({Info.name})" : Info.name;
        public string CustomName { get => s.custom; set { var v = (value ?? "").Trim(); s.custom = v.Length > 16 ? v.Substring(0, 16) : v; } } // 비우면 종 이름으로
        public string SequelaEffect => s.sequelae > 0 ? "수술 실패 이력 · 재수술은 직접 지정(횟수에 따른 위험 가산 없음)" : "";
        private static int NextId() => AllList.Count == 0 ? 1 : AllList.Max(c => c != null && c.s != null ? c.s.id : 0) + 1;

        // 관계(키로 저장하고 실행 중엔 참조로 푼다).
        private Ranch pen, transport, returnPen; private RanchFacility clinic;
        public Ranch Pen { get => Resolve(ref pen, s.pen); internal set { pen = value; s.pen = value != null ? value.Key : ""; } }
        public Ranch TransportTo { get => Resolve(ref transport, s.transport); set { transport = value; s.transport = value != null ? value.Key : ""; } }
        public Ranch ReturnPen { get => Resolve(ref returnPen, s.returnPen); internal set { returnPen = value; s.returnPen = value != null ? value.Key : ""; } }
        public RanchFacility Clinic { get { if (clinic == null && s.clinic != "") clinic = RanchFacility.Find(s.clinic); return clinic; } private set { clinic = value; s.clinic = value != null ? value.Key : ""; } }
        private static Ranch Resolve(ref Ranch cache, string key) { if (cache == null && key != "") cache = Ranch.Find(key); return cache; }
        public bool Managed => Pen != null;
        public bool InOperatingPen => Pen != null && Pen.Operating && RoomSystem.RoomAt(transform.position) == Pen.Room;

        public CommanderAnt Handler { get; internal set; }   // 이 개체를 다루는 장수(포획·돌봄·채취·도축·치료·운반)
        public CommanderAnt Carrier { get; private set; }    // 들고 있는 장수
        public RanchFacility CalledTo { get; internal set; } // 돌봄 시설 호출
        internal string PendingCause { get; set; }
        internal Critter Killer { get; set; }

        private void Awake() { AllList.Add(this); Monster = GetComponent<WildMonster>(); }
        private void OnDestroy() { AllList.Remove(this); ClearAlert(); }

        // ponytail: 종별 체력·크기는 잠정. 외형은 임시 캡슐(새끼는 작게).
        public static Critter Spawn(Species sp, Vector3 at, bool tame, float age = -1, float health = -1)
        {
            if (NavMesh.SamplePosition(at, out var hit, 8f, NavMesh.AllAreas)) at = hit.position;
            var info = SpeciesInfo.For(sp);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.SetActive(false);
            go.name = info.name; go.transform.position = at + Vector3.up * .5f;
            var color = sp switch { Species.Aphid => new Color(.5f, .8f, .3f), Species.Silkworm => new Color(.95f, .93f, .85f), Species.Spider => new Color(.2f, .15f, .15f), _ => new Color(.95f, .75f, .15f) };
            go.GetComponent<Renderer>().material.color = color;
            var id = NextId();
            var monster = go.AddComponent<WildMonster>();
            monster.ConfigureCritter(info.health, info.damage);
            monster.Temperament = info.temper == CritterTemper.Attack ? WildlifeTemperament.Predator : info.temper == CritterTemper.Conditional ? WildlifeTemperament.Defensive : WildlifeTemperament.Timid;
            var critter = go.AddComponent<Critter>();
            critter.s = new State { species = (int)sp, id = id, tame = tame, age = age >= 0 ? age : info.babyMonths + 1, home = new Vec3Dto(at) };
            critter.Rescale();
            go.SetActive(true);
            if (health > 0) monster.RestoreHealth(Mathf.Min(health, info.health));
            return critter;
        }
        private void Rescale() => transform.localScale = Vector3.one * (Info.space >= 2 ? 1f : .5f) * (Stage == CritterStage.Baby ? .6f : 1f);

        // ---------- 생애(나이·배고픔·부상·돌봄·길들이기·생산·결박) ----------
        private float penScan, stageWas = -1;
        private void Update()
        {
            if (SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted || Monster == null || Monster.IsDead) return;
            var dt = Time.deltaTime; if (!(dt > 0)) return;
            if (Carrier != null) { FollowCarrier(); TickLife(dt); return; }
            if (Clinic != null && (Clinic.IsDead || !Clinic.isActiveAndEnabled)) ExitClinic(transform.position); // 시설 파괴: 수술 중이면 처음부터, 회복 중이면 성공·잔여 회복 보존
            if ((penScan -= dt) <= 0) { penScan = 1; UpdatePen(); }
            TickLife(dt);
            if (Monster == null || Monster.IsDead) return;
            TickBehaviour(dt);
        }

        private void TickLife(float dt)
        {
            var month = dt / GameCalendar.SecondsPerMonth;
            TickDisease(month); if (Monster.IsDead) return;
            TickExcretion(month);
            var paused = HungerPausedInClinic; TickRecovery(dt);
            if (Stage != CritterStage.Baby || !Starving) s.age += month; // 새끼는 굶으면 자라지 않는다
            if ((int)Stage != (int)stageWas) { stageWas = (int)Stage; Rescale(); }
            if (Stage == CritterStage.Old)
            {
                // 노령: 나이가 들수록 자연사 확률이 높아진다(정확한 시점은 표시하지 않는다).
                var over = (s.age - Info.oldMonths) / Mathf.Max(.1f, Info.lifeMonths - Info.oldMonths);
                if (UnityEngine.Random.value < month * GameBalance.CritterOldDeathPerMonth * (1 + over * over * 4)) { Die("노령"); return; }
            }
            if (!paused) s.hunger = Mathf.Max(0, s.hunger - month * 100f / GameBalance.CritterHungerMonths); // 실제 수술·시설 내 회복 중에만 배고픔 정지
            if (Starving)
            {
                if (!starveNotified && Managed) { starveNotified = true; ToastManager.Show($"{Pen.Data.displayName}: {Info.name}이(가) 굶고 있습니다."); }
                Wound(Info.health * GameBalance.CritterStarvePerMonth * month, "굶주림"); if (Monster.IsDead) return;
            }
            else if (s.hunger > 30) starveNotified = false;
            s.medicated = Mathf.Max(0, s.medicated - dt); if (s.medicated <= 0) s.medBudget = 0;
            // 경상은 목장에서 천천히 자연 회복, 동물용 의약품 투약 중이면 빨라진다. 중상은 치료 시설 수술로만.
            // 투약 효과는 투약 당시 있던 경상(잃은 체력)만 가속한다. 이후 새로 생긴 경상은 보통 속도.
            var operating = InOperatingPen;
            // 회복 중 치료 시설 안에서 새로 생긴 경상도 자연 회복·투약한다(기획). 그 밖의 시설 대기 상태는 기존대로 회복하지 않는다.
            if (Injured && !HeavyInjury && (operating && Clinic == null || Clinic != null && Recovering))
            {
                var heal = Info.health * GameBalance.CritterLightRegenPerMonth * month;
                if (s.medicated > 0 && s.medBudget > 0) { heal = Mathf.Min(heal * GameBalance.CritterMedicatedRegen, Mathf.Max(heal, s.medBudget)); s.medBudget = Mathf.Max(0, s.medBudget - heal); }
                Monster.Heal(heal);
            }
            // 돌봄 효과·길들이기: 뚫린 우리·우리 밖에서는 돌봄 효과가 끊기고 미완료 길들이기는 서서히 줄어든다.
            s.care = operating ? Mathf.Max(0, s.care - dt) : 0;
            s.waiting = s.care > 0 ? 0 : s.waiting + dt;
            if (!s.tame && Info.tameable)
            {
                s.tameProgress = s.care > 0 ? s.tameProgress + month / GameBalance.CritterTameMonths : Mathf.Max(0, s.tameProgress - month / GameBalance.CritterTameDecayMonths);
                if (s.tameProgress >= 1) { s.tame = true; s.tameProgress = 1; ToastManager.Show($"{Info.name}이(가) 길들여졌습니다."); }
            }
            // 생산: 성체·노령, 운영 중인 우리, 굶지 않을 때. 직접 채취 물건은 한도까지 쌓이면 멈춘다.
            if (operating && Clinic == null && Stage != CritterStage.Baby && !Starving && !SevereDisease)
            {
                var eff = (Sick ? .5f : 1f) * Pen.Efficiency * (s.care > 0 ? GameBalance.CritterCareBoost : 1f);
                if (Info.directPerMonth > 0) s.direct = Mathf.Min(GameBalance.CritterDirectCap, s.direct + Info.directPerMonth * month * eff);
                if (Info.floorPerMonth > 0 && (s.floorProgress += Info.floorPerMonth * month * eff) >= 1)
                { var n = Mathf.FloorToInt(s.floorProgress); s.floorProgress -= n; DiplomacyManager.DropFloor(Info.floor, n, transform.position); }
            }
            // 결박: 바닥에 묶여 있는 동안만 흐르고 실제 운반 중엔 멈춘다. 만료되면 조용히 풀리고 운반 요청도 취소된다.
            if (s.bind > 0 && Carrier == null && !s.slaughterStarted && (s.bind -= dt) <= 0) // 실제 도축 중에는 결박 시간이 멈춘다
            {
                s.bind = 0;
                if (!Downed && Clinic == null) TransportTo = null; // 결박이 풀리면 운반 요청도 취소(길들인 개체 포함)
            }
        }
        private bool starveNotified;

        private void UpdatePen()
        {
            if (Clinic != null) return;
            var room = RoomSystem.RoomAt(transform.position);
            var here = room != null ? Ranch.InRoom(room) : null;
            if (here != null) Pen = here;
            else if (Pen == null && s.pen != "") Pen = null;   // 우리 표지를 철거했다
            else if (Pen != null && Pen.Operating) Pen = null; // 멀쩡한 우리 밖으로 나갔다(자동 복귀 없음)
        }

        // ---------- 피해·죽음·방생 ----------
        // 체력 감소(굶주림·포식·도축 등). 일반 공격(TakeDamage)과 달리 놀라게 하지 않는다.
        internal void Wound(float amount, string cause, Critter killer = null)
        {
            if (Monster == null || Monster.IsDead || !(amount > 0)) return;
            PendingCause = cause; Killer = killer; Monster.Wound(amount);
        }
        internal void Die(string cause) { PendingCause = cause; Monster.Wound(Monster.CurrentHealth + 1); }
        // WildMonster.Die가 부른다: 사체를 남기고(해체 가능), 관리 동물이면 원인과 함께 알린다. 지시한 도축·포식은 알리지 않는다.
        internal void OnKilled()
        {
            var cause = PendingCause ?? "부상";
            if (Carrier != null) Carrier.DropCarriedCritter();
            if (Clinic != null) transform.position = Clinic.Position + Vector3.right * 1.5f; // 시설 안 사망: 사체는 옆 바닥, 자리는 빈다
            CritterCarcass.Drop(this, cause == "포식" ? Killer : null);
            if (Managed && cause != "도축" && cause != "포식")
                ToastManager.ShowAt($"{Pen.Data.displayName}: {Info.name}이(가) 죽었습니다 — {cause}", FocusAction(transform.position));
            Handler?.CommandStop();
            Destroy(gameObject);
        }
        // 방생: 확인 후 즉시 사라진다(사체·생산물 없음, 관련 작업 취소). 치료 시설 안에서는 불가(우리로 복귀한 뒤).
        public bool CanRelease => Clinic == null;
        public void Release()
        {
            if (!CanRelease) return;
            if (Carrier != null) Carrier.DropCarriedCritter();
            Handler?.CommandStop();
            Destroy(gameObject);
        }
        internal static Action FocusAction(Vector3 at) => () => FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.FocusOn(at);

        // ---------- 결박·운반·치료 시설 ----------
        internal void Bind() { s.bind = GameBalance.CritterBindSeconds; s.designated = false; Halt(); }
        internal void PickUp(CommanderAnt carrier)
        {
            Carrier = carrier; CalledTo = null; foodTarget = null;
            if (Clinic != null && !s.surgeryStarted) Clinic = null; // 수술을 마친 개체를 시설에서 꺼낸다
            if (Monster.Agent != null) Monster.Agent.enabled = false;
        }
        internal void PutDown(Vector3 at)
        {
            Carrier = null;
            if (NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) at = hit.position;
            transform.position = at + Vector3.up * .5f;
            if (Monster.Agent != null) { Monster.Agent.enabled = true; Monster.Agent.Warp(at); }
            penScan = 0;
        }
        // 목적지 우리에 내려놓기: 결박을 풀고 운반 요청을 끝낸다. 길들임·진행도·돌봄 효과·도축 금지·돌봄 재시도 중지는 유지.
        internal void Deliver(Vector3 at) { PutDown(at); s.bind = 0; TransportTo = null; UpdatePen(); }
        private void FollowCarrier() { if (Carrier != null) transform.position = Carrier.Position + Vector3.up * 1.2f; }
        internal void EnterClinic(RanchFacility facility)
        {
            if (Pen != null) ReturnPen = Pen; // 회복 재입원은 원래 우리를 유지
            Carrier = null; Clinic = facility; TransportTo = null; s.bind = 0; DropSlaughterOnAdmission();
            transform.position = facility.Position + Vector3.up * 1f;
            if (Monster.Agent != null) Monster.Agent.enabled = false;
        }
        // 수술·회복·실패 처리는 CritterSurgery.cs.

        // ---------- 장수 작업 결과 ----------
        internal void FinishCare(CommanderAnt c)
        {
            s.care = GameBalance.CritterCareBaseSeconds * (1 + c.Talents.Level(CommanderActivity.Husbandry) / 10f); // 사육 능력이 높으면 돌봄 효과가 오래간다
            s.waiting = 0; s.careOrder = false;
        }
        internal int TakeDirect() { var n = Mathf.FloorToInt(s.direct); s.direct -= n; return n; }
        internal void Medicate() { if (NeedsDiseaseMedicine) Disease.medication = 1; s.medicated = GameBalance.CritterMedicatedSeconds; s.medBudget = Mathf.Max(0, Info.health - Monster.CurrentHealth); } // 투약 당시의 경상만
        internal void Feed(float units) { s.hunger = Mathf.Min(100, s.hunger + units * GameBalance.CritterHungerPerUnit); }

        // 취급 실패·공격받음: 성향대로 도주(도주형)·주변 공격(공격형)·다룬 장수 공격(조건부). 잠시 뒤 진정한다.
        public void Agitate(IDamageable handler)
        {
            agitated = GameBalance.CritterAgitatedSeconds; foodTarget = null; CalledTo = null;
            if (Monster == null) return;
            Monster.Docile = false; Monster.Provoke();
            if (Info.temper == CritterTemper.Conditional && handler != null) Monster.Aggro(handler);
            if (Info.temper == CritterTemper.Attack && handler != null) Monster.Aggro(handler);
        }
        public bool Agitated => agitated > 0;
        private float agitated;

        // 탈출 알림(취급 실패로 도주·공격하는 개체). 진정하거나 죽으면 지운다.
        internal string AlertKey { get; set; }
        private void ClearAlert() { if (AlertKey != null) { ToastManager.SetCrisis(AlertKey, null); AlertKey = null; } }
        internal void RaiseEscape(string text)
        {
            AlertKey = "escape:" + GetHashCode();
            ToastManager.SetCrisis(AlertKey, text, () => FindFirstObjectByType<AntColony.Camera.IsometricCameraController>()?.FocusOn(transform.position), "위치로 이동");
        }

        // ---------- 저장 ----------
        internal State Capture()
        {
            var st = JsonUtility.FromJson<State>(JsonUtility.ToJson(s));
            st.position = new Vec3Dto(Carrier != null ? Carrier.Position : transform.position); st.health = Monster != null ? Monster.CurrentHealth : 0;
            return st;
        }
        internal static List<State> CaptureAll() => AllList.Where(c => c != null && c.Monster != null && !c.Monster.IsDead).Select(c => c.Capture()).ToList();
        internal static void RestoreAll(List<State> list)
        {
            foreach (var c in AllList.ToArray()) if (c != null) DestroyImmediate(c.gameObject);
            if (list == null) return;
            foreach (var d in list)
            {
                if (d == null || !Enum.IsDefined(typeof(Species), d.species) || !(d.health > 0) || d.position == null) continue;
                var c = Spawn((Species)d.species, d.position.ToVector3(), d.tame, Mathf.Max(0, d.age), d.health);
                d.pen ??= ""; d.transport ??= ""; d.clinic ??= ""; d.returnPen ??= ""; d.approved ??= ""; d.home ??= d.position;
                d.hunger = Mathf.Clamp(float.IsNaN(d.hunger) ? 80 : d.hunger, 0, 100);
                d.surgeon ??= ""; d.custom = (d.custom ?? "").Trim(); if (d.custom.Length > 16) d.custom = d.custom.Substring(0, 16); d.sequelae = Mathf.Max(0, d.sequelae); if (!(d.recovery >= 0)) d.recovery = 0; if (!(d.medBudget >= 0)) d.medBudget = 0;
                if (!(d.preHealth > 0)) d.preHealth = 1; if (!(d.surgeryWork >= 0) || !(d.surgerySkillWork >= 0)) d.surgeryWork = d.surgerySkillWork = 0;
                d.slaughterStarted = false; // 진행 중이던 도축은 불러오면 처음부터(요청은 유지)
                c.s = d; if (d.id <= 0) d.id = NextId(); c.Rescale();
                if (c.Clinic != null) c.EnterClinicRestored();
            }
        }
        private void EnterClinicRestored() { transform.position = Clinic.Position + Vector3.up; if (Monster.Agent != null) Monster.Agent.enabled = false; }

        // 본거지 주변 야생 개체(새 게임 때만, 불러오기는 저장된 개체를 되살린다).
        public static void SpawnWild(Vector3 home, int seed)
        {
            var rng = new System.Random(seed ^ 0x0c3177e);
            Species[] start = { Species.Aphid, Species.Aphid, Species.Silkworm, Species.Silkworm, Species.Spider, Species.Bee, Species.Bee };
            foreach (var sp in start) Spawn(sp, RingPoint(home, rng), false);
        }
        internal static Vector3 RingPoint(Vector3 home, System.Random rng)
        {
            var angle = (float)rng.NextDouble() * Mathf.PI * 2; var radius = 35f + (float)rng.NextDouble() * 25f;
            return home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        }
    }

    // 야생 생물이 줄면 매달 맵 가장자리 쪽에 한 마리씩 다시 나타난다.
    public sealed class CritterSpawner : MonoBehaviour
    {
        private float timer;
        private System.Random rng;
        private void Update()
        {
            if (!GameSession.Exists || !GameSession.Instance.GameStarted || SaveSystem.Busy || Time.deltaTime <= 0) return;
            timer += Time.deltaTime; if (timer < GameCalendar.SecondsPerMonth) return; timer = 0;
            if (Critter.All.Count(c => c != null && !c.Tame && !c.Managed) >= GameBalance.WildCritterTarget) return;
            rng ??= new System.Random(GameSession.Instance.Options.seed + Environment.TickCount);
            var home = ResourceManager.HomePosition;
            Critter.Spawn((Species)rng.Next(0, 4), Critter.RingPoint(home, rng), false);
        }
    }
}
