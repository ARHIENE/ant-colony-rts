using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 석방된 자국 장수: 게임 3개월 + 거리 비례 시간 뒤 부상을 지닌 채 본거지에 도착한다(안전 귀환, 작업·전투·협상 불가).
    [Serializable] public sealed class Homecoming
    {
        public Prisoner commander;
        public float arrive;
        public string from;
        public string to = ""; // ""면 우리 본거지로, 아니면 그 세력 id로 돌아가는 적 장수(2026-10-10 양쪽 같은 귀환 규칙)
        public List<EquipmentItem> gear = new List<EquipmentItem>(); // 반환에 합의한 압수 장비(함께 도착)
    }
    // 포획 시 압수한 장비. holder = 보관 세력 id 또는 "player". 장비 반환 협상·재정복 구출에 쓴다.
    [Serializable] public sealed class Seizure { public string owner, item, holder; }

    // 2026-10-09 포로 외교: 자국 장수 몸값·상호 교환만(제3자 포로 구매 없음), 포획 시 부상·장비 압수, 거리 비례 안전 귀환.
    public sealed partial class DiplomacyManager
    {
        // ponytail: 기획 미정 수치(잠정). 부상 확률 80%, 거리 계수 = 월드맵 좌표 거리 1당 1000초, 몸값 = (CA×4 + 특성×15 + 정치×10 + 50) × 관계.
        public const float CaptureInjuryChance = .8f, HomecomingDistanceSeconds = 1000;
        public static float HomecomingBaseSeconds => 3 * DiplomacyRules.Month;

        // 상대가 붙잡고 있는 자국 장수: 반란군 소속(원래 우리 장수) + 상대 거점에 억류된 장수.
        public IEnumerable<CommanderAnt> HeldCaptives(Civilization c) => world == null ? Enumerable.Empty<CommanderAnt>()
            : world.Sites.Where(s => s.Defense != null).SelectMany(s => s.Defense.Prisoners)
                .Where(p => p != null && !p.IsDead && Data.seizures.Any(x => x.owner == p.PersonalState.id && x.item == "" && x.holder == c.id));
        public IEnumerable<string> RansomableIds(Civilization c) => c.rebels.Select(p => p.PersonalState.id).Concat(HeldCaptives(c).Select(p => p.PersonalState.id));
        // 우리가 잡은 포로 중 그 세력 출신만 넘길 수 있다.
        public static IEnumerable<Prisoner> ReleasableTo(Civilization c) => PlayerPrisoners.Where(p => p.PersonalState.originFaction == c.id);

        public static double RansomValue(CommanderTalents talents, CommanderTraits traits, Civilization c)
            => (talents.Current * 4 + traits.values.Count * 15 + talents.Level(CommanderActivity.Politics) * 10 + 50) * (1 - Mathf.Clamp(c.affinity, -100, 100) / 400d);
        public double RansomValue(string id, Civilization c)
        {
            var mine = HeldCaptives(c).FirstOrDefault(p => p.PersonalState.id == id);
            if (mine != null) return RansomValue(mine.Talents, mine.Traits, c);
            var p = c.rebels.Concat(PlayerPrisoners).FirstOrDefault(x => x.PersonalState.id == id);
            return p == null ? 0 : RansomValue(p.Talents, p.Traits, c);
        }

        public ExpeditionSite HomeSiteOf(Civilization c) => world?.Sites.FirstOrDefault(s => Faction(s) == c);
        public float HomecomingSeconds(ExpeditionSite from)
            => HomecomingBaseSeconds + (from == null ? 0 : Vector2.Distance(from.MapPosition, new Vector2(.5f, .5f)) * HomecomingDistanceSeconds);
        public float HomecomingSeconds(string id, Civilization c)
            => HomecomingSeconds(HeldCaptives(c).FirstOrDefault(p => p.PersonalState.id == id)?.Captor ?? HomeSiteOf(c));

        // 포획 공통 처리: 부상 없는 장수는 높은 확률로 부상, 이미 부상 중이면 그대로.
        public static void CaptureInjury(CommanderPersonalState state, CommanderTraits traits)
        {
            if (state != null && state.injuries.Count == 0 && UnityEngine.Random.value < CaptureInjuryChance) state.AddInjury(traits);
        }
        // 우리 장수가 상대 세력에 붙잡힘: 부상 판정 + 장비 압수(세력이 없으면 압수 기록 없이 장비 유지).
        public void CapturedBy(CommanderAnt c, Civilization captor)
        {
            if (c == null) return;
            CaptureInjury(c.PersonalState, c.Traits);
            if (captor == null) return;
            Data.seizures.Add(new Seizure { owner = c.PersonalState.id, item = "", holder = captor.id }); // 억류 세력 표식(몸값 협상 상대)
            foreach (var item in c.PersonalState.equipment)
            { captor.equipment.Add(item); Data.seizures.Add(new Seizure { owner = c.PersonalState.id, item = item.id, holder = captor.id }); }
            c.PersonalState.equipment.Clear();
        }
        // 재정복 구출: 그 장수에게서 압수한 장비가 아직 상대 보관 중이면 되돌려 받는다.
        public void Rescued(CommanderAnt c)
        {
            if (c == null) return;
            foreach (var s in Data.seizures.Where(s => s.owner == c.PersonalState.id).ToArray())
            {
                var civ = Data.civilizations.Find(x => x.id == s.holder); var item = s.item == "" ? null : civ?.equipment.Find(e => e.id == s.item);
                if (item != null && !c.PersonalState.equipment.Any(e => e.slot == item.slot)) { civ.equipment.Remove(item); c.PersonalState.equipment.Add(item); }
                Data.seizures.Remove(s);
            }
        }
        // 우리가 적 장수를 붙잡음: 부상 판정 + 장비를 우리 보관함으로 압수(넘치면 수용소 주변 바닥).
        public void Seized(Prisoner p, Vector3 position)
        {
            if (p?.PersonalState == null) return;
            CaptureInjury(p.PersonalState, p.Traits);
            foreach (var item in p.PersonalState.equipment) Data.seizures.Add(new Seizure { owner = p.PersonalState.id, item = item.id, holder = "player" });
            StoreEquipment(p.PersonalState.equipment, position);
            p.PersonalState.equipment.Clear();
        }
        public bool IsSeizedFrom(string itemId, string owner) => Data.seizures.Any(s => s.item == itemId && s.owner == owner);
        public string SeizedOwnerName(string itemId)
        {
            var s = Data.seizures.Find(x => x.item == itemId && itemId != ""); if (s == null) return null;
            return CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c.PersonalState.id == s.owner)?.CommanderName
                ?? PlayerPrisoners.Concat(Data.civilizations.SelectMany(c => c.rebels)).FirstOrDefault(p => p.PersonalState.id == s.owner)?.Name ?? "포로";
        }

        internal static Prisoner Snapshot(CommanderAnt commander) => new Prisoner(commander.CommanderName, CommanderRank.Sergeant, new[] { commander.Role }, commander.Traits)
        {
            Talents = commander.Talents.Copy(), PersonalState = commander.CapturePersonalState(), LabAttack = commander.LabAttackLevel,
            LabArmor = commander.LabArmorLevel, StrikeCooldown = commander.Skills.PowerStrikeCooldownLeft, StanceCooldown = commander.Skills.DefensiveStanceCooldownLeft
        };
        // 석방 성립: 억류된 장수 오브젝트를 정리하고 귀환 기록으로 옮긴다(도착 전까지 소굴 인원이 아님).
        private void BeginHomecoming(string id, Civilization c, List<EquipmentItem> gear)
        {
            var held = HeldCaptives(c).FirstOrDefault(p => p.PersonalState.id == id);
            Prisoner snapshot; ExpeditionSite from;
            if (held != null)
            {
                from = held.Captor; held.Captor.Defense.ReleaseCaptive(held);
                snapshot = Snapshot(held); snapshot.PersonalState.equipment.Clear();
                CommanderRoster.Instance.Forget(held); Destroy(held.gameObject);
            }
            else
            {
                snapshot = c.rebels.FirstOrDefault(p => p.PersonalState.id == id); from = HomeSiteOf(c);
                if (snapshot == null) return;
            }
            Data.homecomings.Add(new Homecoming { commander = snapshot, arrive = Data.elapsed + HomecomingSeconds(from), from = c.name, gear = gear });
            CampaignHistory.Record("석방", snapshot.Name, c.name + "에서 귀환 출발", true);
        }
        private void TickHomecomings()
        {
            foreach (var h in Data.homecomings.Where(h => h.arrive <= Data.elapsed).ToArray())
                if (Arrive(h)) Data.homecomings.Remove(h);
        }
        // 우리가 돌려보낸 적 장수: 우리 장수와 같은 시간(3개월 + 거리) 뒤 출신 세력에 도착해 그 세력 장수 기록으로 복귀한다.
        internal void BeginEnemyHomecoming(Prisoner p, Civilization c, List<EquipmentItem> gear)
        {
            Data.homecomings.Add(new Homecoming { commander = p, arrive = Data.elapsed + HomecomingSeconds(HomeSiteOf(c)), from = "우리 소굴", to = c.id, gear = gear });
            CampaignHistory.Record("석방", p.Name, c.name + "로 귀환 출발", true);
        }
        private bool Arrive(Homecoming h)
        {
            if (!string.IsNullOrEmpty(h.to))
            {
                var civ = Data.civilizations.Find(x => x.id == h.to);
                if (civ != null && !civ.extinct) { civ.prisoners.Add(h.commander); civ.equipment.AddRange(h.gear); }
                Data.seizures.RemoveAll(s => s.owner == h.commander?.PersonalState?.id);
                CampaignHistory.Record("귀환", h.commander?.Name, civ != null && !civ.extinct ? civ.name + " 도착" : "돌아갈 세력 소멸", true);
                return true;
            }
            var p = h.commander; var roster = CommanderRoster.Instance; if (roster == null || p == null) return false;
            var recruit = roster.Create(p.Name, p.Rank, p.Roles, p.Roles[0], p.Traits, world.HomePosition + Vector3.right * 3);
            if (recruit == null) return false;
            recruit.RestoreTalents(p.Talents); recruit.RestorePersonalState(p.PersonalState); recruit.WorkState.duty = CommanderDuty.Civilian;
            recruit.Social.departure = DepartureState.None; recruit.Social.pendingDeparture = false; recruit.PersonalState.departure = ""; recruit.PersonalState.originFaction = "";
            recruit.RestoreLabLevels(p.LabAttack, p.LabArmor); recruit.Skills.Restore(false, p.StrikeCooldown, p.StanceCooldown, 0);
            var extra = new List<EquipmentItem>();
            foreach (var item in h.gear) if (recruit.PersonalState.equipment.Any(e => e.slot == item.slot)) extra.Add(item); else recruit.PersonalState.equipment.Add(item);
            StoreEquipment(extra, world.HomePosition);
            Data.seizures.RemoveAll(s => s.owner == p.PersonalState.id);
            AntColony.UI.ToastManager.Show($"{p.Name} 귀환 ({h.from}) — 부상은 본거지에서 치료하세요.");
            CampaignHistory.Record("귀환", p.Name, h.from, true);
            return true;
        }

        // 자원 수령: 창고에 들어가는 만큼 저장, 넘치면 창고(없으면 비축더미) 주변 바닥에 두고 운반 장수가 옮긴다.
        internal static void StoreResource(ResourceType type, int amount, ResourceReason reason) => StoreResource(type, amount, reason, null);
        // near가 있으면 넘친 분량을 그 위치 주변 바닥에 둔다(재료 개보수 반환: 해당 건물 주변, 2026-10-10).
        internal static void StoreResource(ResourceType type, int amount, ResourceReason reason, Vector3? near)
        {
            var rm = ResourceManager.Instance; if (rm == null || amount <= 0) return;
            var fits = Mathf.Clamp(rm.GetCapacity(type) - rm.GetAmount(type), 0, amount);
            if (fits > 0) rm.Add(type, fits, reason);
            if (amount - fits <= 0) return;
            DropFloor(type, amount - fits, (near ?? StoragePosition()) + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f)));
        }
        // 바닥 물건(운반 작업 대상). 목장 바닥 생산물·먹이통 배출 등(2026-10-11).
        internal static void DropFloor(ResourceType type, int amount, Vector3 at)
        {
            if (amount <= 0) return;
            var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.name = "Received " + type; drop.SetActive(false);
            drop.transform.position = new Vector3(at.x, at.y + .3f, at.z);
            drop.transform.localScale = Vector3.one * .6f;
            drop.AddComponent<ResourceNode>().ConfigureLoot(type, amount);
            drop.AddComponent<ResourceNodeStatus>(); drop.SetActive(true);
        }
        // 바닥 장비는 장비 전리품으로 두고 운반 작업 장수가 보관함으로 옮긴다(우클릭 회수도 가능).
        internal static void StoreEquipment(IEnumerable<EquipmentItem> items, Vector3 fallback)
        {
            var inventory = EquipmentInventory.Instance; var extra = new List<EquipmentItem>();
            foreach (var item in items) { if (inventory != null && !inventory.Full) inventory.Items.Add(item); else extra.Add(item); }
            if (extra.Count > 0) EquipmentLoot.Drop(StoragePosition(fallback), extra);
        }
        private static Vector3 StoragePosition(Vector3? fallback = null)
        {
            var storage = FindObjectsByType<Storage>(FindObjectsSortMode.None).FirstOrDefault(s => s.isActiveAndEnabled && !s.IsDead);
            if (storage != null) return storage.Position + Vector3.right * 3;
            var deposit = BuildingBase.FindNearestDepositPoint(fallback ?? WorldMapManager.Instance?.HomePosition ?? Vector3.zero);
            return (deposit != null ? deposit.Position : fallback ?? Vector3.zero) + Vector3.right * 3;
        }
    }
}
