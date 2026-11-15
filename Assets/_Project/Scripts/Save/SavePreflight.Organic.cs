using System;
using AntColony.World;

namespace AntColony.Save
{
    public static partial class SavePreflight
    {
        static void ValidateOrganic(SaveFileV1 file)
        {
            foreach (var monster in file.monsters)
            {
                var state = monster.runtime?.organic;
                if (state == null) continue;
                Check(OrganicEnemyRules.Valid((OrganicEnemyKind)state.kind) && !monster.runtime.commander, "생물형 종류");
                Check(state.phase >= 0 && state.phase <= 6 && state.spawnCharges >= 0 && state.spawnCharges <= 2
                    && N(state.phaseLeft) && state.phaseLeft <= 3 && N(state.abilityCooldown) && state.abilityCooldown <= 12
                    && N(state.healLock) && state.healLock <= 4 && N(state.mistLeft) && state.mistLeft <= 5
                    && N(state.burrowCooldown) && state.burrowCooldown <= 12
                    && V(new Vec3Dto(state.aim)) && V(new Vec3Dto(state.corpsePosition)), "생물형 행동 상태");
            }
            foreach (var corpse in file.corpses)
                Check(corpse.organicKind == 0 || OrganicEnemyRules.Valid((OrganicEnemyKind)corpse.organicKind)
                    && corpse.kind == CorpseKind.Wildlife && corpse.food == 0 && N(corpse.organicYaw) && corpse.organicYaw < 360, "생물형 사체");
            if (file.organicEffects == null) return;
            Check(file.organicEffects.Count <= 10000, "생체 공격 개수");
            foreach (var effect in file.organicEffects)
                Check(effect != null && OrganicEnemyRules.Valid((OrganicEnemyKind)effect.species)
                    && effect.mode >= 0 && effect.mode <= 1 && effect.ticks >= 0 && effect.ticks <= 4
                    && N(effect.left) && effect.left <= 2 && N(effect.duration) && effect.duration > 0 && effect.duration <= 2
                    && N(effect.damage) && N(effect.radius) && effect.radius <= 2
                    && V(new Vec3Dto(effect.start)) && V(new Vec3Dto(effect.end)), "생체 공격 진행 상태");
        }
    }
}
