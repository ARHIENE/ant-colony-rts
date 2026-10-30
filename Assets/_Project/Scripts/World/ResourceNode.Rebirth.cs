using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 다이아 무중생유: 보유 장수가 재생하지 않는 자연 자원을 다 캐면 30초 재탐색 뒤 처음 양의 50%가 다시 나온다.
    // 노드당 1회, 다시 나온 자원은 누구나 채집한다.
    public partial class ResourceNode
    {
        public const float RebirthSeconds = 30, RebirthShare = .5f;
        public float PeakAmount { get; internal set; }
        public bool RebirthUsed { get; internal set; }
        public float RebirthTimer { get; internal set; }

        private void NotePeak() => PeakAmount = Mathf.Max(PeakAmount, amountRemaining);

        private void TryRebirth(WorkerAnt worker)
        {
            if (!IsDepleted || RebirthUsed || regrowSeconds > 0f || requiresFishing || IsLooseCargo
                || !(worker is CommanderAnt c) || !c.Traits.Has(CommanderTrait.DiamondGathering)) return;
            RebirthUsed = true; RebirthTimer = RebirthSeconds;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            AntColony.UI.ToastManager.Show($"{c.CommanderName}의 무중생유: 고갈된 자원을 다시 찾는 중({RebirthSeconds:0}초)");
        }

        private void TickRebirth(float dt)
        {
            if (RebirthTimer <= 0f) return;
            RebirthTimer = Mathf.Max(0f, RebirthTimer - dt);
            if (RebirthTimer <= 0f) amountRemaining = Mathf.Max(1f, PeakAmount * RebirthShare);
        }

        internal void RestoreRebirth(bool used, float timer, float peak)
        {
            RebirthUsed = used; RebirthTimer = Mathf.Clamp(timer, 0f, RebirthSeconds); PeakAmount = Mathf.Max(0f, peak);
            if (RebirthTimer > 0f && !gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
