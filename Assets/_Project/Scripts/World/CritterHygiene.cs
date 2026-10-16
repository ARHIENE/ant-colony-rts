using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 동물 배설물(2026-10-13 확정: 오염·청소 제외 규칙의 유일한 예외). 우리 안 관리 동물이 그 자리에 배설물 바닥 물품을 남기고,
    // 운반 장수가 일반 바닥 물자처럼 창고·퇴비통으로 수거한다. 우리 면적 대비 배설물 양이 위생 관련 발병 위험을 올린다.
    public sealed partial class Critter
    {
        // ponytail: 배설량은 종·단계별 미정 → 성체 월 2, 새끼 월 1(잠정). 야생 개체는 배설물을 남기지 않는다(맵 전체 오염 방지).
        public const float ManurePerMonth = 2f;
        private void TickExcretion(float months)
        {
            if (!InOperatingPen || Clinic != null || Carrier != null || !(months > 0)) return;
            s.excrete += months * ManurePerMonth * (Stage == CritterStage.Baby ? .5f : 1f);
            while (s.excrete >= 1f) { s.excrete -= 1f; DiplomacyManager.DropFloor(ResourceType.Manure, 1, transform.position); }
        }
    }

    public static class PenHygiene
    {
        // 우리 방 안 바닥 배설물 양(창고·퇴비통에 들어간 것은 위생과 무관).
        public static float ManureIn(Ranch pen)
        {
            var room = pen?.Room; if (room == null) return 0;
            return ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == ResourceType.Manure && RoomSystem.RoomAt(n.transform.position) == room).Sum(n => n.AmountRemaining);
        }
        // 0 양호 ~ 1 위험. ponytail: 칸당 0.5개를 위험 기준으로 둔 잠정식(방치 시간 가중은 미반영).
        public static float Risk(Ranch pen) => pen == null || pen.Cells <= 0 ? 0 : Mathf.Clamp01(ManureIn(pen) / (pen.Cells * .5f));
        public static string Level(float risk) => risk < .3f ? "양호" : risk < .7f ? "주의" : "위험";
    }
}
