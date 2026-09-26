using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 편입 거점의 침공·점령·포로만 담당한다. 전투와 생산은 기존 컴포넌트를 사용한다.
    public sealed class SettlementDefense : MonoBehaviour
    {
        // ponytail: 주기/점령 시간/포로 확률은 임시 밸런스. 확정 후 데이터 에셋으로 옮긴다.
        public const float RaidInterval = 300f;
        public const float CaptureSeconds = 30f;
        [SerializeField, Range(0, 1)] private float captureChance = .5f;
        private readonly List<WildMonster> attackers = new List<WildMonster>();
        private readonly List<CommanderAnt> prisoners = new List<CommanderAnt>();
        private ExpeditionSite site;
        private readonly List<EnemyCommander> allies = new List<EnemyCommander>();
        private string attackerFaction;
        public IReadOnlyList<WildMonster> Attackers => attackers;
        public IReadOnlyList<CommanderAnt> Prisoners => prisoners;
        public bool UnderAttack { get; private set; }
        // 난이도는 습격 간격과 병력 수만 바꾼다. Normal이면 배수 1.0이라 기존과 완전히 같다.
        public static float CurrentRaidInterval => RaidInterval * AntColony.Core.DifficultyRuntime.IntervalScale;
        public float Remaining { get; private set; } = RaidInterval;
        public float CaptureProgress { get; private set; }
        // 감시탑이 이 거점을 감시하면 침공 60초 전에 경보가 켜지고, 월드맵에 진격 경로를 표시한다.
        public bool Warned { get; private set; }
        public string Status => site.Disposition == ConquestDisposition.Lost
            ? $"Lost | Captives {prisoners.Count}. Defeat occupiers and resolve conquest to rescue."
            : UnderAttack ? $"UNDER ATTACK: {attackers.Count} | Landing occupied {CaptureProgress:0}/{CaptureSeconds:0}s"
            : Warned ? $"WATCHTOWER ALERT: raid in {Remaining:0}s | Route: north edge -> landing"
            : $"Defense ready | Next raid {Remaining:0}s | Hold within 8m of landing.";

        private void Awake()
        {
            site = GetComponent<ExpeditionSite>();
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "LandingDefenseZone";
            marker.transform.SetParent(transform);
            marker.transform.position = site.Landing + Vector3.up * .04f;
            marker.transform.localScale = new Vector3(16, .02f, 16);
            Destroy(marker.GetComponent<Collider>());
            var color = new MaterialPropertyBlock();
            color.SetColor(AntColony.Boss.BossLoot.BaseColorId, new Color(.35f, .4f, .2f));
            color.SetColor(AntColony.Boss.BossLoot.ColorId, new Color(.35f, .4f, .2f));
            marker.GetComponent<Renderer>().SetPropertyBlock(color);
        }
        private void Update() => Tick(Time.deltaTime);

        internal void ResetAfterConquest()
        {
            UnderAttack = false;
            Warned = false;
            CaptureProgress = 0;
            Remaining = DiplomacyManager.Instance?.ScheduledRaidSeconds(site) ?? CurrentRaidInterval;
            attackers.Clear();
            foreach (var ally in allies) if (ally != null) Destroy(ally.gameObject);
            allies.Clear();
        }

        // 저장 복원 전용. 진행 중이던 습격 부대는 저장 자체를 거절하므로 여기서는 타이머만 되돌린다.
        internal void RestoreState(float remaining, float captureProgress)
        {
            UnderAttack = false;
            attackers.Clear();
            Remaining = Mathf.Clamp(remaining, 0f, DiplomacyManager.Instance != null ? DiplomacyRules.Month * 3 : CurrentRaidInterval);
            CaptureProgress = Mathf.Clamp(captureProgress, 0f, CaptureSeconds);
            Warned = Remaining <= GameBalance.WatchtowerWarningSeconds && AntColony.Buildings.Watchtower.Watches(site);
        }

        internal void RestorePrisoner(CommanderAnt commander)
        {
            if (commander == null || prisoners.Contains(commander)) return;
            commander.Captor = site;
            prisoners.Add(commander);
            commander.gameObject.SetActive(false);
        }

        public bool TryStartRaid(int requestedCount = 0, string factionId = null)
        {
            if (!isActiveAndEnabled || site.Disposition != ConquestDisposition.Annexed
                || UnderAttack || site.GuardTemplate == null) return false;
            if (DiplomacyManager.Instance != null && !DiplomacyManager.Instance.Data.civilizations.Exists(c => c.war && !c.extinct)) return false;
            if (factionId == null) factionId = DiplomacyManager.Instance?.Data.civilizations.Find(c => c.war && !c.extinct)?.id;
            attackerFaction = factionId;
            // 거점 북쪽에서 착륙 지점으로 진격. 본거지 건물이나 수송수단을 공격 대상으로 삼지 않는다.
            if (!NavMesh.SamplePosition(site.transform.position + Vector3.forward * 28,
                out var hit, 4, NavMesh.AllAreas)) return false;
            var waveSize = requestedCount > 0 ? requestedCount : AntColony.Core.DifficultyRuntime.ScaleCount(site.Difficulty);
            for (var i = 0; i < waveSize; i++)
            {
                var enemy = Instantiate(site.GuardTemplate, hit.position, Quaternion.identity, site.transform);
                enemy.name = site.Title + " Invader " + (i + 1);
                enemy.RaidSettlement(site);
                enemy.DiplomaticFactionId = factionId;
                enemy.gameObject.SetActive(true);
                attackers.Add(enemy);
            }
            UnderAttack = true;
            if (DiplomacyManager.Instance != null)
                foreach (var c in DiplomacyManager.Instance.Data.civilizations)
                    if (c.HasTreaty(TreatyKind.Alliance, DiplomacyManager.Instance.Data.elapsed))
                    {
                        var ally = Instantiate(site.GuardTemplate, site.Landing, Quaternion.identity, site.transform);
                        ally.name = c.name + " 동맹 수비 장수"; ally.Allied = true;
                        ally.gameObject.SetActive(true);
                        var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", c.color); block.SetColor("_Color", c.color);
                        foreach (var renderer in ally.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
                        allies.Add(ally);
                    }
            CaptureProgress = 0;
            Notify("Under attack — defend the landing zone!");
            return true;
        }

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !(seconds > 0) || float.IsInfinity(seconds)
                || site.Disposition != ConquestDisposition.Annexed) return;
            if (!UnderAttack)
            {
                var before = Remaining;
                Remaining = DiplomacyManager.Instance != null ? DiplomacyManager.Instance.ScheduledRaidSeconds(site) : Mathf.Max(0, Remaining - seconds);
                if (!Warned && before > GameBalance.WatchtowerWarningSeconds && Remaining <= GameBalance.WatchtowerWarningSeconds
                    && AntColony.Buildings.Watchtower.Watches(site))
                {
                    Warned = true;
                    Notify($"Watchtower: raid in {Remaining:0}s from the north edge.");
                    AntColony.UI.ToastManager.Show(site.Title + $": watchtower alert, raid in {Remaining:0}s.");
                }
                if (Remaining == 0 && DiplomacyManager.Instance == null) TryStartRaid();
                return;
            }
            attackers.RemoveAll(a => a == null || a.IsDead);
            if (attackers.Count == 0)
            {
                ResetAfterConquest();
                Notify("Defense successful.");
                return;
            }
            var occupied = false;
            foreach (var enemy in attackers)
                if (enemy.isActiveAndEnabled && (enemy.Position - site.Landing).sqrMagnitude <= 36) occupied = true;
            foreach (var unit in AntUnitBase.Active)
                if (unit is CommanderAnt c && c.HasTroops && !c.IsEmbarked
                    && (c.Position - site.Landing).sqrMagnitude <= 64) occupied = false;
            CaptureProgress = occupied ? CaptureProgress + seconds : 0;
            if (CaptureProgress >= CaptureSeconds) Lose();
        }

        private void Lose()
        {
            site.LoseSettlement();
            var faction = DiplomacyManager.Instance?.Data.civilizations.Find(c => c.id == attackerFaction);
            if (faction != null) faction.enemyScore += 50;
            UnderAttack = false;
            var escaped = 0;
            foreach (var c in new List<CommanderAnt>(site.Settlement.Garrison))
            {
                if (c == null) continue;
                c.CommandStop();
                c.EvacuateCargo(site);
                site.Settlement.Remove(c);
                if (Random.value < captureChance)
                {
                    c.TakeDamage(float.MaxValue);
                    if (c.IsDead) continue;
                    c.Captor = site;
                    c.OnCaptured();
                    prisoners.Add(c);
                    CampaignHistory.Record("포로", c.CommanderName, site.Title + " 함락");
                    c.gameObject.SetActive(false);
                }
                else
                {
                    MoveCommander(c, WorldMapManager.Instance.HomePosition + Vector3.right * (3 + escaped));
                    escaped++;
                }
            }
            var world = WorldMapManager.Instance;
            foreach (var ship in world.Transports)
                if (ship != null && ship.Route?.Destination == site) ship.Route.Stop("Stopped: destination lost");
            site.Visitor?.EvacuateLostSite();
            if (world.ViewedSite == site) world.ViewSite(null);
            Notify($"LOST — {prisoners.Count} captured, {escaped} escaped. Retake to rescue.");
        }

        internal void RescuePrisoners()
        {
            var rescued = prisoners.Count;
            foreach (var c in prisoners)
            {
                if (c == null) continue;
                c.Captor = null;
                c.OnRescued();
                CampaignHistory.Record("구출", c.CommanderName, site.Title);
                var annexed = site.Disposition == ConquestDisposition.Annexed;
                MoveCommander(c, annexed ? site.Landing + Vector3.right * 3 : WorldMapManager.Instance.HomePosition + Vector3.right * 3);
                if (annexed) site.Settlement.Add(c);
            }
            prisoners.Clear();
            if (rescued > 0) Notify($"{rescued} commanders rescued without troops.");
        }

        private static void MoveCommander(CommanderAnt c, Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas)) position = hit.position;
            c.Agent.enabled = false;
            c.transform.position = position;
            c.gameObject.SetActive(true);
            c.SetEmbarked(false, position);
        }

        private void Notify(string message) => WorldMapManager.Instance.SettlementNotice = site.Title + ": " + message;
    }
}
