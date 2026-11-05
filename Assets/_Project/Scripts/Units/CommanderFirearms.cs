using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        // One attack cycle: a single round, three-round burst, or five shotgun pellets.
        // Damage is resolved by simulation; animation cannot produce additional hits.
        private void FireEquipment(IDamageable primary, float damage)
        {
            var kind = Weapon.weapon;
            var aim = primary.Position - Position;
            aim.y = 0;
            if (aim.sqrMagnitude < .001f) aim = transform.forward;
            aim.Normalize();
            var targets = SceneObjects.All<MonoBehaviour>().OfType<IDamageable>()
                .Where(t => CombatTargeting.CanAttack(Role, t)).ToArray();
            int rounds = kind == WeaponKind.BurstCarbine ? 3 : kind == WeaponKind.Scattergun ? 5 : 1;
            float perRound = damage * (kind == WeaponKind.BurstCarbine ? .45f : kind == WeaponKind.Scattergun ? .35f : kind == WeaponKind.SniperRifle ? 2f : 1.4f);
            for (int shot = 0; shot < rounds; shot++)
            {
                var direction = Quaternion.Euler(0, kind == WeaponKind.Scattergun ? (shot - 2) * 7f : 0, 0) * aim;
                var line = targets.Where(t => CombatTargeting.CanAttack(Role, t))
                    .Select(t => (target: t, along: Vector3.Dot(t.Position - Position, direction), position: t.Position))
                    .Where(x => x.along > 0 && x.along <= AttackRange && Vector3.Cross(direction, x.position - Position).magnitude <= PierceWidth)
                    .OrderBy(x => x.along).ToArray();
                bool pierces = traits.Has(CommanderTrait.DiamondPierce) && (kind != WeaponKind.Scattergun || shot == 2);
                var roundDamage = perRound;
                foreach (var hit in line)
                {
                    var alive = CombatTargeting.IsAlive(hit.target);
                    FormationDamage.Apply(hit.target, roundDamage, Position, true);
                    if (alive && !CombatTargeting.IsAlive(hit.target)) GainExperience(CombatActivity, CommanderProgression.ClaimKillXp(hit.target));
                    if (!pierces) break;
                    roundDamage *= PierceFalloff;
                }
            }
        }
    }
}
