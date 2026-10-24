# IMPL-I1 통합 인계 (2026-10-24, Claude)

Codex 3차(C1~C6)가 사용량 한도로 중단돼 Claude가 이어받았다. C1·C6은 인계 문서 없이 끊겨 diff로 판정했다.

## 통합
- C1~C6 진행분을 통합 worktree `E:/Git/ant-wt/I1`에 합침(충돌 없음, BossRewardTransfer는 C1·C2 동시 수정이었지만 자동 병합).
- 오프라인 컴파일: Unity 6000.5.8f1 Roslyn + `Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp` 소스 목록을 I1 경로로 바꾼 `Temp/off/a.rsp`. 게임 코드 오류 0. 검사 스크립트(`Temp/off/c.rsp`, A.dll 참조)도 이번 검사 파일 오류 0(옛 MultiStopRoute 등 무관한 낡은 검사 오류만 남음).

## C1(인계 없음) 판정 — 코드 들어감, 실행 미검증
- G48 자동 귀환 HomeDefenseReturn.Allows + holdDeployment + 보스 화물 소지 시 억제(CommanderDuty.TickDuty).
- 수동 우물 작업(CommanderSupplyWork, StartService Hauling 허용, AutoCandidates).
- G19 보스 보상 직접 운반(CommanderBossWork, UnitSelectionController 우클릭 BossStronghold).
- G22 고유 장비(CommanderBossEquipment: 잠정 내구 200 / 방열 50% / 범위 3m).
- G06 표본·본격 채굴(ResourceNode.CanMine/CanTakeSample).
- ImplB6Checks NRE 수정, ImplC1Checks 신규.

## C6(인계 없음) 판정
- SaveOptionalStates: JsonUtility 빈 객체 문제를 원본 JSON으로 판정(rocket·farmSeeds·pause·건물 하위 상태).
- SavePreflight.Integration: 농장·공방·가공대 등 통합 검증. BuildingBase Processor 잔해 체력 0, ImplB3Checks 수정(시간 초과 대응), ImplC6Checks 신규.

## Claude가 이어서 구현
- G39: 로켓 가동 중 외교 습격 신규 출발·정복자 계절 판단·독립 원정대 출발 차단, 이동 중 원정대는 TryArriveRaid로 도착(DiplomacyRaids/DiplomacyManager).
- G50: 포획 경로는 정착지 함락 1곳뿐이라 C3 경고로 충분(추가 없음).
- G11: 패배 사유를 HUD 토스트·결과 화면에 표시(구조 불가 문구 확장).
- G42: TransportDto.crewOrders 저장·복원(승무원/주둔 재연결 뒤)·사전 검증, 우클릭 새 명령 시 대기 지시 취소, 월드맵 지시 문구·대기 중 출발 차단.
- G28: FarmSeedSource 고정 키(start:i / terrain:순번:개체), ResourceNodeDto.seedCrop/seedKey 저장·복원·검증.
- G30: 채집 식량 Lot을 원산지별 상온 기간으로 생성(낚시=생선 3일, 균류 밭=버섯 6일, 그 밖=열매 6일).
- G45: 로켓 가동 전 예상 최종 공세 첫 파동 표시(ColonyInvasion.PredictedFirstWave).
- G16: 공공서비스 창에서 시민 지점별 '로켓으로/가까운 주거로' 대피 시작.
- C4 요청 1: 로켓 좌석 저장 검증에 시민 승객 포함.

## 남은 것
- SaveRoundtripChecks 'spike trap restored: WaterSource' — B10 시작 수원·석유 지점 이후 저장 키 순서 어긋남 의심. Unity로 재현 필요.
- 잠정값은 사용자 승인으로 유지(2026-10-24): 최종 공세 수치, 정제유 하루 1, 공공시설 건설비, 보스 장비 효과, G26 묶음 해석, G45 재산 평가.

## 2026-10-24 오후 추가 구현(Claude)
- G06 표본 연구: CampaignResearch.Resources.cs(연구량 300 잠정, 과학 연구와 동시 불가, 저장 검증), 연구 화면 '표본 연구' 항목, 반입 기록은 채집·원정·교역·보상만(ResourceManager.DeliversSample). 검사 AgentScripts/ImplG06Checks.cs(미실행).
- G31 재산 등급 자동 판정: GameBalanceNeeds.ExpectationTier(3000/15000 잠정), ColonyPopulation.Rocket 1초 관측(로켓 경제 관측도 1초로 제한).
- G42: 직접 지시 대기 중 자율 작업 안 잡음(CommanderDuty, ExpeditionTransport.HasPendingCrewOrder).
- G11: 패배 사유 저장·복원(SaveFileV1.defeatReason).
- G43: 명부에서 지워지는 이탈자(포획된 이탈자·반란 합류)를 CampaignResearch.State.leftCommanders에 남겨 엔딩 '이탈'로 분류.
- G69: 하역 보류 판정을 습격대·적 장수(맵 어디든) + 야생 동물(귀환 지점 25m 잠정)로 좁힘 — 떠도는 야생 동물 때문에 영영 하역 못 하던 막힘 방지.
- G04: 병역 나이 확대 시 노년도 동원 상한 인구에 포함.
- 버그: 최종 공세 SpawnCommander colony null, 새 건물 저장 키(objectName 보존), PowerGrid 씬 종료 중 재생성.
- 검사 함정: _TeamImport 전역 동명 타입 → 검사에서 AntColony.Boss.* 전체 이름 사용.
- 미구현 판단: 독립 의자(개념 없음, 식탁이 무연구로 충족), G43 편입 지역 시민(지역 인구 없음), G51 소비 보호(핵심물·종자가 일반 자원 풀 밖이라 식사·제조로 소비되지 않음).
- 검사: 메모리 부족으로 마지막 배치 중단 — C2·C3·C5·C6·Phase4·Fishing·Stage1·SettlementDefense·TransportRoute·B3·G06·G26 재실행 필요.
