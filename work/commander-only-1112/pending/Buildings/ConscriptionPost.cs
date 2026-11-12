using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class ConscriptionPost : BuildingBase
    {
        public int AvailableTroops(CommanderAnt commander) => 0; // Compatibility only.

        // 2026-11-12 장수 단일 유닛: 시민 병력 없이 장수만 소집한다. 인구·병역 확인은 하지 않는다.
        public bool CanDeploy(CommanderAnt commander) => isActiveAndEnabled && !IsDead && CountsTowardPlayerDefeat && commander != null && commander.CanMobilize;
        public bool TryDeploy(IReadOnlyList<CommanderAnt> commanders)
        {
            if (!isActiveAndEnabled || IsDead || !CountsTowardPlayerDefeat || commanders == null || commanders.Count == 0) return false;
            if (!UnityEngine.AI.NavMesh.SamplePosition(Position + Vector3.forward * 4, out var rally, 2, UnityEngine.AI.NavMesh.AllAreas)) return false;
            var seen = new HashSet<CommanderAnt>();
            foreach (var c in commanders)
                if (c == null || !seen.Add(c) || !c.CanMobilize || !c.CanReach(rally.position)) return false;
            foreach (var c in commanders) c.Mobilize(0, rally.position);
            return true;
        }

        // Legacy callers also mobilize commanders without consuming the old population pool.
        public bool TryDeploy(IReadOnlyList<CommanderAnt> commanders, IReadOnlyList<int> troops)
            => TryDeploy(commanders);
    }
}
