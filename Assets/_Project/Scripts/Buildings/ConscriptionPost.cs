using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class ConscriptionPost : BuildingBase
    {
        public bool TryDeploy(IReadOnlyList<CommanderAnt> commanders, IReadOnlyList<int> troops)
        {
            if (!isActiveAndEnabled || IsDead || !CountsTowardPlayerDefeat || commanders == null || troops == null
                || commanders.Count == 0 || commanders.Count != troops.Count || AntPool.Instance == null) return false;
            var seen = new HashSet<CommanderAnt>(); long total = 0;
            // Keep the rally point outside the footprint, including the frame before NavMesh carving finishes.
            if (!UnityEngine.AI.NavMesh.SamplePosition(Position + Vector3.forward * 4, out var rally, 2, UnityEngine.AI.NavMesh.AllAreas)) return false;
            for (var i = 0; i < commanders.Count; i++)
            {
                var c = commanders[i];
                if (c == null || !seen.Add(c) || !c.CanMobilize || troops[i] <= 0 || troops[i] > c.CommandLimit || !c.CanReach(rally.position)) return false;
                total += troops[i];
            }
            // Phase 4: 병역 제도 상한·민심 바닥 확인(병역 나이 확대 시 늙은 개미로 보충).
            var population = ColonyPopulation.Instance;
            if (population != null && !population.TryDraft((int)total, AntColony.UI.EnemyAlert.CrisisActive)) return false;
            if (total > AntPool.Instance.Free || !AntPool.Instance.TryAssign((int)total)) return false;
            for (var i = 0; i < commanders.Count; i++) commanders[i].Mobilize(troops[i], rally.position);
            return true;
        }
    }
}
