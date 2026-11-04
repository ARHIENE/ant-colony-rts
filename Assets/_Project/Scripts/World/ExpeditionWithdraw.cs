using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    [Serializable]
    public sealed class WithdrawJob
    {
        public string commanderId, helperId;
        public float progress;
        public int boardedTroops;
        public bool lifted, boarded;
    }
    [Serializable]
    public sealed class WithdrawLoad
    {
        public string workerId; // 값이 있으면 실물은 아직 운반자에게 있다(이 항목은 공간 예약만 보유).
        public AntColony.Data.ResourceType type;
        public int amount;
        public float progress;
        public float rate = 1;
    }
    [Serializable]
    public sealed class WithdrawState
    {
        public List<WithdrawBodyJob> bodies = new List<WithdrawBodyJob>();
        public int troops; // 장수보다 먼저 승차 완료한 병력. AntPool.Assigned에 계속 포함한다.
        public bool active;
        public int nextSite = -1;
        public List<WithdrawJob> jobs = new List<WithdrawJob>();
        public List<WithdrawLoad> loads = new List<WithdrawLoad>();
    }
    public readonly struct WithdrawProgress
    {
        public readonly int People, Loads;
        // 경로 이동·부상자 구조·명령 불응 대기는 별도이므로 NaN이면 현재 예측 불가.
        public readonly float EstimatedSeconds;
        public WithdrawProgress(int people, int loads, float seconds) { People = people; Loads = loads; EstimatedSeconds = seconds; }
    }
    public static class ExpeditionWithdraw
    {
        public const float BoardSeconds = 1, LiftSeconds = 2, AssistedBoardSeconds = 2, LoadSeconds = 2, CarrySpeed = .75f;
        public const int WorkPlaces = 2, TroopGroupSize = 10, BodyWeight = 10, RemainsWeight = 5;
        public static float CarryMultiplier(CommanderAnt c)
        {
            var t = c.Transport;
            return t != null && t.withdraw.active && t.withdraw.jobs.Any(j => j.lifted && !j.boarded && j.helperId == c.PersonalState.id) ? CarrySpeed : 1;
        }
    }
    public partial class ExpeditionTransport
    {
        internal WithdrawState withdraw = new WithdrawState();
        public bool IsWithdrawing => withdraw.active;
        public int ReservedCargo => withdraw.loads.Sum(l => l.amount) + BodyReservations;
        private CommanderAnt Person(string id) => crew.FirstOrDefault(c => c != null && c.PersonalState.id == id);
        private static bool InjuredPassenger(CommanderAnt c) => c.PersonalHealth <= 0 || c.Health.Downed;
        // 행동 불능 인원은 일반 Board의 CanChangeAllocation을 통과할 수 없어 구조 명부만 예약한다.
        public bool TryAddInjuredPassenger(CommanderAnt c)
        {
            if (IsDead || State != ExpeditionState.Deployed || Site == null || Route?.IsRunning == true || c == null || !c.isActiveAndEnabled || c.IsDead
                || !c.IsColonyMember || c.IsCaptive || !InjuredPassenger(c) || crew.Contains(c)
                || CommanderLoad >= CommanderCapacity || Load + c.TroopCount > Capacity
                || (c.Transport != null ? c.Transport.State != ExpeditionState.Deployed || c.Transport.Site != Site
                    : c.Garrison != null ? c.Garrison.Site != Site : (c.Position - Site.transform.position).sqrMagnitude > 80 * 80)) return false;
            c.Transport?.DetachCommander(c); c.Garrison?.Remove(c);
            crew.Add(c); c.Transport = this;
            if (withdraw.active) withdraw.jobs.Add(new WithdrawJob { commanderId = c.PersonalState.id });
            return true;
        }
        public WithdrawProgress WithdrawalProgress
        {
            get
            {
                int people = 0; bool waiting = false;
                var tasks = new List<float>();
                var helping = new HashSet<string>(withdraw.jobs.Where(j => !j.boarded && !string.IsNullOrEmpty(j.helperId)).Select(j => j.helperId));
                foreach (var j in withdraw.jobs.Where(j => !j.boarded))
                {
                    var c = Person(j.commanderId); if (c == null || c.IsDead) continue;
                    int troops = c.TroopCount;
                    people += 1 + troops;
                    bool hurt = InjuredPassenger(c);
                    var helper = Person(j.helperId);
                    waiting |= (c.Position - Position).sqrMagnitude > 9 || hurt && helper == null || !hurt && !c.CanReceiveOrders;
                    if (helping.Contains(j.commanderId)) continue;
                    float rate = hurt && helper != null ? Mathf.Max(.001f, helper.WorkRate(CommanderActivity.Strength)) : 1;
                    float work = troops + (hurt ? ExpeditionWithdraw.AssistedBoardSeconds / rate + (j.lifted ? 0 : ExpeditionWithdraw.LiftSeconds / rate) : ExpeditionWithdraw.BoardSeconds);
                    work -= j.progress / (hurt && (troops == 0 || !j.lifted) ? rate : 1);
                    if (hurt && helper != null) work += helper.TroopCount + ExpeditionWithdraw.BoardSeconds;
                    tasks.Add(Mathf.Max(0, work));
                }
                tasks.AddRange(withdraw.loads.Select(l => Mathf.Max(0, ExpeditionWithdraw.LoadSeconds - l.progress) / l.rate));
                waiting |= withdraw.bodies.Count > 0;
                var slots = new float[ExpeditionWithdraw.WorkPlaces];
                foreach (float duration in tasks.OrderByDescending(t => t))
                {
                    int slot = Array.IndexOf(slots, slots.Min()); slots[slot] += duration;
                }
                return new WithdrawProgress(people, withdraw.loads.Count + withdraw.bodies.Count, waiting ? float.NaN : slots.Max());
            }
        }

        private bool RequestWithdrawal(ExpeditionSite next)
        {
            if (!CanOperate || State != ExpeditionState.Deployed || !isActiveAndEnabled || PendingCrewOrderCount > 0) return false;
            if (withdraw.active) return withdraw.nextSite == Save.SaveCatalog.SiteIndex(next);
            CancelFieldWork();
            withdraw.active = true; withdraw.nextSite = Save.SaveCatalog.SiteIndex(next);
            withdraw.jobs = crew.Where(c => c != null && !c.IsDead).Select(c => new WithdrawJob { commanderId = c.PersonalState.id }).ToList();
            foreach (var c in crew.Where(c => c != null && c.CanReceiveOrders && !c.IsCarrying)) c.CommandStop();
            return true;
        }
        public void CancelWithdrawal()
        {
            foreach (var j in withdraw.jobs.Where(j => !j.boarded))
            {
                var c = Person(j.commanderId);
                if (c != null && !c.IsDead && j.boardedTroops > 0)
                {
                    c.RestoreTroops(c.TroopCount + j.boardedTroops, c.PendingTroopDamage);
                    withdraw.troops -= j.boardedTroops; j.boardedTroops = 0;
                }
            }
            foreach (var j in withdraw.jobs.Where(j => j.lifted && !j.boarded))
            {
                var c = Person(j.commanderId); if (c != null && !c.IsDead) c.ApplyMovementMode();
            }
            withdraw.active = false; withdraw.jobs.Clear();
            foreach (var c in crew) if (c != null && c.IsEmbarked && State == ExpeditionState.Deployed) c.SetEmbarked(false, Position + Vector3.right * 3);
            foreach (var j in withdraw.bodies.ToArray()) CancelBodyRecovery(j.bodyId);
            foreach (var c in crew.Where(c => c != null && !c.IsDead && !c.IsEmbarked)) c.ApplyMovementMode();
        }
        // 긴급 출발은 완료된 장수/병력만 출발한다. 남은 병력은 원래 장수와 함께 현장에 남긴다.
        public bool TryEmergencyReturn()
        {
            if (!withdraw.active || !CanOperate || !HasPilot || !withdraw.jobs.Any(j => j.boarded && Person(j.commanderId) != null
                && Person(j.commanderId).PersonalHealth > 0 && !Person(j.commanderId).Health.Downed)) return false;
            foreach (var j in withdraw.jobs.Where(j => !j.boarded).ToArray())
            {
                var c = Person(j.commanderId); if (c == null) continue;
                c.ApplyMovementMode(); crew.Remove(c); c.Transport = null;
                if (Site?.Settlement != null) Site.Settlement.Add(c);
            }
            foreach (var j in withdraw.bodies.ToArray()) CancelBodyRecovery(j.bodyId);
            DropPendingLoads(); withdraw.active = false; withdraw.jobs.Clear();
            StartWithdrawTravel(null); return true;
        }
        private void DropPendingLoads()
        {
            foreach (var l in withdraw.loads.Where(l => string.IsNullOrEmpty(l.workerId))) DiplomacyManager.DropFloor(l.type, l.amount, Position);
            withdraw.loads.Clear();
        }
        internal bool TryLoadFromWorker(WorkerAnt worker, AntColony.Data.ResourceType type, int amount)
        {
            if (IsDead || amount <= 0) return false;
            var c = worker as CommanderAnt;
            if (Walking || State != ExpeditionState.Deployed || c == null) { DepositResources(type, amount); return true; }
            var job = withdraw.loads.FirstOrDefault(l => l.workerId == c.PersonalState.id);
            if (job == null)
            {
                int accepted = Mathf.Min(amount, ReturnCargoSpace);
                if (accepted == 0) { worker.DropCargo(); return true; }
                withdraw.loads.Add(new WithdrawLoad { workerId = c.PersonalState.id, type = type, amount = accepted });
                return false;
            }
            if (job.progress < ExpeditionWithdraw.LoadSeconds) return false;
            withdraw.loads.Remove(job);
            int added = AddCargo(type, Mathf.Min(amount, job.amount));
            if (added < amount)
            {
                var lots = worker.CaptureCargoLots(); lots?.Take(added);
                DiplomacyManager.DropFloor(type, amount - added, worker.Position, lots);
            }
            return true;
        }
        private void StartWithdrawTravel(ExpeditionSite next)
        {
            float travel = next != null && Site != null
                ? ExpeditionRules.BetweenSeconds(Site.MapPosition, next.MapPosition, Walking, Aircraft) / (Walking ? FormationWalkMultiplier : 1)
                : TravelSeconds;
            if (Site?.Settlement != null)
                foreach (var c in Site.Settlement.Garrison) if (c != null && c.IsWorking && !c.IsCarrying) c.CommandStop();
            if (WorldMapManager.Instance?.ViewedSite == Site) WorldMapManager.Instance.ViewSite(null);
            if (next != null)
            {
                if (Site != null && Site.Visitor == this) Site.Visitor = null;
                Site = next; if (next.Visitor == null) next.Visitor = this;
                State = ExpeditionState.Outbound;
            }
            else State = ExpeditionState.Returning;
            Remaining = travel;
        }
        private bool Approach(CommanderAnt c, Vector3 target)
        {
            if ((c.Position - target).sqrMagnitude <= 9) return true;
            if (c.CanReceiveOrders && c.TryWorkApproach(target, out var point)) c.CommandMove(point);
            return false;
        }
        private void TickWithdrawal(float seconds)
        {
            if (IsDead) return;
            // 현장 반납도 두 작업 자리를 공유한다. 아직 적재하지 않은 실물은 예약 화물이다.
            int places = ExpeditionWithdraw.WorkPlaces;
            foreach (var l in withdraw.loads.ToArray())
            {
                var worker = string.IsNullOrEmpty(l.workerId) ? null : CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c.PersonalState.id == l.workerId);
                if (!string.IsNullOrEmpty(l.workerId))
                {
                    if (worker == null || !worker.CanReceiveOrders || !worker.IsCarrying || worker.CarriedType != l.type || worker.CarriedAmount + .001f < l.amount
                        || (worker.Position - Position).sqrMagnitude > 64)
                    { withdraw.loads.Remove(l); continue; }
                    if (l.progress >= ExpeditionWithdraw.LoadSeconds) continue;
                }
                if (places-- <= 0) break;
                l.progress = Mathf.Min(ExpeditionWithdraw.LoadSeconds, l.progress + seconds * (worker != null ? worker.WorkRate(CommanderActivity.Strength) : l.rate));
                if (l.progress < ExpeditionWithdraw.LoadSeconds) continue;
                if (worker != null) continue; // WorkerAnt.Deposit가 실물·예약을 같은 호출에서 확정한다.
                withdraw.loads.Remove(l); var added = AddCargo(l.type, l.amount);
                if (added < l.amount) DiplomacyManager.DropFloor(l.type, l.amount - added, Position);
            }
            TickBodies(seconds, ref places);
            if (!withdraw.active) return;
            var helpers = new HashSet<string>(withdraw.jobs.Where(j => !j.boarded && !string.IsNullOrEmpty(j.helperId)).Select(j => j.helperId));
            // 부상자부터 구조해 조력자가 먼저 탑승해 버리지 않게 한다.
            foreach (var j in withdraw.jobs.OrderByDescending(j => Person(j.commanderId) != null && InjuredPassenger(Person(j.commanderId))).ToArray())
            {
                var c = Person(j.commanderId);
                if (c == null || c.IsDead) { withdraw.jobs.Remove(j); continue; }
                if (j.boarded || helpers.Contains(j.commanderId) || withdraw.bodies.Any(b => b.carrierId == j.commanderId)) continue;
                bool hurt = InjuredPassenger(c);
                if (!hurt && j.lifted)
                {
                    helpers.Remove(j.helperId); j.helperId = null; j.lifted = false; j.progress = 0; c.ApplyMovementMode();
                }
                CommanderAnt helper = null;
                if (hurt)
                {
                    helper = Person(j.helperId);
                    if (helper == null || !helper.CanReceiveOrders || helper.IsCarrying)
                    {
                        if (j.lifted) { c.ApplyMovementMode(); j.lifted = false; j.progress = 0; }
                        helpers.Remove(j.helperId);
                        helper = crew.FirstOrDefault(x => x != null && x != c && x.CanReceiveOrders && !x.IsCarrying && !helpers.Contains(x.PersonalState.id)
                            && !withdraw.bodies.Any(b => b.carrierId == x.PersonalState.id)
                            && withdraw.jobs.Any(k => k.commanderId == x.PersonalState.id && !k.boarded));
                        j.helperId = helper?.PersonalState.id;
                        if (helper == null) continue;
                        helpers.Add(j.helperId);
                    }
                    if (!j.lifted)
                    {
                        if (!Approach(helper, c.Position) || places <= 0) continue;
                        places--; j.progress += seconds * helper.WorkRate(CommanderActivity.Strength);
                        if (j.progress < ExpeditionWithdraw.LiftSeconds) continue;
                        j.progress = 0; j.lifted = true; c.Agent.enabled = false;
                        continue;
                    }
                    c.transform.position = helper.Position;
                    if (!Approach(helper, Position)) continue;
                }
                else
                {
                    if (!c.CanReceiveOrders) continue;
                    if (c.IsCarrying)
                    {
                        if (!c.IsWorking && Approach(c, Position)) c.TryReturnCargo(this);
                        continue;
                    }
                    if (!Approach(c, Position)) continue;
                }
                if (places <= 0) continue;
                places--;
                j.progress += seconds * (hurt && c.TroopCount == 0 ? helper.WorkRate(CommanderActivity.Strength) : 1);
                int group = Mathf.Min(ExpeditionWithdraw.TroopGroupSize, c.TroopCount);
                if (group > 0)
                {
                    float duration = group * ExpeditionWithdraw.BoardSeconds;
                    if (j.progress < duration) continue;
                    j.progress -= duration; j.boardedTroops += group; withdraw.troops += group;
                    c.RestoreTroops(c.TroopCount - group, c.PendingTroopDamage);
                    if (c.TroopCount > 0) continue;
                }
                if (j.progress < (hurt ? ExpeditionWithdraw.AssistedBoardSeconds : ExpeditionWithdraw.BoardSeconds)) continue;
                c.RestoreTroops(c.TroopCount + j.boardedTroops, c.PendingTroopDamage);
                withdraw.troops -= j.boardedTroops; j.boardedTroops = 0;
                c.SetEmbarked(true, Position); j.boarded = true; j.progress = 0;
                if (helper != null) { helpers.Remove(j.helperId); helper.ApplyMovementMode(); }
            }
            if (withdraw.loads.Count > 0 || withdraw.bodies.Count > 0 || withdraw.jobs.Any(j => !j.boarded)) return;
            if (!CanOperate || !HasPilot || Site?.Settlement?.Garrison.Any(c => c != null && c.IsCarrying) == true) return;
            var next = withdraw.nextSite < 0 ? null : WorldMapManager.Instance.Sites[withdraw.nextSite];
            if (next != null && (!next.isActiveAndEnabled || next.Disposition != ConquestDisposition.Annexed || ReturnCargoSpace < 1)) next = null;
            withdraw.active = false; withdraw.jobs.Clear(); StartWithdrawTravel(next);
        }
        public int TroopPassengers => withdraw.troops;
        private void ReturnTroopPassengers()
        {
            if (withdraw.troops <= 0 || AntPool.Instance == null) return;
            int count = withdraw.troops; withdraw.troops = 0; AntPool.Instance.ReturnAssigned(count);
        }
        // 잔해에 남은 승차 완료 병력은 구조 장수의 부대로 합쳐 기존 탑승 명령으로 회수한다.
        public bool TryRescueTroops(CommanderAnt c)
        {
            if (!IsWreck || !FieldWorker(c) || withdraw.troops <= 0 || c.TroopCount + withdraw.troops > c.CommandLimit
                || c.Transport != null && c.Transport != this && c.Transport.Load + withdraw.troops > c.Transport.Capacity) return false;
            c.RestoreTroops(c.TroopCount + withdraw.troops, c.PendingTroopDamage); withdraw.troops = 0; return true;
        }
        internal void RestoreCargoAmounts(int[] amounts)
        {
            cargo.Clear();
            for (int i = 0; i < amounts.Length; i++) if (amounts[i] > 0) cargo[(AntColony.Data.ResourceType)i] = amounts[i];
        }
        internal bool HasBoardedTroopsFor(CommanderAnt c) => withdraw.jobs.Any(j => j.commanderId == c.PersonalState.id && j.boardedTroops > 0);
        internal WithdrawState CaptureWithdrawal()
        {
            var copy = JsonUtility.FromJson<WithdrawState>(JsonUtility.ToJson(withdraw));
            copy.jobs.RemoveAll(j => Person(j.commanderId) == null || Person(j.commanderId).IsDead);
            foreach (var j in copy.jobs)
                if (!string.IsNullOrEmpty(j.helperId) && (Person(j.helperId) == null || Person(j.helperId).IsDead))
                { j.helperId = null; j.lifted = false; j.progress = 0; }
            copy.bodies.RemoveAll(j => !Corpse.All.Any(b => b.Data.commanderId == j.bodyId) || Person(j.carrierId) == null || Person(j.carrierId).IsDead);
            copy.loads.RemoveAll(l => !string.IsNullOrEmpty(l.workerId) && !CommanderRoster.Instance.Commanders.Any(c => c.PersonalState.id == l.workerId
                && !c.IsDead && c.IsCarrying && c.CarriedType == l.type && c.CarriedAmount + .001f >= l.amount));
            return copy;
        }
        internal void RestoreWithdrawal(WithdrawState state)
        {
            withdraw = state ?? new WithdrawState();
            withdraw.bodies = withdraw.bodies ?? new List<WithdrawBodyJob>();
            RestoreBodyWork();
            withdraw.jobs = withdraw.jobs ?? new List<WithdrawJob>(); withdraw.loads = withdraw.loads ?? new List<WithdrawLoad>();
            if (!withdraw.active) { withdraw.jobs.Clear(); return; }
            foreach (var j in withdraw.jobs)
            {
                var c = Person(j.commanderId); if (c == null) continue;
                if (j.boarded) c.SetEmbarked(true, Position);
                else if (j.lifted) c.Agent.enabled = false;
            }
        }
    }
}
