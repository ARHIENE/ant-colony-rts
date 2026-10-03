using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public CommanderSocialState Social => personalState.social;
        public bool IsDeparting => Social.departure != DepartureState.None;
        public bool IsHostile => Social.departure == DepartureState.Fleeing || Social.departure == DepartureState.Rebellion || Social.departure == DepartureState.Retreating;
        public bool IsColonyMember => !IsDead && !IsDeparting;
        private static CommanderAnt[] SocialRoster => CommanderRoster.Instance != null
            ? CommanderRoster.Instance.Commanders.ToArray() : Active.OfType<CommanderAnt>().ToArray();
        public bool IsFriend(CommanderAnt other) => other != null && personalState.relations.Exists(r => r.otherId == other.personalState.id && r.value >= SocialRules.Friend);
        private bool MutualFriend(CommanderAnt other) => IsFriend(other) && other.IsFriend(this);

        // ponytail: 장수 수십 명 규모의 상호 친구 집단을 직접 검색한다. 대규모 명부가 되면 관계 변경 시 캐시한다.
        public List<CommanderAnt> Faction()
        {
            var members = new List<CommanderAnt> { this };
            foreach (var c in SocialRoster.OrderBy(c => c.PersonalState.id, System.StringComparer.Ordinal))
                if (c != this && c.IsColonyMember && members.All(m => m.MutualFriend(c))) members.Add(c);
            return members.Count >= 3 ? members : new List<CommanderAnt>();
        }
        public bool HasRivalFaction
        {
            get
            {
                if (!personalState.relations.Any(r => r.value <= SocialRules.Rival)) return false;
                var own = Faction();
                return own.Count >= 3 && SocialRoster.Any(c => c.IsColonyMember && !own.Contains(c)
                    && personalState.relations.Any(r => r.otherId == c.personalState.id && r.value <= SocialRules.Rival) && c.Faction().Count >= 3);
            }
        }
        public void OnTroopsRecalled(int removed, int before)
        {
            if (removed * 2 < before || before <= 0) return;
            MoodEvent("병력 회수", traits.Has(CommanderTrait.Loyal) ? 0 : traits.Has(CommanderTrait.Ambitious) ? -10 : -5);
        }
        // Phase 3: 예전 충성심 사건은 한 달짜리 기분 요인으로 남긴다(수치는 기존 충성심 변동 그대로, 잠정).
        public void MoodEvent(string reason, int value) { if (value != 0) personalState.AddMood(reason, value, GameCalendar.SecondsPerMonth); }
        public void OnHunger() => MoodEvent("굶주림 누적", traits.Has(CommanderTrait.Ascetic) ? 0 : traits.Has(CommanderTrait.Glutton) ? -4 : -2);
        public void OnExpeditionStarted()
        {
            Social.expeditions++;
            if (Social.expeditions == 10) ChangeEventTrait(CommanderTrait.Wanderer, CommanderTrait.Homebody);
        }
        public void OnExpeditionVictory() => MoodEvent("원정 승리 기억", traits.Has(CommanderTrait.Wanderer) || traits.Has(CommanderTrait.Bloodthirsty) ? 6
            : traits.Has(CommanderTrait.Homebody) ? 1 : 3);
        public static void OnPrisonerExecuted(string victimId, string faction, string victimName)
        {
            foreach (var c in SocialRoster.Where(c => c.IsColonyMember && !c.IsCaptive))
            {
                int change = !string.IsNullOrEmpty(faction) && c.personalState.originFaction == faction ? -15
                    : c.traits.Has(CommanderTrait.ColdBlooded) || c.traits.Has(CommanderTrait.Reckless) || c.traits.Has(CommanderTrait.Bloodthirsty) ? 3
                    : c.traits.Has(CommanderTrait.Loyal) ? 0 : c.traits.Has(CommanderTrait.Sociable) || c.traits.Has(CommanderTrait.Coward) ? -10 : -5;
                c.MoodEvent("포로 처형", change);
                c.FriendDied(victimId, victimName, true);
            }
        }
        private void FriendDied(string id, string victimName, bool preventable)
        {
            var relation = personalState.relations.Find(r => r.otherId == id);
            if (relation == null || relation.value < SocialRules.Friend) return;
            if (!traits.Has(CommanderTrait.ColdBlooded))
            {
                var loss = relation.spouse ? 10f : 5f;
                if (preventable) loss *= 2;
                if (traits.Has(CommanderTrait.Loyal)) loss *= .5f;
                MoodEvent("친구 사망: " + victimName, -Mathf.RoundToInt(loss));
            }
            if (Random.value < .3f) ShiftEventTrait(CommanderTrait.Depressive, false);
        }
        public void NotifyDowned(bool fatal, string cause)
        {
            foreach (var c in SocialRoster.Where(c => c != this && c.IsColonyMember))
            {
                if (fatal) c.FriendDied(personalState.id, commanderName, cause == "처형" || cause == "방치");
                if (c.IsFriend(this)) c.StartRevenge();
            }
        }
        public void OnCaptured()
        {
            personalState.captiveSeconds = personalState.unsupportedSeconds = 0;
            Social.captiveDebt = 0;
            foreach (var c in SocialRoster.Where(c => c != this && c.IsColonyMember && c.IsFriend(this))) c.StartRevenge();
        }
        public void TickCaptivity(float seconds)
        {
            if (!IsCaptive || IsDead || !(seconds > 0) || float.IsInfinity(seconds)) return;
            personalState.captiveSeconds += seconds;
            personalState.unsupportedSeconds += seconds;
            while (personalState.unsupportedSeconds >= SocialRules.Month)
            {
                personalState.unsupportedSeconds -= SocialRules.Month;
                Social.captiveDebt += 10;
                foreach (var c in SocialRoster.Where(c => c != this && c.IsColonyMember && !c.IsCaptive && c.IsFriend(this)))
                    c.MoodEvent("친구 포로 방치", c.traits.Has(CommanderTrait.ColdBlooded) ? 0 : c.traits.Has(CommanderTrait.Sociable) ? -10 : -5);
            }
        }
        public void OnRescued()
        {
            MoodEvent("구출", 20 - Social.captiveDebt); Social.captiveDebt = 0;
            foreach (var c in SocialRoster.Where(c => c != this && c.IsColonyMember && !c.IsCaptive && c.IsFriend(this)))
                c.MoodEvent("친구 구출", c.traits.Has(CommanderTrait.Sociable) ? 10 : 5);
            if (personalState.captiveSeconds >= SocialRules.Month * 2 && Random.value < .3f)
                ChangeEventTrait(CommanderTrait.ColdBlooded, CommanderTrait.Sociable);
            personalState.captiveSeconds = personalState.unsupportedSeconds = 0;
        }
        private void StartRevenge()
        {
            if (!isActiveAndEnabled || IsCaptive || IsEmbarked || IsDeparting || !HasTroops) return;
            InterruptPersonalWork();
            personalState.rageRemaining = traits.Has(CommanderTrait.Reckless) ? 120 : 60;
            ToastManager.Show(commanderName + ": 친구를 위한 복수");
        }
        private void TickRevenge(float seconds)
        {
            var target = CombatTargeting.FindNearestEnemy(Position, 10000, IsFlying ? AntColony.Data.UnitRole.Flying : Role);
            if (target != null && !SameBattlefield(target.Position)) target = null;
            TickForcedAttack(target, seconds);
        }
        private void TickSocial(float seconds)
        {
            if (!IsColonyMember || IsCaptive || IsEmbarked) return;
            if (Social.pendingDeparture) { TryDeparture(); if (IsDeparting) return; }
            if (Social.seriousInjuries >= 3 && !Social.injuryTraitChanged)
            {
                Social.injuryTraitChanged = true;
                if (traits.values.Remove(CommanderTrait.Robust)) ReportTrait("Robust 제거");
                else ChangeEventTrait(CommanderTrait.Frail);
            }
            // Phase 3: 기분이 탈주 기준 이하로 한 달 내내 이어지면 떠난다(파벌이면 무장 반란).
            personalState.lowMoodSeconds = Mood <= traits.DepartureMood ? personalState.lowMoodSeconds + seconds : 0;
            if (personalState.lowMoodSeconds >= SocialRules.Month) { personalState.lowMoodSeconds = 0; TryDeparture(); }
        }
        private void ChangeEventTrait(CommanderTrait next, CommanderTrait? remove = null)
        {
            bool changed = remove.HasValue && traits.values.Remove(remove.Value);
            // 반대 특성은 제거만 한다. 다음 사건에서 빈칸에 새 특성을 얻는다.
            if (!changed) changed = traits.TryAdd(next);
            if (changed) ReportTrait(remove.HasValue && !traits.Has(next) ? remove + " 제거" : next.ToString());
        }
        private void ShiftEventTrait(CommanderTrait first, bool up)
        {
            var index = traits.values.FindIndex(t => (int)t >= (int)first && (int)t < (int)first + 4);
            if (index < 0) return;
            int value = (int)traits.values[index];
            int next = Mathf.Clamp(value + (up ? 1 : -1), (int)first, (int)first + 3);
            if (next == value) return;
            traits.values[index] = (CommanderTrait)next; ReportTrait(traits.values[index].ToString());
        }
        private void ReportTrait(string result)
        {
            ToastManager.Show(commanderName + ": 특성 변화 — " + result);
            CampaignHistory.Record("특성 변화", commanderName, result, true);
        }
        private void InterruptPersonalWork()
        {
            CommandStop(); ScienceAssignment?.ReleaseResearcher(); LabUpgradeLab?.CancelResearch();
            TreatmentFacility?.Release(this); CraftingWorkshop?.Release();
        }
    }
}
