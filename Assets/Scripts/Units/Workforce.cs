using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 대상마다 요청 수만 저장한다. 실제 개미는 일하는 장수가 있을 때 풀에서 빌리고, 중단 시 반환한다.
    public sealed class Workforce : MonoBehaviour
    {
        public const int Maximum = GameBalance.WorkforceBase + GameBalance.WorkforcePerSkill * CommanderTalents.MaxLevel;
        public int Requested { get; private set; }
        public int Allocated { get; private set; }
        public int Limit { get; private set; }
        private AntPool pool;
        public static Workforce For(Component target) => target == null ? null
            : target.GetComponent<Workforce>() ?? target.gameObject.AddComponent<Workforce>();
        public void Request(int count) { Requested = Mathf.Clamp(count, 0, Maximum); Refresh(); }
        public float Multiplier => 1 + Allocated * GameBalance.WorkforcePerAnt;
        private void Update() => Refresh();
        public void Refresh()
        {
            Limit = 0;
            if (isActiveAndEnabled && !AntColony.Save.SaveSystem.Busy)
                foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>())
                    if (c.WorkTarget == this) Limit = Mathf.Max(Limit, c.WorkforceLimit);
            var next = Mathf.Min(Requested, Limit);
            if (pool == null) { pool = AntPool.Instance; Allocated = 0; }
            if (pool == null) return;
            if (next < Allocated) { pool.ReturnWorkers(Allocated - next); Allocated = next; }
            if (next > Allocated) { int amount = Mathf.Min(next - Allocated, pool.Free); if (pool.TryAssignWorkers(amount)) Allocated += amount; }
        }
        private void OnDisable() { if (pool != null) pool.ReturnWorkers(Allocated); Allocated = Limit = 0; }
    }
}
