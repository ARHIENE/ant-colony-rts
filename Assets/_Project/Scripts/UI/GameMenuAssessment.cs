using System.Linq;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public static string RatingStars(float value) => new string('★', Mathf.FloorToInt(value)) + (value % 1 >= .5f ? "½" : "") + $" {value:0.#}/5";
        public static string TalentSummary(string id)
        {
            var rating = WorldMapManager.Instance?.Assessment.Rating(id);
            return !rating.HasValue ? "미평가" : $"현재 {RatingStars(rating.Value.Current)} · 잠재 " + PotentialStars(rating.Value);
        }
        private static string PotentialStars(TalentRating r) => r.ExactPotential ? RatingStars(r.PotentialLow) + " (정확 평가)"
            : $"{r.PotentialLow:0.#}~{r.PotentialHigh:0.#}/5 (추정)";
        public void ShowAssessment(string target = null)
        {
            var assessment = WorldMapManager.Instance?.Assessment;
            if (assessment == null) { Resume(); return; }
            var window = V5Window("인재 평가", "우리 성인 장수 기준 상대평가 · 일반 잠재력은 추정치");
            ManagementTabs(window, "인재·편성", "평가");
            var left = V5Section(window, "평가 담당 / 보고서", 12, 112, 530, 536);
            var evaluator = SortedCommanders().FirstOrDefault(c => c.PersonalState.id == assessment.EvaluatorId);
            MenuTheme.Text(left, evaluator == null ? "평가 담당 없음 · 기존 보고 보존" : $"담당: {evaluator.CommanderName} · 조사 {evaluator.Talents.Level(CommanderActivity.Investigation)}"
                + (evaluator.IsAwayFromHome ? " · 파견 중, 갱신 대기" : $" · 최초 평가 {assessment.Progress:0.#}/10초"), 13, 48);
            foreach (var c in SortedCommanders().Where(c => c.IsColonyMember && !c.IsChild && !c.IsCaptive))
            {
                var pick = c;
                MenuTheme.Button(left, "평가 담당 지정: " + c.CommanderName, () => { assessment.AssignEvaluator(pick); ShowAssessment(target); });
            }
            MenuTheme.Button(left, "평가 담당 해제", () => { assessment.AssignEvaluator(null); ShowAssessment(target); });
            foreach (var report in assessment.Reports.OrderByDescending(r => r.interested).ThenBy(r => r.name))
            {
                var id = report.target;
                MenuTheme.Button(left, (report.interested ? "관심 · " : "") + report.name + " · " + GameCalendar.Format(report.at), () => ShowAssessment(id));
            }
            if (assessment.Reports.Count == 0) MenuTheme.Text(left, "아직 평가된 인재가 없습니다. 담당 지정 후 게임을 진행하세요.", 13, 56);
            var right = V5Section(window, "평가 내용", 554, 112, 534, 536);
            var selected = assessment.Reports.FirstOrDefault(r => r.target == target) ?? assessment.Reports.FirstOrDefault();
            var rating = selected == null ? null : assessment.Rating(selected.target);
            if (selected != null && rating.HasValue)
            {
                var r = rating.Value;
                MenuTheme.Text(right, selected.name + "\n종합 현재 능력 " + RatingStars(r.Current) + "\n예상 잠재력 " + PotentialStars(r), 15, 86);
                MenuTheme.Text(right, "평가일 " + GameCalendar.Format(r.At) + $" · 현재 평가 기준 {r.BasisVersion} / 잠재 기준 {r.PotentialBasisVersion}\n비교 기준 변경 전의 일반 평가는 다음 갱신까지 보존합니다.", 12, 56);
                MenuTheme.Text(right, "별점은 우리 성인 장수와 비교한 평가입니다. 더 강한 인재가 합류하면 다음 평가에서 별점이 내려갈 수 있으며, 그 자체로 개인 기술이 감소한 것은 아닙니다.\n연구자·요리사 등은 아래 해당 분야를 비교하세요. 종합 별점이 모든 직무의 적합성을 뜻하지는 않습니다. 잠재력도 도달 보장이 아닙니다.", 12, 90).color = MenuTheme.Muted;
                MenuTheme.Button(right, selected.interested ? "관심 등록 해제" : "관심 등록", () => { assessment.SetInterested(selected.target, !selected.interested); ShowAssessment(selected.target); });
                for (var i = 0; i < r.Skills.Length; i++) MenuTheme.Text(right, SkillNames[i] + "  " + RatingStars(r.Skills[i]), 13, 24);
            }
            else MenuTheme.Text(right, "미평가 · 평가 담당자를 지정하세요. 보고서에서 직접 영입할 수는 없습니다.", 13, 64);
            MenuTheme.Button(right, "새로고침", () => ShowAssessment(target));
            V5Fit(window);
        }
    }
}
