using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public static class Review1101Q2Checks
{
    private static int checks;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("Q2 실패: " + name); checks++; }
    private static void Near(float value, float expected, string name) => Check(Math.Abs(value - expected) < .01f, name);
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<string> Main()
    {
        checks = 0;
        for (var n = 0; n <= 12; n++)
        for (var patients = 0; patients <= n; patients++)
            Check(AntColony.World.EventRules.RecoveryNeeded(n, patients) == (n > 0 && patients == n), "전원 환자 경계 " + n + "/" + patients);
        foreach (AntColony.Units.WoundKind kind in Enum.GetValues(typeof(AntColony.Units.WoundKind)))
        foreach (var tended in new[] { false, true })
        foreach (var resting in new[] { false, true })
        {
            var w = new AntColony.Units.Wound { kind = kind, part = AntColony.Units.BodyPart.ArmL, damage = 3, quality = .5f, tended = tended };
            var seconds = AntColony.Units.MedicalOutlook.WoundSeconds(w, resting);
            if (w.Permanent) Check(float.IsPositiveInfinity(seconds), "영구 손상 자연 회복 예측 금지");
            else Near(seconds, 3 / ((tended ? .02f : .005f) * (resting ? 1.5f : 1)), "회복 공식 " + kind);
        }
        var illness = new AntColony.Units.Illness { kind = AntColony.Units.IllnessKind.Mold };
        Check(AntColony.Units.MedicalOutlook.IllnessTrend(illness, false).Contains("222초"), "곰팡이 면역 시간");
        Check(AntColony.Units.MedicalOutlook.IllnessTrend(illness, true).Contains("171초"), "병상 면역 시간");
        illness.immunity = 1;
        Check(AntColony.Units.MedicalOutlook.IllnessTrend(illness, true).Contains("회복 중"), "면역 완성");
        var h = new AntColony.Units.HealthState();
        Check(AntColony.UI.GameMenuController.HealthLines(h) == "이상 없음", "건강한 상태");
        h.wounds.Add(new AntColony.Units.Wound { kind = AntColony.Units.WoundKind.Scar, part = AntColony.Units.BodyPart.ArmL, damage = 1 });
        var line = AntColony.UI.GameMenuController.HealthLines(h);
        Check(line.Contains("영구 손상") && !line.Contains("회복 약"), "흉터를 회복 대기로 오인하지 않음");
        h.wounds.Clear(); h.wounds.Add(new AntColony.Units.Wound { kind = AntColony.Units.WoundKind.Cut, part = AntColony.Units.BodyPart.ArmL, damage = 3 });
        Check(AntColony.Units.MedicalOutlook.RecoveryLines(h, false).Contains("600초"), "미처치 회복 시간");
        var talents = new AntColony.Units.CommanderTalents();
        var skill = AntColony.Units.CommanderActivity.Medicine;
        talents.levels[(int)skill] = 11;
        Check(AntColony.Units.CommanderDevelopment.TrainingText(talents, skill).Contains("0/1200"), "다음 기술 성장 목표");
        talents.usage[(int)AntColony.Units.CommanderActivity.Gathering] = 600;
        Check(AntColony.Units.CommanderDevelopment.TrainingText(talents, skill).Contains("활용 부족"), "미활용 감소 예고");
        talents.levels[(int)skill] = 10;
        Check(!AntColony.Units.CommanderDevelopment.TrainingText(talents, skill).Contains("활용 부족"), "10단계 감소 하한");
        talents.levels[(int)skill] = 20;
        Check(AntColony.Units.CommanderDevelopment.TrainingText(talents, skill).Contains("최고 단계"), "만렙 목표");
        Check(Enumerable.Range(0, 7).Select(AntColony.UI.CampaignNextSteps.ParallelGoal).All(s => s.Length > 10), "모든 시대 병행 목표");
        Check(Enumerable.Range(1, 6).Select(AntColony.UI.CampaignNextSteps.ParallelGoal).Distinct().Count() == 6, "시대별 생활 목표 구별");
        await Task.CompletedTask;
        return $"Q2 오프라인 {checks}검사 통과 · Unity 장면 미실행";
    }

    // Claude가 실제 Play 장면에서 실행. 기존 부상·의료 명령은 finally에서 원래 참조로 복원한다.
    public static async Task<string> RunQ2Scene()
    {
        var roster = AntColony.Core.CommanderRoster.Instance;
        if (roster == null) throw new Exception("Play 장면·장수 명부 필요");
        var all = roster.Commanders.Where(c => c != null && c.CivilianWorkReady && c.CanReceiveOrders && c.MedicalJob == AntColony.Units.MedJob.None).Take(2).ToArray();
        if (all.Length != 2) throw new Exception("의료 작업 중이 아닌 활동 가능 장수 2명이 필요합니다(낮에 실행)");
        var doctor = all[0]; var patient = all[1];
        var oldHealth = patient.PersonalState.health;
        var fields = new[] { "medJob", "medPatient", "medWork", "medCamp", "medPrisoner", "rescueBed" }.Select(n => typeof(AntColony.Units.CommanderAnt).GetField(n, Private)).ToArray();
        var old = fields.Select(f => f.GetValue(doctor)).ToArray();
        var timeScale = UnityEngine.Time.timeScale;
        try
        {
            UnityEngine.Time.timeScale = 0;
            patient.PersonalState.health = new AntColony.Units.HealthState();
            fields[0].SetValue(doctor, AntColony.Units.MedJob.Tend); fields[1].SetValue(doctor, patient); fields[2].SetValue(doctor, 5f);
            Check(AntColony.Units.CommanderAnt.CareProgress(patient).Contains(doctor.CommanderName), "실제 담당 표시");
            var tick = typeof(AntColony.Units.CommanderAnt).GetMethod("TickMed", Private);
            Check(!(bool)tick.Invoke(doctor, new object[] { 1f }) && doctor.MedicalJob == AntColony.Units.MedJob.None, "회복한 대상 처치 중단");
            patient.Health.wounds.Add(new AntColony.Units.Wound { kind = AntColony.Units.WoundKind.Cut, damage = 2, part = AntColony.Units.BodyPart.ArmL });
            patient.Health.medicine = AntColony.Units.MedicineLevel.NoCare;
            fields[0].SetValue(doctor, AntColony.Units.MedJob.Tend); fields[1].SetValue(doctor, patient);
            Check(!(bool)tick.Invoke(doctor, new object[] { 1f }) && !patient.Health.wounds[0].tended, "이동 중 치료 금지 변경 반영");
            patient.Health.wounds.Add(new AntColony.Units.Wound { kind = AntColony.Units.WoundKind.Missing, part = AntColony.Units.BodyPart.ArmL });
            Check(AntColony.Units.CommanderAnt.RequestBlock(patient, AntColony.Units.SurgeryKind.Amputate, AntColony.Units.BodyPart.HandL).Contains("먼저"), "상실한 팔 아래 손 수술 금지");
            Check(!string.IsNullOrEmpty(AntColony.UI.CampaignNextSteps.Text()), "실제 판 진행 안내");
        }
        finally
        {
            patient.PersonalState.health = oldHealth;
            for (var i = 0; i < fields.Length; i++) fields[i].SetValue(doctor, old[i]);
            UnityEngine.Time.timeScale = timeScale;
        }
        await Task.CompletedTask;
        return "Q2 장면 5검사 통과 · 구조 이동·집도의 교대·화면 크기 수동 확인 필요";
    }
}
