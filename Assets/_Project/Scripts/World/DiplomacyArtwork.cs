using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public enum ArtworkPurpose { Contact, Peace, Release }
    public sealed partial class DiplomacyManager
    {
        [Serializable] public sealed class ArtworkRetry { public string civ; public ArtworkPurpose purpose; public float until; }
        public static float ArtworkChance(float affinity) => Math.Max(0, Math.Min(1, .5f + affinity / 200f + .3f));
        public float ArtworkWait(Civilization c, ArtworkPurpose purpose) => Math.Max(0, Data.artworkRetries?.FirstOrDefault(r => r.civ == c.id && r.purpose == purpose)?.until - Data.elapsed ?? 0);
        public IEnumerable<Decoration> GiftArtworks => SceneObjects.All<Decoration>().Where(a => a.isActiveAndEnabled && !a.IsDead && a.DiamondArtwork && a.Quality == 3);
        public string ArtworkReason(Civilization c, ArtworkPurpose purpose, string prisoner = null)
        {
            if (!Available || c == null || !Data.civilizations.Contains(c) || c.extinct || !Enum.IsDefined(typeof(ArtworkPurpose), purpose)) return "유효한 문명을 선택하세요.";
            if (ArtworkWait(c, purpose) > 0) return $"화룡 재시도 대기 {ArtworkWait(c, purpose) / GameCalendar.SecondsPerDay:0.0}일";
            if (purpose == ArtworkPurpose.Contact) return !c.contacted && world.Sites.Any(s => Faction(s) == c && WorldMapManager.SiteRevealed(s)) ? "" : "미접촉 문명의 거점을 먼저 발견하세요.";
            if (!c.contacted) return "접촉이 필요합니다.";
            if (purpose == ArtworkPurpose.Peace)
            {
                if (!CanNegotiatePeace(c)) return "기존 평화 협상 대기 시간이 필요합니다.";
                var due = Reparations(c);
                if (ResourceManager.Instance == null || due > 0 && (!ResourceManager.Instance.CanAfford(due, 0) || (long)c.resources[0] + due > int.MaxValue)
                    || due < 0 && c.resources[0] < -due) return "배상 자원이 부족합니다.";
            }
            if (purpose == ArtworkPurpose.Release && !RansomableIds(c).Contains(prisoner)) return "석방받을 자국 장수를 선택하세요.";
            return "";
        }
        public string OfferArtwork(Civilization c, Decoration artwork, ArtworkPurpose purpose, string prisoner = null)
        {
            var reason = ArtworkReason(c, purpose, prisoner); if (reason != "") return reason;
            if (!GiftArtworks.Contains(artwork)) return "사용 가능한 화룡 걸작을 선택하세요.";
            if (UnityEngine.Random.value >= ArtworkChance(c.affinity))
            {
                Data.artworkRetries ??= new List<ArtworkRetry>();
                Data.artworkRetries.RemoveAll(r => r.civ == c.id && r.purpose == purpose);
                Data.artworkRetries.Add(new ArtworkRetry { civ = c.id, purpose = purpose, until = Data.elapsed + GameCalendar.SecondsPerDay * 15 });
                return "거절 · 작품 보존, 해당 문명/목적은 15일 후 재시도";
            }
            if (purpose == ArtworkPurpose.Peace && !MakePeace(c)) return "평화 조건이 바뀌었습니다. 작품은 보존됩니다.";
            if (purpose == ArtworkPurpose.Contact) MarkContact(c);
            if (purpose == ArtworkPurpose.Release)
            {
                BeginHomecoming(prisoner, c, new List<EquipmentItem>());
                c.rebels.RemoveAll(p => p.PersonalState.id == prisoner);
            }
            artwork.DiamondArtwork = false; artwork.gameObject.SetActive(false); Destroy(artwork.gameObject);
            CampaignHistory.Record("화룡", c.name, purpose + " 성공 · 걸작 선물", true);
            return "화룡 선물 수락 · 작품 소모";
        }
        private static bool ValidArtwork(State s) => s.artworkRetries == null || s.artworkRetries.Count <= 300
            && s.artworkRetries.All(r => r != null && s.civilizations.Any(c => c.id == r.civ) && Enum.IsDefined(typeof(ArtworkPurpose), r.purpose)
                && r.until >= 0 && !float.IsInfinity(r.until)) && s.artworkRetries.Select(r => (r.civ, r.purpose)).Distinct().Count() == s.artworkRetries.Count;
    }
}
