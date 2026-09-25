namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Core;
    using AntColony.Data;
    using AntColony.Units;
    using AntColony.World;
    using UnityEngine;
    using UnityEngine.AI;
    using Object = UnityEngine.Object;

    // 장수 획득 경로 3종(번식/영입/포로) 검사.
    public static class CommanderAcquisitionChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");

            var origin = new Vector3(1200f, 0f, 1200f);
            var objects = new List<Object>();
            var nav = default(NavMeshDataInstance);
            var passed = new List<string>();
            var sceneCommanders = new List<CommanderAnt>();

            void Check(bool condition, string label)
            {
                if (!condition) throw new Exception("FAIL: " + label);
                passed.Add(label);
            }

            GameObject New(string name, Vector3 position)
            {
                var go = new GameObject(name);
                go.transform.position = position;
                objects.Add(go);
                return go;
            }

            try
            {
                // 검사용 평지 NavMesh. 장수 생성은 NavMesh 위에서만 성공한다.
                var source = new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(120f, 1f, 120f),
                    transform = Matrix4x4.TRS(origin + Vector3.down * .5f, Quaternion.identity, Vector3.one)
                };
                var settings = NavMesh.GetSettingsByID(0);
                // 소스 transform이 이미 월드 좌표이므로 데이터 원점은 zero여야 한다(origin을 주면 좌표가 두 번 더해진다).
                var data = NavMeshBuilder.BuildNavMeshData(settings,
                    new List<NavMeshBuildSource> { source }, new Bounds(origin, new Vector3(140f, 20f, 140f)),
                    Vector3.zero, Quaternion.identity);
                objects.Add(data);
                nav = NavMesh.AddNavMeshData(data);
                Check(nav.valid, "테스트 NavMesh가 만들어진다");

                // --- 공통 기반 ---
                // 개미 풀·자원·명부는 씬의 싱글턴을 그대로 쓴다(기존 검사와 같은 방식).
                // 새로 만들면 싱글턴 가드에 걸려 곧바로 파괴된다.
                var pool = AntPool.Instance;
                var resources = ResourceManager.Instance;
                var roster = CommanderRoster.Instance;
                Check(pool != null && resources != null && roster != null, "씬의 개미 풀·자원·명부를 찾는다");

                var profile = ScriptableObject.CreateInstance<UnitData>();
                profile.role = UnitRole.Worker;
                profile.maxHealth = 10f;
                profile.moveSpeed = 3.5f;
                profile.attackDamage = 2f;
                profile.attackRange = 2f;
                profile.attackInterval = 1f;
                profile.gatherRate = 1f;
                profile.carryCapacity = 5;
                objects.Add(profile);

                // 씬의 시작 장수들은 서로 가까이 붙어 있어 그대로 두면 같이 번식하며 식량을 소비한다.
                // 검사 동안만 비활성화해 격리한다(번식은 isActiveAndEnabled인 장수만 대상으로 한다).
                foreach (var existing in roster.Commanders)
                    if (existing != null && existing.gameObject.activeSelf)
                    {
                        sceneCommanders.Add(existing);
                        existing.gameObject.SetActive(false);
                    }
                await Task.Yield();
                Check(sceneCommanders.Count > 0, "씬의 기존 장수를 검사 동안 비활성화한다");

                // ================= 1. 번식 =================
                var nurseryGo = New("Check Nursery", origin);
                var nursery = nurseryGo.AddComponent<NurseryChamber>();
                SetPrivate(nursery, "affinityPerSecond", 10f);
                SetPrivate(nursery, "birthAffinity", 100f);
                SetPrivate(nursery, "birthFoodCost", 30);
                // 씬에 이미 장수가 있으므로 정원은 절대값이 아니라 현재 인원 기준으로 잡는다.
                SetPrivate(nursery, "maxCommanders", CommanderRoster.Instance.Count + 5);

                var parentA = roster.Create("Parent A", CommanderRank.Sergeant,
                    new[] { UnitRole.Worker, UnitRole.Melee }, UnitRole.Worker,
                    new CommanderTraits(CommanderPersonality.Brave, 60), origin);
                var parentB = roster.Create("Parent B", CommanderRank.Sergeant,
                    new[] { UnitRole.Worker, UnitRole.Ranged }, UnitRole.Worker,
                    new CommanderTraits(CommanderPersonality.Cautious, 40), origin + new Vector3(2f, 0f, 0f));
                Check(parentA != null && parentB != null, "부모 장수 2명이 만들어진다");
                // 번식은 연인(서로 관계 70 이상) 쌍만 한다.
                parentA.PersonalState.Relation(parentB.PersonalState.id).value = 70;
                parentB.PersonalState.Relation(parentA.PersonalState.id).value = 70;

                var baseCount = roster.Count;

                // 씬에 이미 쌓인 식량을 비워 "비용을 못 내는 상황"을 만든다.
                resources.TrySpend(resources.GetAmount(ResourceType.Food), 0);
                Check(resources.GetAmount(ResourceType.Food) == 0, "검사 시작 시 식량을 0으로 비운다");

                // 식량이 없으면 출산하지 않는다.
                nursery.Tick(11f);
                Check(nursery.BirthCount == 0 && roster.Count == baseCount, "식량이 없으면 출산하지 않는다");

                // 호감도는 유지되므로 식량이 생기면 바로 출산한다.
                resources.Add(ResourceType.Food, 100);
                var foodBefore = resources.GetAmount(ResourceType.Food);
                Check(foodBefore >= 30, "출산 비용을 낼 식량이 확보된다");
                nursery.Tick(1f);
                Check(nursery.BirthCount == 1, "호감도가 차고 식량이 있으면 출산한다");
                Check(roster.Count == baseCount + 1, "태어난 장수가 명부에 합류한다");
                Check(resources.GetAmount(ResourceType.Food) == foodBefore - 30, "출산에 식량을 정확히 한 번 지불한다");

                var child = roster.Commanders[roster.Count - 1];
                var inheritedPersonality = child.Traits.Personality == parentA.Traits.Personality
                    || child.Traits.Personality == parentB.Traits.Personality;
                Check(inheritedPersonality, "태어난 장수가 부모 중 한쪽의 성격을 물려받는다");

                var inheritedRole = child.CanTakeRole(UnitRole.Melee) || child.CanTakeRole(UnitRole.Ranged);
                Check(inheritedRole, "태어난 장수가 부모의 전투 보직을 물려받는다");

                // 출산 직후 호감도가 리셋돼 매 틱 연속 출산하지 않는다.
                Check(nursery.GetAffinity(parentA, parentB) < 100f, "출산 후 호감도가 초기화된다");

                // 태어난 자식도 번식 대상이라 그대로 두면 부모와 새 쌍을 이룬다. 이 구간에서는 격리한다.
                child.gameObject.SetActive(false);

                // 멀리 떨어진 쌍은 호감도가 쌓이지 않는다.
                parentB.transform.position = origin + new Vector3(40f, 0f, 0f);
                nursery.Tick(11f);
                Check(nursery.GetAffinity(parentA, parentB) == 0f, "떨어져 있으면 호감도가 쌓이지 않는다");

                // 정원이 차면 출산하지 않는다.
                parentB.transform.position = origin + new Vector3(2f, 0f, 0f);
                var birthsBeforeCap = nursery.BirthCount;
                SetPrivate(nursery, "maxCommanders", roster.Count);
                resources.Add(ResourceType.Food, 200);
                var cappedFood = resources.GetAmount(ResourceType.Food);
                nursery.Tick(11f);
                Check(nursery.BirthCount == birthsBeforeCap, "정원이 차면 출산하지 않는다");
                Check(resources.GetAmount(ResourceType.Food) == cappedFood, "출산이 막히면 식량도 소비하지 않는다");
                SetPrivate(nursery, "maxCommanders", roster.Count + 10);

                // 번식 구간 종료. 양육실 Update가 이후 구간의 인원/식량을 건드리지 않게 끈다.
                nurseryGo.SetActive(false);

                // ================= 2. 스카우트 영입 =================
                var scoutGo = New("Check Scout", origin);
                var scout = scoutGo.AddComponent<ScoutPost>();
                SetPrivate(scout, "dispatchFoodCost", 20);
                SetPrivate(scout, "dispatchAnts", 1);
                SetPrivate(scout, "travelSeconds", 10f);
                SetPrivate(scout, "baseChance", 1f);
                SetPrivate(scout, "chancePerCommander", 0f);
                SetPrivate(scout, "maxChance", 1f);
                SetPrivate(scout, "maxCommanders", 30);

                var freeBefore = pool.Free;
                var scoutFoodBefore = resources.GetAmount(ResourceType.Food);
                Check(scout.TryDispatch(), "스카우트를 파견한다");
                Check(scout.IsDispatched, "파견 중 상태가 된다");
                Check(pool.Free == freeBefore - 1, "파견 개미가 대기 풀에서 빠진다");
                Check(resources.GetAmount(ResourceType.Food) == scoutFoodBefore - 20, "파견 비용을 지불한다");
                Check(!scout.TryDispatch(), "이미 파견 중이면 중복 파견하지 않는다");

                var beforeRecruit = roster.Count;
                scout.Tick(5f);
                Check(scout.IsDispatched && roster.Count == beforeRecruit, "도착 전에는 결과가 나오지 않는다");

                scout.Tick(6f);
                Check(!scout.IsDispatched, "귀환하면 파견 상태가 풀린다");
                Check(pool.Free == freeBefore, "파견 개미가 대기 풀로 돌아온다");
                Check(scout.SuccessCount == 1 && roster.Count == beforeRecruit + 1, "확률 100%면 장수가 합류한다");

                // 실패해도 개미는 돌아오고 장수만 늘지 않는다.
                SetPrivate(scout, "baseChance", 0f);
                SetPrivate(scout, "maxChance", 0f);
                var beforeFail = roster.Count;
                Check(scout.TryDispatch(), "재파견할 수 있다");
                scout.Tick(11f);
                Check(scout.FailureCount == 1 && roster.Count == beforeFail, "확률 0%면 장수가 합류하지 않는다");
                Check(pool.Free == freeBefore, "영입 실패에도 파견 개미는 돌아온다");

                // 세력이 클수록 성공률이 오른다.
                SetPrivate(scout, "baseChance", .1f);
                SetPrivate(scout, "chancePerCommander", .02f);
                SetPrivate(scout, "maxChance", .8f);
                var smallFactionChance = scout.CurrentChance;
                roster.Create("Extra", CommanderRank.Corporal, new[] { UnitRole.Worker }, UnitRole.Worker, null, origin);
                Check(scout.CurrentChance > smallFactionChance, "세력이 커지면 영입 성공률이 오른다");

                // 식량이 모자라면 파견 자체가 거부되고 개미도 빠지지 않는다.
                SetPrivate(scout, "dispatchFoodCost", 100000);
                var poorFree = pool.Free;
                Check(!scout.TryDispatch(), "식량이 모자라면 파견하지 않는다");
                Check(pool.Free == poorFree, "파견이 거부되면 개미도 차출하지 않는다");
                SetPrivate(scout, "dispatchFoodCost", 0);

                // ================= 3. 포로 =================
                var campGo = New("Check Prison", origin);
                var camp = campGo.AddComponent<PrisonerCamp>();
                SetPrivate(camp, "capacity", 2);
                SetPrivate(camp, "basePersuadeChance", 1f);
                SetPrivate(camp, "loyaltyPenalty", 0f);
                SetPrivate(camp, "escapeChance", 0f);
                SetPrivate(camp, "persuadeFoodCost", 0);
                await Task.Yield();
                Check(PrisonerCamp.Instance == camp, "수용소가 싱글턴으로 등록된다");

                var loyalTraits = new CommanderTraits(CommanderPersonality.Devoted, 100);
                var weakTraits = new CommanderTraits(CommanderPersonality.Brave, 0);

                Check(camp.TryCapture("POW 1", CommanderRank.Sergeant, new[] { UnitRole.Worker, UnitRole.Melee }, weakTraits),
                    "적 장수를 포로로 받는다");
                Check(camp.Count == 1, "포로가 수용소에 쌓인다");

                // 회유 확률은 충성심과 성격을 반영한다.
                SetPrivate(camp, "loyaltyPenalty", .5f);
                SetPrivate(camp, "basePersuadeChance", .6f);
                camp.TryCapture("POW 2", CommanderRank.Sergeant, new[] { UnitRole.Worker }, loyalTraits);
                Check(camp.PersuadeChance(camp.Prisoners[0]) > camp.PersuadeChance(camp.Prisoners[1]),
                    "충성심이 낮은 포로가 회유하기 쉽다");

                Check(!camp.TryCapture("POW 3", CommanderRank.Sergeant, new[] { UnitRole.Worker }, weakTraits),
                    "정원이 차면 더 받지 않는다");

                // 회유 실패는 포로를 잃지 않고 반복 시도를 허용한다.
                SetPrivate(camp, "basePersuadeChance", 0f);
                SetPrivate(camp, "attemptBonus", 0f);
                var beforePersuade = roster.Count;
                Check(!camp.TryPersuade(1), "확률 0%면 회유에 실패한다");
                Check(camp.Count == 2 && roster.Count == beforePersuade, "회유에 실패해도 포로는 남는다");
                Check(camp.Prisoners[1].PersuadeAttempts == 1, "회유 시도 횟수가 쌓인다");

                // 회유 성공 시 포로가 내 장수로 합류한다.
                SetPrivate(camp, "basePersuadeChance", 1f);
                SetPrivate(camp, "loyaltyPenalty", 0f);
                var beforeRecruited = roster.Count;
                Check(camp.TryPersuade(0), "확률 100%면 회유에 성공한다");
                Check(camp.Count == 1, "회유된 포로는 수용소에서 빠진다");
                Check(roster.Count == beforeRecruited + 1, "회유된 장수가 명부에 합류한다");
                Check(camp.RecruitedCount == 1, "회유 성공 횟수가 기록된다");

                var recruited = roster.Commanders[roster.Count - 1];
                Check(recruited.CommanderName == "POW 1", "회유된 장수가 포로의 이름을 유지한다");
                Check(recruited.CanTakeRole(UnitRole.Melee), "회유된 장수가 포로의 보직을 유지한다");

                // 처형은 언제든 가능하다.
                Check(camp.Execute(0), "포로를 처형할 수 있다");
                Check(camp.Count == 0 && camp.ExecutedCount == 1, "처형한 포로가 사라진다");
                Check(!camp.Execute(0), "없는 포로는 처형되지 않는다");

                // 탈출 판정.
                camp.TryCapture("POW 4", CommanderRank.Sergeant, new[] { UnitRole.Worker }, weakTraits);
                SetPrivate(camp, "escapeChance", 0f);
                SetPrivate(camp, "escapeCheckSeconds", 10f);
                // 탈출 타이머는 Awake 시점 값에서 흘러가고 있으므로, 판정 시점을 검사에서 직접 맞춘다.
                SetPrivate(camp, "escapeTimer", .1f);
                camp.Tick(1f);
                Check(camp.Count == 1 && camp.EscapedCount == 0, "탈출 확률 0%면 포로가 남는다");

                SetPrivate(camp, "escapeChance", 1f);
                SetPrivate(camp, "escapeTimer", .1f);
                camp.Tick(1f);
                Check(camp.Count == 0 && camp.EscapedCount == 1, "탈출 확률 100%면 포로를 잃는다");

                // ================= 4. 적 장수 → 포로 연결 =================
                var enemyGo = New("Check Enemy Commander", origin + new Vector3(10f, 0f, 0f));
                enemyGo.SetActive(false);
                var enemy = enemyGo.AddComponent<EnemyCommander>();
                enemy.ConfigureCommander("Raid Leader", CommanderRank.Lieutenant,
                    new[] { UnitRole.Worker, UnitRole.Ranged }, new CommanderTraits(CommanderPersonality.Brave, 20));
                SetPrivate(enemy, "captureChance", 1f);
                SetPrivate(enemy, "maxHealth", 5f);
                enemyGo.SetActive(true);
                await Task.Yield();

                enemy.MakeRaider();
                enemy.TakeDamage(999f);
                Check(enemy.WasCaptured, "쓰러진 적 장수가 포로가 된다");
                Check(camp.Count == 1, "포로가 수용소에 들어간다");
                Check(camp.Prisoners[0].Name == "Raid Leader", "포로가 적 장수의 이름을 가진다");
                Check(camp.Prisoners[0].Rank == CommanderRank.Lieutenant, "포로가 적 장수의 관직을 가진다");

                // 수용소가 없거나 꽉 차면 포로가 되지 않고 평소대로 죽는다.
                SetPrivate(camp, "capacity", 1);
                var enemy2Go = New("Check Enemy Commander 2", origin + new Vector3(12f, 0f, 0f));
                enemy2Go.SetActive(false);
                var enemy2 = enemy2Go.AddComponent<EnemyCommander>();
                SetPrivate(enemy2, "maxHealth", 5f);
                SetPrivate(enemy2, "captureChance", 1f);
                enemy2Go.SetActive(true);
                await Task.Yield();
                enemy2.MakeRaider();
                enemy2.TakeDamage(999f);
                Check(!enemy2.WasCaptured, "정원이 차면 적 장수는 포로가 되지 않는다");
                Check(camp.Count == 1, "정원을 넘겨 포로가 늘지 않는다");

                // ================= 5. 성격이 전투 수치에 반영되는가 =================
                var brave = roster.Create("Brave", CommanderRank.Sergeant, new[] { UnitRole.Worker }, UnitRole.Worker,
                    new CommanderTraits(CommanderPersonality.Brave, 50), origin);
                var cautious = roster.Create("Cautious", CommanderRank.Sergeant, new[] { UnitRole.Worker }, UnitRole.Worker,
                    new CommanderTraits(CommanderPersonality.Cautious, 50), origin + new Vector3(1f, 0f, 0f));
                brave.TryAssign(1);
                cautious.TryAssign(1);
                Check(brave.AttackDamage > cautious.AttackDamage, "용감형이 신중형보다 공격력이 높다");
                Check(cautious.Armor > brave.Armor, "신중형이 용감형보다 방어력이 높다");

                // 성격 페널티가 공격력을 음수로 만들지 않는다.
                var weak = roster.Create("Weak", CommanderRank.Sergeant, new[] { UnitRole.Worker }, UnitRole.Worker,
                    new CommanderTraits(CommanderPersonality.Cautious, 50), origin + new Vector3(3f, 0f, 0f));
                profile.attackDamage = 0f;
                weak.SetRoleProfiles(new[] { profile });
                weak.TryAssign(3);
                Check(weak.AttackDamage >= 0f, "성격 페널티로 공격력이 음수가 되지 않는다");

                return "PASS: " + passed.Count + " commander acquisition checks (breeding, scouting, prisoners)";
            }
            finally
            {
                // 검사가 명부에 밀어 넣은 장수를 전부 걷어낸다(씬에 원래 있던 장수는 남긴다).
                var roster = CommanderRoster.Instance;
                if (roster != null)
                {
                    var leftovers = new List<CommanderAnt>(roster.Commanders);
                    foreach (var commander in leftovers)
                        if (commander != null && !sceneCommanders.Contains(commander))
                            Object.DestroyImmediate(commander.gameObject);
                }

                // 격리를 위해 껐던 씬 장수를 되돌린다.
                foreach (var existing in sceneCommanders)
                    if (existing != null) existing.gameObject.SetActive(true);

                if (nav.valid) NavMesh.RemoveNavMeshData(nav);
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
