using System;
using System.Linq;
using System.Threading.Tasks;

public static class Review1101Q1Checks
{
    private static int count;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("Q1 실패: " + name); count++; }

    public static async Task<string> Main()
    {
        count = 0;
        Check(AntColony.UI.PersonalDisplayNames.Mood("Hunger") == "허기", "허기 표시");
        Check(AntColony.UI.PersonalDisplayNames.Mood("Rest") == "휴식", "휴식 표시");
        Check(AntColony.UI.PersonalDisplayNames.Mood("Catharsis") == "감정 해소", "감정 해소 표시");
        Check(AntColony.UI.PersonalDisplayNames.Mood("Death: Rest") == "사망: Rest", "장수 이름 원문 보존");
        Check(AntColony.UI.PersonalDisplayNames.Mood("Downed: 민수") == "쓰러짐: 민수", "쓰러짐 표시");
        Check(AntColony.UI.PersonalDisplayNames.Mood(null) == "", "빈 구저장 표시");
        Check(AntColony.UI.PersonalDisplayNames.Mood("사용자 원문") == "사용자 원문", "기존 원문 보존");
        foreach (AntColony.Units.EquipmentSlot slot in Enum.GetValues(typeof(AntColony.Units.EquipmentSlot)))
            Check(AntColony.UI.PersonalDisplayNames.Slot(slot) != slot.ToString(), "장비 부위 " + slot);
        foreach (var category in new[] { "Ants", "World", "Bosses" })
            Check(AntColony.UI.PersonalDisplayNames.Category(category) != category, "도감 범주 " + category);
        Check(AntColony.UI.PersonalDisplayNames.Category("기존 분류") == "기존 분류", "도감 키 변환 없음");
        Check(AntColony.UI.SiteNames.Korean("Independent 3 Settlement") == "독립 정착지", "거점 표시·내부 번호 숨김");
        Check(AntColony.UI.SiteNames.Korean("Independent") == "독립", "독립 세력 단독 표시");
        Check(AntColony.UI.SiteNames.Korean("Neutral ResourceSite 1") == "중립 자원지 1", "자원지 구별 번호 보존");
        Check(AntColony.UI.SiteNames.Korean("사용자 거점") == "사용자 거점", "한국어 거점 보존");
        var legacy = new AntColony.Save.DiscoveryDto { category = "World", key = "site:Amber Colony", title = "Amber Colony", body = "Amber / Settlement. Fixed difficulty 2." };
        Check(AntColony.UI.PersonalDisplayNames.DiscoveryTitle(legacy) == "호박 소굴", "구도감 거점 제목");
        Check(AntColony.UI.PersonalDisplayNames.DiscoveryBody(legacy) == "호박 · 정착지 · 고정 위험도 D2", "구도감 본문");
        Check(legacy.key == "site:Amber Colony" && legacy.title == "Amber Colony" && legacy.body.StartsWith("Amber /"), "구도감 저장 원문 불변");
        legacy.category = "Ants"; legacy.key = "weapon:ShortBow";
        Check(AntColony.UI.PersonalDisplayNames.DiscoveryTitle(legacy) == "단궁", "기존 무기 함수 재사용");
        legacy.body = "사용자가 보존한 원문";
        Check(AntColony.UI.PersonalDisplayNames.DiscoveryBody(legacy) == legacy.body, "알려진 옛 문장만 표시 변환");
        Check(AntColony.UI.ExpeditionPlanning.LoadAmount(100, 20, 90, 30) == 30, "재고만큼 부분 보급");
        Check(AntColony.UI.ExpeditionPlanning.LoadAmount(100, 20, 5, 90) == 5, "예약을 뺀 적재 여유");
        Check(AntColony.UI.ExpeditionPlanning.LoadAmount(10, 20, 90, 90) == 0, "충분한 보급 추가 차감 없음");
        Check(AntColony.UI.ExpeditionPlanning.LoadAmount(100, 20, 90, 0) == 0, "재고 없음");
        // 엔진 객체를 생성하지 않고 순수 필드 기반 속도/예산 속성만 검사한다.
        var transport = (AntColony.World.ExpeditionTransport)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AntColony.World.ExpeditionTransport));
        var health = typeof(AntColony.Buildings.BuildingBase).GetField("currentHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        health.SetValue(transport, 400f);
        Check(transport.PlannedRoundTripSeconds(null) == 41, "정상 차량 왕복40+접안1초");
        health.SetValue(transport, 150f);
        Check(Math.Abs(transport.PlannedRoundTripSeconds(null) - (40 / .75f + 1)) < .001f, "손상 차량 실속도 왕복");
        typeof(AntColony.World.ExpeditionTransport).GetField("<Remaining>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(transport, 30f);
        Check(transport.TravelRemainingSeconds == 40, "감속 후 실제 도착 남은초");
        health.SetValue(transport, 50f);
        Check(float.IsPositiveInfinity(transport.PlannedRoundTripSeconds(null)) && transport.RecommendedFood(null) == 0, "이동 불가 예산 미정·무한대 변환 차단");
        var civ = new AntColony.World.Civilization { war = true, warStarted = 100, enemyScore = 10, playerScore = 3 };
        Check(AntColony.UI.DiplomacyPlanning.PeaceLabel(civ, 699).Contains("1초"), "평화 관문 직전");
        Check(AntColony.UI.DiplomacyPlanning.PeaceLabel(civ, 700).Contains("7 지불"), "평화 관문 경계·지불 방향");
        civ.playerScore = 20;
        Check(AntColony.UI.DiplomacyPlanning.PeaceLabel(civ, 700).Contains("10 수령"), "배상 수령 방향");
        civ.playerScore = 10;
        Check(AntColony.UI.DiplomacyPlanning.PeaceLabel(civ, 700).Contains("배상 없음"), "무배상");
        civ.war = false;
        Check(AntColony.UI.DiplomacyPlanning.PeaceLabel(civ, 0).Contains("현재 평화"), "평화 중 협상 상태");

        foreach (AntColony.Boss.BossId id in Enum.GetValues(typeof(AntColony.Boss.BossId)))
        {
            var state = new AntColony.Boss.BossProgress.State { revealed = new[] { true, true, true, true } };
            var progress = new AntColony.Boss.BossProgress(state);
            var reward = id == AntColony.Boss.BossId.A ? 2 : 1;
            Check(progress.RespawnRemaining(id, 0) == 0, "생존 보스 재등장 없음 " + id);
            Check(progress.RecordKill(id, 100), "처치 기록 " + id);
            Check(progress.UnclaimedCount(id) == reward, "보상 꾸러미와 개수 구분 " + id);
            Check(progress.RespawnRemaining(id, 100) == AntColony.Boss.BossProgress.RespawnSeconds, "재등장 시계 시작 " + id);
            Check(progress.RespawnRemaining(id, 100 + AntColony.Boss.BossProgress.RespawnSeconds - 1) == 1, "재등장 직전 " + id);
            Check(!progress.TryRespawn(id, 1900, true, false, false), "아군 잔류 재등장 보류 " + id);
            Check(progress.RespawnRemaining(id, 1901) == 0, "기한 후 음수 없음 " + id);
            Check(progress.TryAcquire(id, "test", out var parcel), "실물 회수 " + id);
            Check(progress.UnclaimedCount(id) == 0 && progress.Cargo.Sum(p => p.count) == reward, "회수 후 현지/운송 구분 " + id);
            Check(progress.TryDeliver(parcel.id) && progress.Home(id) == reward, "반입 후 본거지 수량 " + id);
            Check(progress.TryRespawn(id, 1900, false, false, false), "빈 전장 재등장 " + id);
            Check(progress.RespawnRemaining(id, 1901) == 0, "새 회차 대기 표시 없음 " + id);
        }
        Check(AntColony.Boss.BossProgress.Hazard(AntColony.Boss.BossId.B).Contains("호위 최대2"), "B 실제 호위 위험");
        Check(AntColony.Boss.BossProgress.Hazard(AntColony.Boss.BossId.C).Contains("열 피해6"), "C 실제 열 위험");
        await Task.CompletedTask;
        return "Q1 " + count + "검사 통과 (순수 규칙·표시; 실제 전투/출정은 별도)";
    }

    // Claude: 시작 완료 격리 Play에서 출정 준비/이동/귀환 대기/보스 전장 각각 열고 재실행.
    // 현재 게임을 바꾸지 않고 실제 컴포넌트의 안내와 판정이 일치하는지 확인한다.
    public static async Task<string> RunQ1Scene()
    {
        var result = await Main();
        var world = AntColony.World.WorldMapManager.Instance;
        Check(world != null, "시작 완료 월드");
        foreach (var ship in world.Transports.Where(s => s != null && s.isActiveAndEnabled))
        {
            Check(ship.ReturnCargoSpace >= 0, "실제 화물 여유");
            foreach (var site in world.Sites.Where(AntColony.World.WorldMapManager.SiteRevealed))
            {
                var block = ship.DepartureBlock(site);
                if (block != "") Check(!ship.TryDepart(site), "불가 출정은 상태를 바꾸지 않음: " + block);
                if (!ship.CanOperate) Check(ship.RecommendedFood(site) == 0, "이동 불가 예산 미정");
                else Check(ship.RecommendedFood(site) == AntColony.World.ExpeditionTransport.ExpectedFood(
                    ship.PlannedRoundTripSeconds(site) + 2 * AntColony.Core.GameCalendar.SecondsPerDay, ship.DailyFood, 0), "실속도 보급 예산");
            }
            Check(!string.IsNullOrEmpty(ship.CargoSummary), "현재 실물 화물 요약");
        }
        var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsInactive.Exclude);
        foreach (var key in new[] { "Continue Colony", "Load Game", "New Game", "Back" })
            foreach (var text in texts.Where(t => t.transform.parent.name == key)) Check(text.text != key, "버튼 키 보존·한국어 라벨 " + key);
        return result + "\n실장면 검사 " + count + "개. 성공 출정→귀환, 실제 타격/회피, 화면 잘림은 Claude 수동 확인 필요.";
    }
}
