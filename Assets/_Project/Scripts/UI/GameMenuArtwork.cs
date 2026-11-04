using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private Decoration giftArtwork;
        private void ArtworkDiplomacy()
        {
            var d = DiplomacyManager.Instance; if (d?.Available != true) return;
            var f = Frame("외교 · 화룡 걸작 선물");
            L.Label(f, "작품과 목적을 직접 선택 · 성공 때만 소모 · 거절 후 같은 문명/목적 15일 대기 · 성공률 +30%p(상한100%)", 13, 48, 48, 1344, 40);
            var list = L.List(f, 48, 100, 1344, 620);
            foreach (var artwork in d.GiftArtworks)
            {
                var a = artwork;
                MenuTheme.Button(list, (giftArtwork == a ? "선택됨 · " : "작품 선택 · ") + a.name + " · " + a.ArtistId, () => { giftArtwork = a; ArtworkDiplomacy(); });
            }
            if (!d.GiftArtworks.Any()) MenuTheme.Text(list, "전 과정을 화룡 보유자가 제작한 걸작(품질3)이 없습니다.", 13, 28);
            foreach (var civ in d.Data.civilizations.Where(c => !c.extinct))
            {
                var c = civ;
                MenuTheme.Text(list, c.name + $" · 수락 확률 {DiplomacyManager.ArtworkChance(c.affinity):P0}", 15, 28);
                void Offer(ArtworkPurpose purpose, string label, string prisoner = null)
                {
                    var reason = d.ArtworkReason(c, purpose, prisoner);
                    var button = MenuTheme.Button(list, label + (reason == "" ? "" : " · " + reason), () =>
                    { ToastManager.Show(d.OfferArtwork(c, giftArtwork, purpose, prisoner)); giftArtwork = null; ArtworkDiplomacy(); });
                    button.interactable = reason == "" && giftArtwork != null;
                }
                if (!c.contacted) Offer(ArtworkPurpose.Contact, "접촉 선물");
                else
                {
                    if (c.war) Offer(ArtworkPurpose.Peace, "휴전 선물 · 기존 배상 유지");
                    foreach (var id in d.RansomableIds(c).ToArray()) Offer(ArtworkPurpose.Release, "석방 요청 · " + id, id);
                }
            }
            L.Button(f, "Back", "외교로 돌아가기", 1180, 764, 212, 34, Diplomacy);
        }
    }
}
