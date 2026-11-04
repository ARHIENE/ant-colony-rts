using System;
using AntColony.Buildings;
using AntColony.Boss;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void BossProgressionSection(CampaignResearch research)
        {
            var progress = research.BossProgress;
            MenuTheme.Text(content, "보스 탐지 · A → B/C → D", 15, 35);
            MenuTheme.Text(content, BossEncounterRules.BranchAdvice, 13, 65);
            MenuTheme.Text(content, "보스는 단일 체력과 비행·착지 패턴으로 표현합니다. 장수의 신체 부위 손상·절단·의료 규칙은 적용하지 않습니다. 호위는 식용 불가 야생종 시신을 남깁니다. 호위 대상 전투 시간·처치 경험치는 모든 장수 합산으로 보스 회차당 총200까지입니다.", 13, 90);
            MenuTheme.Text(content, "탐험으로 보급·재료·표본을 준비하고, 핵심물을 본거지에 반입해 탐지기를 제작하세요. 보스 보상은 핵심물 복구 제작과 달리 재공략에서만 장비를 다시 얻습니다. 처치12일 후 현지 아군·수송수단이 모두 철수하면 재등장합니다.", 13, 85);
            foreach (BossId id in Enum.GetValues(typeof(BossId)))
            {
                var target = id;
                MenuTheme.Text(content, $"{id} · {BossProgress.Hazard(id)}\n보상: {BossProgress.Reward(id)}\n"
                    + $"최초 획득 {(progress.Obtained(id) ? "완료" : "미획득")} / 반입 {(progress.Delivered(id) ? "완료" : "미반입")} / 핵심물 {progress.Home(id)} / {progress.DetectionBlock(id)}", 13, 90);
                MenuTheme.Text(content, BossEncounterRules.Guide(id), 12, 72);
                if (id != BossId.A && !progress.Revealed(id))
                {
                    var cost = id == BossId.D ? "B·C 핵심물 각1" : "A 핵심물1 + 목재5 + 돌5";
                    MenuTheme.Button(content, $"{id} 탐지기 제작 ({cost}, 기본 작업60)", () => {
                        if (!progress.TryStartCraft(target, false, ResourceManager.Instance)) ToastManager.Show("선행 반입·핵심물·빈 제작대·진행 중인 제작을 확인하세요.");
                        ScienceFacilities();
                    }).interactable = progress.DetectionBlock(id) == "" && progress.Tools(id) == 0 && progress.CraftRemaining == 0;
                    MenuTheme.Button(content, $"{id} 탐지기 사용 · 위치 영구 공개", () => {
                        progress.UseDetector(target); ScienceFacilities();
                    }).interactable = progress.Tools(id) > 0;
                }
                if (progress.Obtained(id))
                    MenuTheme.Button(content, $"{id} 핵심물 1개 복구 ({BossProgress.EraMaterial(id).DisplayName()}100 + 부품10, 기본 작업60)", () => {
                        if (!progress.TryStartCraft(target, true, ResourceManager.Instance)) ToastManager.Show("빈 제작대 또는 진행 중인 제작을 확인하세요. 가공재·부품은 현장 배송 후 소비합니다.");
                        ScienceFacilities();
                    }).interactable = progress.CraftRemaining == 0;
            }
            MenuTheme.Text(content, "제작대의 빈 대기열에 직접 주문합니다. 재료 실물 배송 후 담당 장수가 작업하며, 일시중지·원정·담당 부재 중에는 진행하지 않습니다.", 13, 65);
            if (progress.CraftRemaining > 0)
            {
                MenuTheme.Text(content, $"진행 중인 제작: {progress.CraftRemaining:0} 작업량 남음 · 제작대에서 담당/배송 확인", 13, 45);
                if (!Workshop.HasBossJob)
                    foreach (var bench in SceneObjects.All<Workshop>())
                        MenuTheme.Button(content, "구저장 유료 제작 이어받기: " + bench.name, () => { progress.ResumeLegacyCraft(bench); ScienceFacilities(); });
            }
        }
    }
}
