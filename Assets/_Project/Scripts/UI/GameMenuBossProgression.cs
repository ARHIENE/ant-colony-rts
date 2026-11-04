using System;
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
                    MenuTheme.Button(content, $"{id} 탐지기 제작 ({cost}, 60초)", () => {
                        if (!progress.TryStartCraft(target, false, ResourceManager.Instance)) ToastManager.Show("선행 반입·핵심물·재료·진행 중인 제작을 확인하세요.");
                        ScienceFacilities();
                    }).interactable = progress.DetectionBlock(id) == "" && progress.Tools(id) == 0 && progress.CraftRemaining == 0;
                    MenuTheme.Button(content, $"{id} 탐지기 사용 · 위치 영구 공개", () => {
                        progress.UseDetector(target); ScienceFacilities();
                    }).interactable = progress.Tools(id) > 0;
                }
                if (progress.Obtained(id))
                    MenuTheme.Button(content, $"{id} 핵심물 1개 복구 ({BossProgress.EraMaterial(id).DisplayName()}100 + 부품10, 60초)", () => {
                        if (!progress.TryStartCraft(target, true, ResourceManager.Instance)) ToastManager.Show("가공재·부품 또는 진행 중인 제작을 확인하세요.");
                        ScienceFacilities();
                    }).interactable = progress.CraftRemaining == 0;
            }
            if (progress.CraftRemaining > 0) MenuTheme.Text(content, $"진행 중인 제작: {progress.CraftRemaining:0}초 남음", 13, 30);
        }
    }
}
