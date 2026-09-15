namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using AntColony.Buildings;
    using AntColony.Data;
    using AntColony.World;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // 에디터 설정 스크립트(Tools/Ant Colony/Setup Commander Acquisition Buildings)의 결과 검사.
    // Play 모드가 필요 없다. 두 번 실행한 뒤 돌리면 멱등성(템플릿이 하나씩만 생김)까지 확인된다.
    public static class AcquisitionSetupChecks
    {
        public static string Main()
        {
            var passed = new List<string>();

            void Check(bool condition, string label)
            {
                if (!condition) throw new Exception("FAIL: " + label);
                passed.Add(label);
            }

            // 씬에서 이름이 같은 오브젝트를 전부 모은다. 멱등성이 깨지면 여기서 2개 이상이 잡힌다.
            List<GameObject> FindAll(string objectName)
            {
                var found = new List<GameObject>();
                foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
                    if (transform.gameObject.scene.IsValid() && transform.gameObject.name == objectName)
                        found.Add(transform.gameObject);
                return found;
            }

            void CheckTemplate<T>(string id, BuildingKind kind) where T : Component
            {
                var name = $"{id}Template";
                var found = FindAll(name);
                Check(found.Count == 1, $"{name}이 씬에 정확히 하나 있다 (실제 {found.Count}개)");

                var template = found[0];
                Check(!template.activeSelf, $"{name}은 비활성이다");
                Check(template.GetComponent<T>() != null, $"{name}에 {typeof(T).Name}가 붙어 있다");

                var building = template.GetComponent<BuildingBase>();
                Check(building != null, $"{name}에 BuildingBase가 붙어 있다");
                Check(building.Data != null, $"{name}에 BuildingData가 연결돼 있다");
                Check(building.Data.kind == kind, $"{name}의 BuildingData 종류가 {kind}다");
                Check(building.Data.constructionAnts > 0, $"{name}에 건설 인력이 설정돼 있다");
                Check(template.GetComponent<Renderer>() != null, $"{name}에 배치 미리보기용 Renderer가 있다");
            }

            CheckTemplate<NurseryChamber>("Nursery", BuildingKind.Nursery);
            CheckTemplate<ScoutPost>("ScoutPost", BuildingKind.ScoutPost);
            CheckTemplate<PrisonerCamp>("PrisonerCamp", BuildingKind.PrisonerCamp);

            // 적 장수 템플릿. m_Script 교체가 실제로 EnemyCommander를 만들어냈는지 확인한다.
            var commanders = FindAll("EnemyCommanderTemplate");
            Check(commanders.Count == 1, $"EnemyCommanderTemplate이 씬에 정확히 하나 있다 (실제 {commanders.Count}개)");
            Check(!commanders[0].activeSelf, "EnemyCommanderTemplate은 비활성이다");
            var commander = commanders[0].GetComponent<EnemyCommander>();
            Check(commander != null, "EnemyCommanderTemplate의 스크립트가 EnemyCommander로 바뀌었다");

            // 모든 적 소굴이 같은 적 장수 템플릿을 쓴다. 하나라도 비어 있으면 그 소굴엔 장수가 안 붙는다.
            var invasions = Object.FindObjectsByType<ColonyInvasion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(invasions.Length > 0, "씬에 ColonyInvasion이 있다");
            foreach (var invasion in invasions)
            {
                var wired = GetPrivate(invasion, "commanderTemplate") as EnemyCommander;
                Check(wired != null, $"{invasion.name}의 commanderTemplate이 연결돼 있다");
                Check(wired == commander, $"{invasion.name}이 씬의 EnemyCommanderTemplate을 가리킨다");
            }

            return "PASS: " + passed.Count + " acquisition setup checks (templates, data, enemy commander wiring)";
        }

        private static object GetPrivate(object target, string field)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (info != null) return info.GetValue(target);
            }
            throw new Exception("필드를 찾지 못했다: " + field);
        }
    }
}
