using System;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class WildMonster
    {
        OrganicCombatState organic;
        OrganicEnemyVisual organicVisual;
        IDamageable organicTarget;
        const int Strike = 1, Mend = 2, ReleaseBrood = 3, Consume = 4, Mist = 5, Burrow = 6;
        public OrganicEnemyKind OrganicKind => organic == null ? OrganicEnemyKind.None : (OrganicEnemyKind)organic.kind;
        public bool IsOrganic => OrganicEnemyRules.Valid(OrganicKind);
        public int OrganicPhase => organic?.phase ?? 0;
        public int OrganicSpawnCharges => organic?.spawnCharges ?? 0;
        public float OrganicMistLeft => organic?.mistLeft ?? 0;
        public void ConfigureOrganic(OrganicEnemyKind kind, bool keepEarlyStats = false)
        {
            if (!OrganicEnemyRules.Valid(kind) || this is EnemyCommander) throw new ArgumentException("생물형 일반 적만 구성할 수 있습니다.");
            organic = new OrganicCombatState { kind = (int)kind };
            if (!keepEarlyStats)
            {
                attackRange = OrganicEnemyRules.Range(kind);
                attackInterval = OrganicEnemyRules.Ranged(kind) ? 2.8f : 2;
                moveSpeed = kind == OrganicEnemyKind.HuntRider ? 3.3f : kind == OrganicEnemyKind.SiegeBrooder || kind == OrganicEnemyKind.Bulwark ? 1.7f : 2.5f;
                if (kind == OrganicEnemyKind.WallRam || kind == OrganicEnemyKind.WallMelter) Aim = EnemyAim.Wall;
                if (kind == OrganicEnemyKind.BreachBeast) Aim = EnemyAim.Building;
            }
            RestoreOrganicVisual();
        }
        void RestoreOrganicVisual()
        {
            if (!IsOrganic) return;
            organicVisual = OrganicEnemyVisual.Attach(this);
            Agent.baseOffset = 0; Agent.speed = moveSpeed; Agent.stoppingDistance = attackRange;
        }
        internal void RestoreOrganic(OrganicCombatState state, bool targets = false)
        {
            if (targets)
            {
                organicTarget = state == null ? null : SaveReferences.Get<IDamageable>(state.target);
                return;
            }
            organic = state == null ? null : JsonUtility.FromJson<OrganicCombatState>(JsonUtility.ToJson(state));
            if (organic != null && !OrganicEnemyRules.Valid((OrganicEnemyKind)organic.kind)) throw new InvalidOperationException("알 수 없는 생물형 적 저장 ID");
            if (!IsOrganic) return;
            RestoreOrganicVisual();
            if (organic.phase != 0)
            {
                float duration = organic.phase == Strike ? .74f : organic.phase == Mend ? 2.2f : organic.phase == Burrow ? 3 : 1.2f;
                organicVisual.Act(organic.phase == Mend ? 5 : 2, organic.phaseLeft, Mathf.Clamp01((duration - organic.phaseLeft) / (organic.phase == Mend ? 3 : 1.4f)));
            }
        }
        OrganicCombatState CaptureOrganic()
        {
            if (!IsOrganic) return null;
            var result = JsonUtility.FromJson<OrganicCombatState>(JsonUtility.ToJson(organic));
            result.target = TargetAlive(organicTarget) ? SaveReferences.Key(organicTarget) : null;
            return result;
        }
        void OrganicHurt()
        {
            if (!IsOrganic) return;
            // 피격은 준비/회복 채널만 취소한다. 이미 발사된 투사체와 다음 공격 시각은 유지한다.
            organic.phase = 0; organic.phaseLeft = 0; organicTarget = null;
            organicVisual?.Act(3, .6f);
        }
        bool TickOrganic(float dt)
        {
            if (!IsOrganic) return false;
            attackTimer = Mathf.Max(0, attackTimer - dt);
            organic.abilityCooldown = Mathf.Max(0, organic.abilityCooldown - dt);
            organic.healLock = Mathf.Max(0, organic.healLock - dt);
            organic.mistLeft = Mathf.Max(0, organic.mistLeft - dt);
            organic.burrowCooldown = Mathf.Max(0, organic.burrowCooldown - dt);
            if (organic.phase != 0)
            {
                if (organic.phase == Burrow && organic.phaseLeft > .8f && TargetAlive(currentTarget) && !IsBarrier(currentTarget) && CanMove())
                { agent.speed = moveSpeed; RequestDestination(currentTarget.Position); }
                else StopMoving();
                if (organic.phase == Strike && (!TargetAlive(organicTarget) || TargetDistance(organicTarget) > attackRange + .25f))
                { organic.phase = 0; organic.phaseLeft = 0; organicTarget = null; organicVisual?.Act(0, 0); return false; }
                organic.phaseLeft = Mathf.Max(0, organic.phaseLeft - dt);
                if (organic.phaseLeft <= 0)
                {
                    int phase = organic.phase; organic.phase = 0;
                    if (phase == Strike) ResolveOrganicStrike();
                    else if (phase == Mend) MendNearby();
                    else if (phase == ReleaseBrood) ReleaseYoung();
                    else if (phase == Consume) ConsumeCorpse();
                    else if (phase == Mist) organic.mistLeft = 5;
                    organicTarget = null;
                }
                return true;
            }
            if (organic.abilityCooldown > 0 || RootRemaining > 0) return false;
            if (OrganicKind == OrganicEnemyKind.BloomMender && HasMendTarget()) { BeginAbility(Mend, 2.2f, 5); return true; }
            if (OrganicKind == OrganicEnemyKind.CarrionReclaimer && currentHealth < maxHealth && FindOrganicCorpse() is Corpse corpse)
            { organic.corpsePosition = corpse.Position; BeginAbility(Consume, 1.2f, 6); return true; }
            if (!TargetAlive(currentTarget) || Vector3.Distance(Position, currentTarget.Position) > 12) return false;
            if (OrganicKind == OrganicEnemyKind.BroodCarrier && organic.spawnCharges > 0) { BeginAbility(ReleaseBrood, 1.2f, 8); return true; }
            if (OrganicKind == OrganicEnemyKind.VeilWeaver) { BeginAbility(Mist, 1.2f, 9); return true; }
            if (OrganicKind == OrganicEnemyKind.BurrowLurker && organic.burrowCooldown <= 0)
            { organic.burrowCooldown = 12; BeginAbility(Burrow, 3, 4); return true; }
            return false;
        }
        void BeginAbility(int phase, float duration, float cooldown)
        {
            StopMoving(); organic.phase = phase; organic.phaseLeft = duration; organic.abilityCooldown = cooldown;
            organicVisual?.Act(phase == Mend ? 5 : 2, phase == Mend ? 3 : 1.4f);
        }
        float TargetDistance(IDamageable target)
        {
            if (target is Component component && target is BuildingBase && component.GetComponent<Collider>() is Collider collider)
                return Vector3.Distance(Position, collider.ClosestPoint(Position));
            return Vector3.Distance(Position, target.Position);
        }
        void BeginOrganicStrike()
        {
            if (attackTimer > 0 || !TargetAlive(currentTarget)) return;
            if (OrganicCombatEffects.BlockingBarrier(Position, currentTarget.Position, currentTarget) is BuildingBase barrier)
            { currentTarget = barrier; return; }
            StopMoving();
            var direction = currentTarget.Position - Position; direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction);
            organicTarget = currentTarget; organic.aim = currentTarget.Position;
            organic.phase = Strike; organic.phaseLeft = .74f; attackTimer = attackInterval;
            organicVisual?.Act(2, 1.4f);
        }
        void ResolveOrganicStrike()
        {
            if (!TargetAlive(organicTarget) || TargetDistance(organicTarget) > attackRange + .25f) return;
            float damage = attackDamage * (!IsHorde ? 1 : Mathf.Min(HordeCount, organicTarget is BuildingBase ? HordeWallPressureCap : HordeFrontline));
            if (organicTarget is BuildingBase) damage *= OrganicEnemyRules.StructureMultiplier(OrganicKind);
            if (OrganicEnemyRules.Ranged(OrganicKind)) OrganicCombatEffects.Fire(this, organicTarget, organic.aim, damage);
            else if (OrganicCombatEffects.BlockingBarrier(Position, organicTarget.Position, organicTarget) == null)
            {
                FormationDamage.Apply(organicTarget, damage, Position);
                if (OrganicKind == OrganicEnemyKind.WallMelter) OrganicCombatEffects.Corrode(this, organicTarget, damage);
            }
        }
        bool MendEligible(WildMonster target) => target != null && target != this && target.isActiveAndEnabled && target.IsOrganic
            && target.Allied == Allied && !target.Retreating && !target.IsDead && target.OrganicKind != OrganicEnemyKind.BloomMender
            && target.organic.healLock <= 0 && target.currentHealth < target.LiveOrganicHealthCap - .01f
            && (target.Position - Position).sqrMagnitude <= 25;
        float LiveOrganicHealthCap => IsHorde ? Mathf.Min(maxHealth, HordeCount * hordeBodyHealth) : maxHealth;
        bool HasMendTarget() { foreach (var target in Active) if (MendEligible(target)) return true; return false; }
        void MendNearby()
        {
            int count = 0;
            foreach (var target in Active)
            {
                if (!MendEligible(target) || OrganicCombatEffects.BlockingBarrier(Position, target.Position, target) != null) continue;
                target.currentHealth = Mathf.Min(target.LiveOrganicHealthCap, target.currentHealth + 3);
                target.organic.healLock = 4;
                OrganicSupportVisual.Pulse(Position, target.Position, new Color(.55f, 1, .9f));
                if (++count >= 6) break;
            }
        }
        Corpse FindOrganicCorpse()
        {
            foreach (var corpse in Corpse.All)
                if (corpse != null && corpse.Available && corpse.Data.organicKind > 0 && corpse.Handler == null
                    && (corpse.Position - Position).sqrMagnitude < 9) return corpse;
            return null;
        }
        void ConsumeCorpse()
        {
            var corpse = FindOrganicCorpse();
            if (corpse == null || (corpse.Position - organic.corpsePosition).sqrMagnitude > .1f) return;
            corpse.Bury(); currentHealth = Mathf.Min(LiveOrganicHealthCap, currentHealth + 5);
        }
        void ReleaseYoung()
        {
            var local = SceneObjects.Any<LocalIncursions>();
            if (local == null || organic.spawnCharges <= 0) return;
            organic.spawnCharges--; // 실패해도 재시도/로드로 무한 생성하지 않는다.
            for (int i = 0; i < 2; i++) local.SpawnOrganicYoung(this, i);
        }
        public float OrganicDamageMultiplier(Vector3 source, bool ranged, bool area)
        {
            if (!IsOrganic || area) return 1;
            float multiplier = 1;
            var direction = source - Position; direction.y = 0;
            if (OrganicKind == OrganicEnemyKind.Bulwark && Vector3.Dot(transform.forward, direction.normalized) > .5f) multiplier = .65f;
            foreach (var ally in Active)
            {
                if (ally == null || ally == this || !ally.IsOrganic || ally.IsDead || ally.Retreating || ally.Allied != Allied) continue;
                var offset = Position - ally.Position;
                if (ranged && ally.organic.mistLeft > 0 && offset.sqrMagnitude <= 25) multiplier = Mathf.Min(multiplier, .75f);
                if (ally.OrganicKind == OrganicEnemyKind.Bulwark && offset.sqrMagnitude <= 9
                    && Vector3.Dot(ally.transform.forward, offset.normalized) < -.2f
                    && Vector3.Dot(ally.transform.forward, (source - ally.Position).normalized) > .5f) multiplier = Mathf.Min(multiplier, .75f);
            }
            return multiplier;
        }
    }
}
