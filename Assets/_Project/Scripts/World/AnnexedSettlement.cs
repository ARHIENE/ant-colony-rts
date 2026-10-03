using System.Collections.Generic;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 편입 후의 생산과 주둔 명부. 재고는 기존 소굴 노드에 남기고 본거지로 직접 보내지 않는다.
    public class AnnexedSettlement : MonoBehaviour
    {
        // ponytail: 생산량/주기는 임시 밸런스다. 거점별 생산 기획 확정 시 데이터로 옮긴다.
        public const float ProductionSeconds = 60f;
        private float elapsed;
        private readonly List<CommanderAnt> garrison = new List<CommanderAnt>();
        public IReadOnlyList<CommanderAnt> Garrison => garrison;
        public ExpeditionSite Site { get; private set; }
        public ExpeditionTransport DockedTransport => Site != null && Site.Visitor != null
            && Site.Visitor.isActiveAndEnabled && Site.Visitor.State == ExpeditionState.Deployed ? Site.Visitor : null;

        private void Awake() => Site = GetComponent<ExpeditionSite>();
        private void Update() => Tick(Time.deltaTime);

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || Site == null || Site.Disposition != ConquestDisposition.Annexed
                || Site.Colony == null || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            elapsed += seconds;
            var periods = Mathf.Floor(elapsed / ProductionSeconds);
            if (periods < 1) return;
            elapsed %= ProductionSeconds;
            // 한 번에 긴 시간이 지나도 기존 재고 상한 이상을 더할 필요는 없다.
            Site.Colony.AddResources((int)(Mathf.Min(periods * 10, 300) * Site.Difficulty),
                (int)(Mathf.Min(periods * 5, 200) * Site.Difficulty));
        }

        // 저장 복원 전용.
        internal void RestoreElapsed(float seconds) => elapsed = Mathf.Clamp(seconds, 0f, ProductionSeconds);
        internal float Elapsed => elapsed;

        internal void Add(CommanderAnt commander)
        {
            garrison.Add(commander);
            commander.Garrison = this;
        }

        internal void Remove(CommanderAnt commander)
        {
            garrison.Remove(commander);
            if (commander.Garrison == this) commander.Garrison = null;
        }
    }
}
