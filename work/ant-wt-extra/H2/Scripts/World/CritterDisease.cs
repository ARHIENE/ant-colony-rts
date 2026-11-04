using System;
using System.Linq;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.World
{
    // 질병명·기간·확률은 종 목록 확정 전 잠정값. 장수 질병과 공유하지 않는다.
    public enum AnimalDisease { None, FoodPoisoning, WoundInfection, Illness, Contagious }

    [Serializable]
    public sealed class AnimalDiseaseState
    {
        public AnimalDisease kind;
        public float elapsed, severity, medication;
        public bool announced, criticalAnnounced;
        public bool Symptomatic => kind != AnimalDisease.None && elapsed >= .25f;
        public bool Severe => Symptomatic && severity >= .65f;
        public bool Infectious => Symptomatic && kind == AnimalDisease.Contagious;
        public bool Valid => Enum.IsDefined(typeof(AnimalDisease), kind) && Finite(elapsed) && Finite(severity)
            && severity <= 1 && Finite(medication);
        private static bool Finite(float x) => x >= 0 && !float.IsInfinity(x);

        public void Tick(float months)
        {
            if (kind == AnimalDisease.None || !(months > 0) || float.IsInfinity(months)) return;
            elapsed += months;
            // 식중독은 경과를 기다린다. 의약품으로 즉시 완치하지 않는다.
            float duration = kind == AnimalDisease.FoodPoisoning ? 1.2f : 4f;
            float treated = kind == AnimalDisease.FoodPoisoning ? 0 : Mathf.Min(months, medication);
            medication = Mathf.Max(0, medication - months);
            severity = Mathf.Clamp01(severity + months * .22f - treated * .5f);
            if (elapsed >= duration)
            {
                kind = AnimalDisease.None; elapsed = severity = medication = 0;
                announced = criticalAnnounced = false;
            }
        }
    }

    public sealed partial class Critter
    {
        public AnimalDiseaseState Disease => s.disease ??= new AnimalDiseaseState();
        public bool NeedsClinicFood => Clinic != null && !HungerPausedInClinic && Hunger < GameBalance.CritterFullHunger;
        public bool Sick => Disease.Symptomatic;
        public bool Contagious => Disease.Infectious;
        public bool SevereDisease => Disease.Severe;
        public bool NeedsDiseaseMedicine => Sick && Disease.kind != AnimalDisease.FoodPoisoning && Disease.medication <= 0;
        public string DiseaseStatus => !Sick ? "질병 증상 없음" :
            (Disease.kind == AnimalDisease.FoodPoisoning ? "식중독" : Disease.kind == AnimalDisease.WoundInfection ? "상처 감염"
                : Disease.kind == AnimalDisease.Contagious ? "전염성 질환" : "질환")
            + (SevereDisease ? " · 중증(생산 중단)" : " · 경증(생산 감소)")
            + (Contagious ? " · 전염 가능, 다른 우리로 격리" : "")
            + (Disease.medication > 0 ? " · 투약 효과 중" : "");

        public bool ContractDisease(AnimalDisease kind)
        {
            if (kind == AnimalDisease.None || !Enum.IsDefined(typeof(AnimalDisease), kind) || Disease.kind != AnimalDisease.None) return false;
            s.disease = new AnimalDiseaseState { kind = kind, severity = .1f };
            return true;
        }

        private void TickDisease(float months)
        {
            bool wasSick = Sick;
            Disease.Tick(months);
            if (Sick && !Disease.announced)
            {
                Disease.announced = true;
                if (Pen == null && ReturnPen == null) ToastManager.ShowAt($"{DisplayName}: {DiseaseStatus}", FocusAction(transform.position));
            }
            if (SevereDisease && !Disease.criticalAnnounced)
            {
                Disease.criticalAnnounced = true;
                if (Pen == null && ReturnPen == null) ToastManager.ShowAt($"{DisplayName}: 질병 중증 — 치료가 필요합니다.", FocusAction(transform.position));
            }
            if (wasSick && !Sick && Pen != null && !Pen.Animals.Any(a => a != this && a.Sick))
                ToastManager.ShowAt($"{Pen.Data.displayName}: 아픈 동물이 모두 회복했습니다.", FocusAction(Pen.Position));
            if (SevereDisease) Wound(Info.health * .03f * months, "질병");
            if (Clinic != null || Carrier != null || !InOperatingPen) return;
            if (Contagious)
            {
                // 종별 감수성 표가 정해지기 전에는 같은 종만 전염된다. 잠복기·치료대에서는 전염하지 않는다.
                foreach (var other in Pen.Animals.Where(a => a != this && a.Species == Species && a.InOperatingPen && a.Carrier == null).ToArray())
                    if (UnityEngine.Random.value < 1 - Mathf.Exp(-months * .35f)) other.ContractDisease(AnimalDisease.Contagious);
            }
            // 우리 위생(배설물)이 나쁠수록 발병 위험이 오른다(즉시 체력 감소 없음, 2026-10-13).
            else if (Disease.kind == AnimalDisease.None && UnityEngine.Random.value < 1 - Mathf.Exp(-months * .005f * (1 + 4 * PenHygiene.Risk(Pen)))) ContractDisease(AnimalDisease.Contagious);
            else if (Injured && UnityEngine.Random.value < 1 - Mathf.Exp(-months * .03f)) ContractDisease(AnimalDisease.WoundInfection);
        }
    }
}
