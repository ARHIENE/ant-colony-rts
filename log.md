# 프로젝트 로그

## 개요
- 프로젝트: 개미 소굴 RTS(가제), Unity 6000.5.8f1 URP
- 루트: `E:\Git\ant`
- 저장소: `github.com/ARHIENE/ant-colony-rts`, 작업 브랜치 `develop` (`master`는 안정 버전)
- 목표: Notion `게임 개발 노션 정리 > 기획(스펙 문서)` 전체 구현
- 상세 이력: `changelog.md`

## 현재 구현 상태 (2026-09-08 SAVE)
- 자원: Food/Soil 보유량·저장 한도, 수동 채집·반납, 유지비와 식량 부족 시 아사/반란 구현.
- 유닛 조작: 클릭/드래그/Shift 선택, 우클릭 이동·채집·타겟 공격, A키 어택무브, 이동 확인 마커 구현.
- 생산/건설: 여왕방 일개미 생산, 일개미의 자유 건설 배치·완공, 역할별 병영 독립 1~3티어 구현.
- 전투 역할: Melee Soldier Ant와 임시 Ranged/Defense/Flying Ant 생산 가능. Flying은 아직 지상 NavMesh를 쓰는 임시 프로토타입.
- 역할 강화: Melee/Ranged/Defense/Flying 연구소의 공격력·방어력 3레벨 연구 기반 구현. 연구 뒤 새 유닛에만 적용.
- 건물 전투: 활성 플레이어 건물 기본 체력 300, 피해·파괴, 전체 건물 파괴 시 패배 이벤트와 HUD 메시지 구현.
- 전투/보스: Soldier Ant 전투, WildMonster 추적·공격·재탐색, MiniBird 텔레그래프 패턴 구현.
- 맵/카메라: 플레이 클러스터와 NavMesh를 지형 중앙에 배치. 카메라 초점은 지형 0~400 범위로 제한.

## 이번 SAVE 검증
- DigSite, Storage, QueenChamber를 포함한 기존 건물의 체력이 300으로 초기화되는 것을 확인.
- 등록된 플레이어 건물을 모두 제거했을 때 `GameManager` 패배 상태와 HUD 패배 이벤트가 발생하는 것을 확인.
- Flying 병영에서 유닛 2개를 생산해 `FlyingAntData`, 체력 35, 공격력 4, 방어력 1 적용을 확인.
- Flying 공격 연구 1레벨 뒤 새로 생산된 유닛 공격력이 6으로 적용되고 기존 유닛은 4를 유지하는 것을 확인.
- FlyingAnt·FlyingBarracksTemplate·FlyingResearchLabTemplate 원본은 모두 비활성 상태로 복구하고 씬 저장.
- 최종 컴파일 오류 0건, 콘솔 오류·예외·Assert 0건. unity-cli 연결/해제 경고만 확인.
- 캡처: `.unity/capture/image_game_2026-09-08_01-45-00.png`

## 다음 작업 우선순위
1. Support 역할 유닛·병영·연구소 프로토타입 추가 및 Play 검증.
2. Flying의 실제 비행 이동과 공중/대공 공격 규칙 확정·구현.
3. 임시 Ranged/Defense/Flying Ant의 실제 종·외형·공격 연출 확정.
4. 농사·낚시·특수자원.
5. 적 소굴 약탈과 주기적 침공 방어전.
6. 대형 개미·특수 배양소와 추가 보스/레이드 맵.

## 구현 시 주의
- `.prefab`/`.prefab.meta`와 `Assets/_TeamImport/`는 Git 커밋 금지.
- 실제 개미 종, 역할별 건물 명칭, Support 액티브 스킬은 기획상 미정이다. 확정 전에는 중립적인 임시 이름과 최소 프로토타입만 사용한다.
- Flying은 현재 지상 `SoldierAnt`를 재사용한다. 실제 비행을 추가할 때 이동, 지형 무시, 피격 가능 대상, 대공 규칙을 함께 정의해야 한다.
- 기획 변경은 Notion 해당 하위 페이지와 관련 참조 페이지를 함께 갱신한다. 하위 페이지가 있는 기획 부모에는 `replace_content`를 사용하지 않는다.
- 건설 배치 미리보기는 현재 템플릿 크기의 큐브이며 역할별 건물 외형이 추가되면 교체가 필요하다.
- `SnapToTerrainMenu` Raycast는 지형 Collider만 대상으로 제한되지 않아 추후 보정 필요.
- 씬 전환을 추가할 때 `GameManager.UnregisterBuilding`이 전환 중 일시적인 0개 상태를 패배로 오인하지 않는지 재검증한다.
- unity-cli는 포트 16401 사용. 스크립트 재컴파일 뒤 브리지가 끊기면 Unity를 정상 종료 후 재실행하고 창을 활성화한다.
- Unity는 현재 Play 모드가 종료된 상태다.

## 이번 SAVE 개발 일지
- https://app.notion.com/p/3d5c4a0ecd3181b1aeeecccf4b6e98ae
