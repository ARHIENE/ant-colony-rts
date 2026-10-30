using System;
using System.Text;
using System.Threading.Tasks;

public static class Review1029CombatChecks
{
    private sealed class Target : AntColony.Core.IDamageable, AntColony.Core.IAirborne
    {
        public bool IsDead => false;
        public bool IsAirborne { get; set; }
        public UnityEngine.Vector3 Position => default;
        public void TakeDamage(float amount) { }
    }
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
        var report = new StringBuilder();
        foreach (float raw in new[] { .2f, 1f, 5f, 8f, 25f, 100f })
            foreach (float armor in new[] { 0f, 4f, 8f, 20f, 100f })
                foreach (float reduction in new[] { 0f, .2f, .75f, 2f })
                {
                    float multiplier = AntColony.Units.FormationRules.ReceivedMultiplier(0, reduction);
                    float damage = AntColony.Core.FormationDamage.AfterArmor(raw, armor, multiplier);
                    float expected = Math.Max(Math.Min(1, raw), raw - armor) * (1 - Math.Min(.75f, reduction));
                    Check(Math.Abs(damage - expected) < .0001f, $"damage {raw}/{armor}/{reduction}");
                    Check(damage > 0 && damage <= raw, "positive finite damage");
                }
        foreach (int troops in new[] { 1, 5, 10, 20 })
        {
            // 실제 공유 피해 공식으로 병력 1HP·개인100HP·병력수 공격을 시간순 적분한다.
            double enemyHp = 1000, health = 100, troopHp = troops, time = 0;
            while (health > 0 && enemyHp > 0 && time < 5000)
            {
                enemyHp -= 10 * Math.Max(1, Math.Ceiling(troopHp));
                float incoming = AntColony.Core.FormationDamage.AfterArmor(8, 4);
                double absorbed = Math.Min(troopHp, incoming); troopHp -= absorbed; health -= incoming - absorbed;
                time++;
            }
            report.AppendLine($"{troops} troops, incoming8/armor4 every1s: {(enemyHp <= 0 ? "win" : "downed")} at {time}s, enemyHP={Math.Max(0, enemyHp):0}");
            Check(time <= 30, "attrition resolves by commander HP budget");
        }
        var air = new Target { IsAirborne = true };
        Check(AntColony.Core.CombatTargeting.IsAirborne(air), "shared airborne interface");
        Check(!AntColony.Core.CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, air), "non scene targets are never attackable");
        foreach (AntColony.Units.Formation f in AntColony.Units.FormationRules.All)
        {
            var m = AntColony.Units.FormationRules.Modifiers(f, front: true, withdrawing: true, ranged: true, hits: 4);
            Check(1 + m.Attack > 0 && 1 + m.Move > 0 && 1 + m.AttackSpeed > 0, "formation valid " + f);
        }
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Swift).Move
            > AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Column).Move, "swift escapes faster than column");
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Wedge, front: true).Attack
            > AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Arrow).Attack, "wedge frontal advantage");
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Wedge).Attack == 0, "wedge flank loses advantage");
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Cover, withdrawing: true).RangedReduction
            > AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Crane).RangedReduction, "cover withdrawal advantage");
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Cover).RangedReduction == 0, "cover requires withdrawal");
        Check(AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Snake, forestOrMountain: true).Attack > 0
            && AntColony.Units.FormationRules.Modifiers(AntColony.Units.Formation.Snake).Attack < 0, "snake terrain tradeoff");
        var state = new AntColony.Units.FormationState(); state.Hit("boss"); state.Hit("boss");
        Check(state.hits == 2, "same target accumulates"); state.Target("other"); Check(state.hits == 0, "target change resets focus");
        state.Hit("boss"); state.Tick(3); Check(state.hits == 0, "focus expires");
        Check(Math.Abs(AntColony.Units.FormationRules.Stat(10, 0, .3f) - 13) < .001, "equipment/training basis receives formation once");
        return $"PASS {checks} combat checks\n" + report;
    }
}
