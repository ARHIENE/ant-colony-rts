using System.Linq;
using AntColony.Core;
using AntColony.Map;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        private const float HomeThreatRadius = 25f; // 잠정
        internal static bool HomeHasEnemies()
        {
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            bool AtHome(Vector3 p) => bounds.Contains(new Vector3(p.x, 0, p.z));
            // 맵을 떠도는 일반 야생 동물 때문에 영영 하역 못 하지 않도록, 습격대는 맵 어디든·야생 동물은 귀환 지점 근처만 센다.
            var anchor = HomeReturnPoint.Anchor;
            return WildMonster.All.Any(m => m != null && m.Threatening && AtHome(m.Position)
                    && (m.IsRaider || m is EnemyCommander || (m.Position - anchor).sqrMagnitude <= HomeThreatRadius * HomeThreatRadius)
                    && CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, m))
                || AntUnitBase.Active.Any(u => u is CommanderAnt c && c.IsHostile && AtHome(c.Position)
                    && CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, c));
        }

        private bool TryHomeLanding(out Vector3 point)
        {
            point = HomeReturnPoint.Anchor;
            // 도보 귀환대는 길목으로 돌아와 직접 교전한다. 별도 맞이 호위전을 요구하지 않는다.
            if (!Walking && HomeHasEnemies()) return false;
            return Aircraft ? HomeReturnPoint.TryAir(out point) : HomeReturnPoint.TryGround(Position, out point);
        }
    }
}
