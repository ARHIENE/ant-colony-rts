using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private bool storyInjuryActive;
        private float storyScanAt;
        private float storySpeechAt = -100;
        private int storySpeechPriority;
        public bool TryStorySpeech(float now, int priority) => now - storySpeechAt >= 10 || priority > storySpeechPriority;
        public void StorySpeechAccepted(float now, int priority) { storySpeechAt = now; storySpeechPriority = priority; }
        private void TellStory(string story, string speech, int priority)
        {
            if (!IsColonyMember || IsHostile) return;
            CampaignHistory.Story(personalState.id, CommanderName, story);
            SpeechBubbles.Say(this, speech, priority);
        }
        private void TickPersonalStory(bool downed = false)
        {
            if (!CampaignHistory.Recording || !downed && Time.unscaledTime < storyScanAt) return;
            storyScanAt = Time.unscaledTime + .5f;
            bool serious = downed || personalState.HasSeriousInjury || Health.Downed || PersonalHealth <= 0;
            if (!serious) { storyInjuryActive = false; return; }
            if (storyInjuryActive || !CampaignHistory.Recording || !IsColonyMember || IsCaptive) return;
            storyInjuryActive = true;
            TellStory("큰 부상을 입음", "도움이 필요해", 2);
            if (CommanderRoster.Instance == null) return;
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                if (c == null || c == this || !c.IsColonyMember || c.IsCaptive || c.IsEmbarked || c.IsAwayFromHome != IsAwayFromHome
                    || (c.Position - Position).sqrMagnitude > 144 || c.Traits.Has(CommanderTrait.ColdBlooded)) continue;
                var relation = c.PersonalState.relations.Find(r => r.otherId == personalState.id);
                float loss = InjuryConcern(relation, c.Traits.Has(CommanderTrait.Coward));
                c.PersonalState.AddMood("Downed: " + CommanderName, loss * (c.Traits.Has(CommanderTrait.Undertaker) ? .5f : 1), 180);
                c.TellStory(CommanderName + "의 중상을 목격", CommanderName + (relation?.spouse == true || relation?.value >= SocialRules.Friend ? ", 무사해야 해" : "가 다쳤어"), 2);
            }
        }
        public static float InjuryConcern(CommanderRelation relation, bool coward) => relation?.spouse == true || relation?.value >= SocialRules.Friend ? -15 : coward ? -16 : -8;
    }
}
