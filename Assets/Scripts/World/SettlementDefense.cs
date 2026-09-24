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
        public IReadOnlyList<WildMonster> Attackers => attackers;
        public IReadOnlyList<CommanderAnt> Prisoners => prisoners;
        public bool UnderAttack { get; private set; }
        // 난이도는 습격 간격과 병력 수만 바꾼다. Normal이면 배수 1.0이라 기존과 완전히 같다.
        public static float CurrentRaidInterval => RaidInterval * AntColony.Core.DifficultyRuntime.IntervalScale;
        public float Remaining { get; private set; } = RaidInterval;
        public float CaptureProgress { get; private set; }
        public string Status => site.Disposition == ConquestDisposition.Lost
            ? $"Lost | Captives {prisoners.Count}. Defeat occupiers and resolve conquest to rescue."
            : UnderAttack ? $"UNDER ATTACK: {attackers.Count} | Landing occupied {CaptureProgress:0}/{CaptureSeconds:0}s"
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
            CaptureProgress = 0;
            Remaining = CurrentRaidInterval;
            attackers.Clear();
        }

        // 저장 복원 전용. 진행 중이던 습격 부대는 저장 자체를 거절하므로 여기서는 타이머만 되돌린다.
        internal void RestoreState(float remaining, float captureProgress)
        {
            UnderAttack = false;
            attackers.Clear();
            Remaining = Mathf.Clamp(remaining, 0f, CurrentRaidInterval);
            CaptureProgress = Mathf.Clamp(captureProgress, 0f, CaptureSeconds);
        }

        internal void RestorePrisoner(CommanderAnt commander)
        {
            if (commander == null || prisoners.Contains(commander)) return;
            commander.Captor = site;
            prisoners.Add(commander);
            commander.gameObject.SetActive(false);
        }

        public bool TryStartRaid()
        {
            if (!isActiveAndEnabled || site.Disposition != ConquestDisposition.Annexed
                || UnderAttack || site.GuardTemplate == null) return false;
            // 거점 북쪽에서 착륙 지점으로 진격. 본거지 건물이나 수송수단을 공격 대상으로 삼지 않는다.
            if (!NavMesh.SamplePosition(site.transform.position + Vector3.forward * 28,
                out var hit, 4, NavMesh.AllAreas)) return false;
            var waveSize = AntColony.Core.DifficultyRuntime.ScaleCount(site.Difficulty);
            for (var i = 0; i < waveSize; i++)
            {
                var enemy = Instantiate(site.GuardTemplate, hit.position, Quaternion.identity, site.transform);
                enemy.name = site.Title + " Invader " + (i + 1);
                enemy.RaidSettlement(site);
                enemy.gameObject.SetActive(true);
                attackers.Add(enemy);
            }
            UnderAttack = true;
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
                Remaining = Mathf.Max(0, Remaining - seconds);
                if (Remaining == 0) TryStartRaid();
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
                    c.Captor = site;
                    prisoners.Add(c);
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
