using System;
using AntColony.World;
using UnityEngine;

// Pipeline eval_file/RunChecks에서 실행. 실제 장면을 바꾸지 않는 상태·저장 회귀 검사.
public static class Spec1013AnimalHealthChecks
{
    public static string Main()
    {
        int n = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); n++; }
        var d = new AnimalDiseaseState { kind = AnimalDisease.Contagious, severity = .1f };
        Check(!d.Symptomatic && !d.Infectious, "잠복기 전염 금지");
        d.Tick(.3f); Check(d.Symptomatic && d.Infectious, "증상 이후 전염");
        d.medication = 1;
        d.Tick(.1f); Check(d.Infectious, "투약 즉시 전염 소멸 금지");
        var copy = JsonUtility.FromJson<AnimalDiseaseState>(JsonUtility.ToJson(d));
        Check(copy.Valid && copy.elapsed == d.elapsed && copy.medication == d.medication, "저장 왕복 진행도·약효");
        copy.Tick(4); Check(copy.kind == AnimalDisease.None && !copy.Infectious, "회복 후 전염 해제");
        var food = new AnimalDiseaseState { kind = AnimalDisease.FoodPoisoning, severity = .3f, medication = 1 };
        food.Tick(.1f); Check(food.severity > .3f, "식중독은 약으로 완치하지 않음");
        food.Tick(1.2f); Check(food.kind == AnimalDisease.None, "식중독 경과 회복");
        Check(Critter.SuccessChance(10, .2f, 0) == Critter.SuccessChance(10, .2f, 99), "수술 실패 횟수 성공률 벌점 없음");
        Check(Critter.SuccessChance(10, .1f, 0) < Critter.SuccessChance(10, .3f, 0), "실제 부상 악화는 위험 증가");
        Check(Critter.DeathOnFail(0) == Critter.DeathOnFail(99), "실패 횟수 사망 벌점 없음");
        d.elapsed = float.NaN; Check(!d.Valid, "손상 저장 거부");
        return $"PASS {n} animal health checks";
    }
}
