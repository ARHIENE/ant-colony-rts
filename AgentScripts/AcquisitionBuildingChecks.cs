namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Core;
    using AntColony.Data;
    using AntColony.UI;
    using AntColony.Units;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // 장수 획득 건물 3종의 건설 연동(템플릿·건설 완료·중복 건물)과 HUD 패널 검사.
    // 획득 규칙 자체(번식·영입·포로 확률)는 CommanderAcquisitionChecks가 담당한다.
    public static class AcquisitionBuildingChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");

            var origin = new Vector3(1400f, 0f, 1400f);
            var objects = new List<Object>();
            var passed = new List<string>();

            void Check(bool condition, string label)
            {
                if (!condition) throw new Exception("FAIL: " + label);
                passed.Add(label);
            }

            GameObject New(string name, Vector3 position, bool active)
            {
                var go = new GameObject(name);
                go.transform.position = position;
                go.SetActive(active);
                objects.Add(go);
                return go;
            }

            BuildingData Data(BuildingKind kind, int soil, float buildTime, int ants)
            {
                var data = ScriptableObject.CreateInstance<BuildingData>();
                data.displayName = kind.ToString();
                data.kind = kind;
                data.maxHealth = 100f;
                data.foodCost = 0;
                data.soilCost = soil;
                data.buildTimeSeconds = buildTime;
                data.constructionAnts = ants;
                objects.Add(data);
                return data;
            }

            try
            {
                var pool = AntPool.Instance;
                var placement = Object.FindFirstObjectByType<BuildingPlacementController>();
                Check(pool != null && placement != null, "씬의 개미 풀과 배치 컨트롤러를 찾는다");

                // 검사 시작 시점의 활성 건물 상태. 템플릿이 여기에 끼어들지 않아야 한다.
                var nurseryBefore = NurseryChamber.Primary;
                var campBefore = PrisonerCamp.Instance;

                // ================= 1. 템플릿과 건설 연동 =================
                var nurseryTemplate = New("NurseryTemplate", origin, false);
                var nurseryBuilding = nurseryTemplate.AddComponent<BuildingBase>();
                SetPrivate(nurseryBuilding, "data", Data(BuildingKind.Nursery, 40, 6f, 5));
                nurseryTemplate.AddComponent<NurseryChamber>();

                var campTemplate = New("PrisonerCampTemplate", origin + new Vector3(3f, 0f, 0f), false);
                var campBuilding = campTemplate.AddComponent<BuildingBase>();
                SetPrivate(campBuilding, "data", Data(BuildingKind.PrisonerCamp, 50, 7f, 6));
                campTemplate.AddComponent<PrisonerCamp>();

                var scoutTemplate = New("ScoutPostTemplate", origin + new Vector3(6f, 0f, 0f), false);
                var scoutBuilding = scoutTemplate.AddComponent<BuildingBase>();
                SetPrivate(scoutBuilding, "data", Data(BuildingKind.ScoutPost, 30, 5f, 4));
                scoutTemplate.AddComponent<ScoutPost>();
                await Task.Yield();

                // 비활성 템플릿은 건물로 동작하면 안 된다. 그렇지 않으면 짓기도 전에 포로를 받고 번식이 돈다.
                Check(NurseryChamber.Primary == nurseryBefore, "비활성 양육실 템플릿은 번식 주체로 등록되지 않는다");
                Check(PrisonerCamp.Instance == campBefore, "비활성 수용소 템플릿은 수용소로 등록되지 않는다");

                // 배치 컨트롤러가 새 건물 종류의 템플릿을 실제로 찾아낸다.
                Check(!placement.GetNurseryBuildLabel().Contains("Unavailable"), "양육실 건설 라벨이 템플릿을 찾는다");
                Check(!placement.GetScoutPostBuildLabel().Contains("Unavailable"), "파견소 건설 라벨이 템플릿을 찾는다");
                Check(!placement.GetPrisonerCampBuildLabel().Contains("Unavailable"), "수용소 건설 라벨이 템플릿을 찾는다");
                Check(placement.GetNurseryBuildLabel().Contains("Ants"), "건설 라벨에 비용과 인력이 표시된다");

                // 건설 완료 경로: 예약한 개미가 돌아오고 건물이 활성화된다.
                var pending = Object.Instantiate(campTemplate, origin + new Vector3(9f, 0f, 0f), Quaternion.identity);
                pending.name = BuildingKind.PrisonerCamp.ToString();
                pending.SetActive(false);
                objects.Add(pending);

                var freeBeforeBuild = pool.Free;
                Check(pool.TryReserve(6), "건설 인력을 예약한다");
                Check(pool.Free == freeBeforeBuild - 6, "예약한 개미가 대기 풀에서 빠진다");

                var siteGo = New("CheckConstructionSite", origin + new Vector3(9f, 0f, 0f), true);
                var site = siteGo.AddComponent<BuildingConstructionSite>();
                site.Initialize(pending, 7f, pool, 6);
                Check(!pending.activeSelf && PrisonerCamp.Instance == campBefore, "건설 중에는 수용소가 동작하지 않는다");

                site.Complete();
                await Task.Yield();
                Check(pending.activeSelf, "건설이 끝나면 건물이 활성화된다");
                Check(pool.Free == freeBeforeBuild, "건설 인력이 대기 풀로 돌아온다");

                var builtCamp = pending.GetComponent<PrisonerCamp>();
                Check(builtCamp != null && builtCamp.enabled, "완공된 수용소 컴포넌트가 살아 있다");
                Check(PrisonerCamp.Instance == builtCamp, "완공된 수용소가 포로 수용 창구가 된다");

                // ================= 2. 수용소를 여러 채 지어도 모두 동작한다 =================
                var secondCampGo = New("PrisonerCamp 2", origin + new Vector3(12f, 0f, 0f), false);
                var secondCamp = secondCampGo.AddComponent<PrisonerCamp>();
                SetPrivate(secondCamp, "capacity", 2);
                SetPrivate(secondCamp, "escapeChance", 0f);
                SetPrivate(builtCamp, "capacity", 1);
                SetPrivate(builtCamp, "escapeChance", 0f);
                secondCampGo.SetActive(true);
                await Task.Yield();
                Check(secondCamp != null && secondCamp.enabled, "두 번째 수용소가 파괴되지 않는다");

                var traits = new CommanderTraits(CommanderPersonality.Brave, 10);
                Check(PrisonerCamp.Instance == builtCamp, "자리가 남아 있으면 먼저 지은 수용소를 쓴다");
                Check(builtCamp.TryCapture("POW A", CommanderRank.Sergeant, new[] { UnitRole.Worker }, traits),
                    "첫 수용소가 포로를 받는다");
                Check(PrisonerCamp.Instance == secondCamp, "첫 수용소가 차면 자리가 남은 수용소를 고른다");

                var camp = PrisonerCamp.Instance;
                Check(camp.TryCapture("POW B", CommanderRank.Sergeant, new[] { UnitRole.Worker }, traits),
                    "자리가 남은 수용소가 포로를 받는다");
                Check(secondCamp.Count == 1, "포로가 자리 있는 수용소에 들어간다");

                // 전부 차면 Instance는 여전히 수용소를 돌려주되 수용은 거부된다(적 장수는 평소대로 죽는다).
                secondCamp.TryCapture("POW C", CommanderRank.Sergeant, new[] { UnitRole.Worker }, traits);
                Check(PrisonerCamp.Instance != null && !PrisonerCamp.Instance.HasSpace, "모두 차면 자리 없음으로 보고된다");

                // ================= 3. 파견 중 파괴돼도 개미가 돌아온다 =================
                var scoutGo = New("Check Scout Post", origin, true);
                var scout = scoutGo.AddComponent<ScoutPost>();
                SetPrivate(scout, "dispatchFoodCost", 0);
                SetPrivate(scout, "dispatchAnts", 2);
                SetPrivate(scout, "travelSeconds", 100f);

                var freeBeforeScout = pool.Free;
                var assignedBeforeScout = pool.Assigned;
                Check(scout.TryDispatch(), "스카우트를 파견한다");
                Check(pool.Free == freeBeforeScout - 2, "파견 개미가 대기 풀에서 빠진다");

                scoutGo.SetActive(false);
                await Task.Yield();
                Check(!scout.IsDispatched, "파견소가 꺼지면 파견이 취소된다");
                Check(pool.Free == freeBeforeScout && pool.Assigned == assignedBeforeScout,
                    "파견 중 파견소가 꺼져도 개미가 돌아온다");

                scoutGo.SetActive(true);
                scoutGo.SetActive(false);
                await Task.Yield();
                Check(pool.Free == freeBeforeScout, "이미 돌아온 개미를 두 번 반납하지 않는다");

                // ================= 4. 양육실을 여러 채 지어도 번식이 배로 빨라지지 않는다 =================
                var firstNurseryGo = New("Check Nursery 1", origin, true);
                var firstNursery = firstNurseryGo.AddComponent<NurseryChamber>();
                var secondNurseryGo = New("Check Nursery 2", origin + new Vector3(2f, 0f, 0f), true);
                var secondNursery = secondNurseryGo.AddComponent<NurseryChamber>();
                await Task.Yield();
                Check(NurseryChamber.Primary == firstNursery, "먼저 지은 양육실이 번식을 주도한다");

                // 몇 프레임 돌려도 두 번째 양육실은 호감도를 쌓지 않는다(중복 출산 방지).
                await Task.Yield();
                await Task.Yield();
                var roster = CommanderRoster.Instance;
                var sample = roster != null && roster.Count >= 2 ? roster.Commanders : null;
                if (sample != null)
                    Check(secondNursery.GetAffinity(sample[0], sample[1]) == 0f,
                        "두 번째 양육실은 호감도를 쌓지 않는다");
                Check(secondNursery.BirthCount == 0, "두 번째 양육실은 출산하지 않는다");
                Check(secondNursery.GetStatusLabel().Contains("idle"), "두 번째 양육실은 대기 중이라고 표시된다");
                // 기획 확정 전까지 조건은 "장수 쌍 사이 거리"다. 문구도 그대로여야 한다.
                Check(firstNursery.GetStatusLabel().Contains("of each other"),
                    "양육실 표시가 장수 쌍 사이 거리 조건을 그대로 설명한다");

                firstNurseryGo.SetActive(false);
                secondNurseryGo.SetActive(false);

                // ================= 5. HUD 패널 =================
                // 패널이 어느 수용소를 보는지 확정한다(수용소를 하나만 남긴다).
                pending.SetActive(false);
                SetPrivate(secondCamp, "capacity", 5);
                secondCamp.TryCapture("POW D", CommanderRank.Sergeant, new[] { UnitRole.Worker }, traits);
                await Task.Yield();
                Check(PrisonerCamp.Instance == secondCamp, "남은 수용소가 하나면 그 수용소가 창구가 된다");

                var canvasGo = New("Check Acquisition Canvas", Vector3.zero, false);
                canvasGo.AddComponent<Canvas>();
                var panel = canvasGo.AddComponent<CommanderAcquisitionPanel>();
                canvasGo.SetActive(true);
                await Task.Yield();
                await Task.Yield();

                // 선택은 인덱스가 아니라 포로 자체다. 앞 포로가 사라져도 같은 포로를 계속 가리켜야 한다.
                Check(panel.SelectedPrisoner != null, "패널이 포로를 선택한다");

                panel.SelectNextPrisoner();
                await Task.Yield();
                var picked = panel.SelectedPrisoner;
                Check(picked != null && picked != secondCamp.Prisoners[0], "다음 포로로 선택을 옮긴다");

                secondCamp.Execute(0);
                await Task.Yield();
                Check(panel.SelectedPrisoner == picked, "앞 포로가 사라져도 선택한 포로가 바뀌지 않는다");

                // 선택한 포로가 사라지면 남은 포로로 넘어간다(선택이 빈 채로 굳지 않는다).
                secondCamp.Execute(picked);
                await Task.Yield();
                Check(panel.SelectedPrisoner != picked, "사라진 포로는 선택에서 풀린다");

                // 비용을 못 내면 회유를 시도하지 않고 이유를 알린다.
                SetPrivate(secondCamp, "persuadeFoodCost", 1000000);
                await Task.Yield();
                var attemptsBefore = panel.SelectedPrisoner != null ? panel.SelectedPrisoner.PersuadeAttempts : 0;
                panel.Persuade();
                Check(panel.Feedback.Contains("Not enough food"), "식량이 모자라면 회유 실패 이유를 알린다");
                if (panel.SelectedPrisoner != null)
                    Check(panel.SelectedPrisoner.PersuadeAttempts == attemptsBefore,
                        "비용을 못 내면 회유 시도로 세지 않는다");

                secondCamp.enabled = false;
                panel.CycleCamp();
                Check(panel.Camp == null, "컴포넌트가 꺼진 수용소는 HUD 대상에서 제외된다");
                var prisonersBefore = secondCamp.Count;
                panel.ExecuteSelected();
                Check(secondCamp.Count == prisonersBefore, "비활성 수용소의 포로를 HUD로 처형할 수 없다");

                scoutGo.SetActive(true);
                scout.enabled = false;
                panel.CycleScout();
                Check(panel.Scout == null, "컴포넌트가 꺼진 파견소는 HUD 대상에서 제외된다");
                var freeBeforeDisabledDispatch = pool.Free;
                panel.Dispatch();
                Check(!scout.IsDispatched && pool.Free == freeBeforeDisabledDispatch,
                    "비활성 파견소는 HUD 파견으로 개미를 차출하지 않는다");

                return "PASS: " + passed.Count + " acquisition building checks (construction, duplicates, HUD)";
            }
            finally
            {
                for (var i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            }
        }

        // 검사에서만 쓰는 직렬화 필드 주입. 인스펙터 값을 코드로 대신 설정한다.
        private static void SetPrivate(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(field,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new Exception("필드를 찾지 못했다: " + field);
        }

    }
}
