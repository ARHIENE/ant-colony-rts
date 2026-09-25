using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public bool TryDeparture()
        {
            if (!IsColonyMember || IsCaptive) return false;
            if (IsEmbarked) { Social.pendingDeparture = true; return false; }
            var faction = Faction();
            bool rebellion = faction.Any(c => c != this && c.Traits.Loyalty <= 40 && c.isActiveAndEnabled && !c.IsEmbarked && !c.IsCaptive);
            var leaving = rebellion ? faction.Where(c => c == this || c.Traits.Loyalty <= 40 || Random.value < .5f).ToArray() : new[] { this };
            foreach (var c in leaving)
                if (c.isActiveAndEnabled && !c.IsCaptive && !c.IsEmbarked) c.BeginDeparture(rebellion);
            return IsDeparting;
        }
        private void BeginDeparture(bool rebellion)
        {
            if (!TryFindEscape(out var point)) { Social.pendingDeparture = true; return; }
            InterruptPersonalWork();
            var site = Transport?.Site ?? Garrison?.GetComponent<AntColony.World.ExpeditionSite>();
            if (IsCarrying)
            {
                // 떠나는 장수의 작업 화물은 현장에 남겨 회수할 수 있게 한다.
                if (site != null) EvacuateCargo(site);
                else DropCargo();
            }
            Garrison?.Remove(this); Transport?.DetachCommander(this);
            AntPool.Instance?.LoseAssigned(troopCount);
            Social.pendingDeparture = false;
            Social.departure = rebellion ? DepartureState.Rebellion : DepartureState.Fleeing;
            Social.rebellionSeconds = SocialRules.RebellionSeconds;
            Social.escapePoint = point;
            personalState.rageRemaining = 0;
            personalState.mentalBreak = MentalBreak.None; personalState.breakRemaining = 0;
            skills.CancelEffects(); Social.acidRemaining = 0; Social.diving = false;
            var selectable = GetComponent<SelectableObject>(); selectable.SetSelected(false); selectable.enabled = false;
            RefreshDepartureVisual();
            var kind = rebellion ? "무장 반란" : "탈주";
            CampaignHistory.Record("떠난 장수", commanderName, kind, true);
            RefreshDepartureNotice();
            if (!HasTroops) CaptureDeparting();
        }
        private bool TryFindEscape(out Vector3 point)
        {
            var site = Transport?.Site ?? Garrison?.GetComponent<AntColony.World.ExpeditionSite>();
            var bounds = site != null ? new Bounds(site.transform.position, new Vector3(64, 100, 64)) : HomeMapBuilder.CurrentWorldBounds;
            point = Position;
            float distance = float.MaxValue;
            for (int side = 0; side < 4; side++)
                for (int step = -4; step <= 4; step++)
                {
                    var p = bounds.center + new Vector3(side < 2 ? (side == 0 ? -1 : 1) * (bounds.extents.x - 2) : step * .2f * bounds.extents.x,
                        Position.y - bounds.center.y, side >= 2 ? (side == 2 ? -1 : 1) * (bounds.extents.z - 2) : step * .2f * bounds.extents.z);
                    if (!NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas) || !CanReach(hit.position)) continue;
                    var d = (hit.position - Position).sqrMagnitude;
                    if (d >= distance) continue;
                    distance = d; point = hit.position;
                }
            return distance < float.MaxValue;
        }
        public void TickDeparture(float seconds)
        {
            if (!IsHostile || IsDead || !(seconds > 0) || float.IsInfinity(seconds)) return;
            if (!HasTroops) { CaptureDeparting(); return; }
            Social.rootRemaining = Mathf.Max(0, Social.rootRemaining - seconds);
            if (Social.departure == DepartureState.Rebellion)
            {
                Social.rebellionSeconds = Mathf.Max(0, Social.rebellionSeconds - seconds);
                if (Social.rebellionSeconds == 0) { Social.departure = DepartureState.Retreating; CommandStop(); }
                else
                {
                    var units = Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember && CombatTargeting.IsAlive(c)).Cast<IDamageable>();
                    var buildings = FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(b => b.CountsTowardPlayerDefeat && !b.IsDead).Cast<IDamageable>();
                    TickForcedAttack(units.Concat(buildings).Where(t => SameBattlefield(t.Position)).OrderBy(t => (t.Position - Position).sqrMagnitude).FirstOrDefault(), seconds);
                    return;
                }
            }
            if (Social.rootRemaining > 0) { StopMoving(); return; }
            SetMoveDestination(Social.escapePoint); TickFlightMovement();
            var delta = Position - Social.escapePoint; delta.y = 0;
            if (delta.sqrMagnitude <= 2.25f) FinishDeparture();
        }
        private void TickForcedAttack(IDamageable target, float seconds)
        {
            if (target == null || !HasTroops || Data == null) { StopMoving(); return; }
            if (GetDistanceTo(target.Position) > Data.attackRange)
            {
                if (Social.rootRemaining > 0) { StopMoving(); return; }
                SetMoveDestination(target.Position); TickFlightMovement(); return;
            }
            StopMoving(); Social.attackSeconds = Mathf.Max(0, Social.attackSeconds - seconds);
            if (Social.attackSeconds > 0) return;
            Social.attackSeconds = Data.attackInterval;
            GetComponent<AntVisual>()?.Attack(target.Position);
            DealDamage(target);
        }
        private void CaptureDeparting()
        {
            if (!IsHostile) return;
            troopCount = 0; pendingDamage = 0; CommandStop();
            var camp = PrisonerCamp.Instance;
            if (camp != null && camp.TryCapture(this))
            {
                CommanderRoster.Instance?.Forget(this);
                Social.departure = DepartureState.Imprisoned;
                gameObject.SetActive(false); Destroy(gameObject);
            }
            else
            {
                personalState.dead = true; personalState.departure = "Deceased";
                if (personalState.equipment.Count > 0) { AntColony.World.EquipmentLoot.Drop(Position, personalState.equipment); personalState.equipment.Clear(); }
                CampaignHistory.Record("사망", commanderName, "이탈 중 제압: 수용소 없음", true);
                gameObject.SetActive(false);
            }
            RefreshDepartureNotice();
        }
        private void FinishDeparture()
        {
            Social.departure = DepartureState.Left;
            personalState.departure = "Left";
            troopCount = 0; pendingDamage = 0;
            CampaignHistory.Record("이탈", commanderName, "맵 가장자리 도착", true);
            gameObject.SetActive(false); RefreshDepartureNotice();
        }
        public static void RefreshDepartureNotice()
        {
            var rebels = Active.OfType<CommanderAnt>().Where(c => c.IsHostile).Select(c => c.CommanderName).ToArray();
            ToastManager.SetCrisis("departure", rebels.Length == 0 ? null : "이탈 위기: " + string.Join(", ", rebels) + " — 병력을 제압해 포획하세요.");
        }
        private bool SameBattlefield(Vector3 target)
        {
            var world = AntColony.World.WorldMapManager.Instance;
            if (world != null)
                foreach (var site in world.Sites)
                    if ((Position - site.transform.position).sqrMagnitude < 50 * 50)
                        return (target - site.transform.position).sqrMagnitude < 50 * 50;
            return HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(target.x, 0, target.z));
        }
        internal void RefreshDepartureVisual()
        {
            if (!IsHostile) return;
            var color = new MaterialPropertyBlock(); color.SetColor("_BaseColor", new Color(.9f, .2f, .12f)); color.SetColor("_Color", new Color(.9f, .2f, .12f));
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(color);
        }
        public void Root(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || !IsHostile || IsFlying) return;
            Social.rootRemaining = Mathf.Max(Social.rootRemaining, seconds); StopMoving();
        }
    }
}
