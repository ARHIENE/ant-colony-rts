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
- 전투 역할: Melee/Ranged/Defense/Flying/Support 유닛·병영·연구소 프로토타입 구성. Support 생산과 연구 적용까지 검증 완료.
- 역할 강화: 역할별 연구소의 공격력·방어력 3레벨 연구 기반 구현. 연구 뒤 새 유닛에만 적용.
- 건물 전투: 활성 플레이어 건물 기본 체력 300, 피해·파괴, 전체 건물 파괴 시 패배 이벤트와 HUD 메시지 구현.
- 전투/보스: Soldier Ant 전투, WildMonster 추적·공격·재탐색, MiniBird 텔레그래프 패턴 구현.
- 맵/카메라: 플레이 클러스터와 NavMesh를 지형 중앙에 배치. 카메라 초점은 지형 0~400 범위로 제한.

## 이번 SAVE 작업과 검증
- `SupportAntData`와 씬 내부 `SupportAnt`, `SupportBarracksTemplate`, `SupportResearchLabTemplate`을 추가했다.
- Support 임시 수치는 식량 25, 생산 8초, 체력 30, 이동속도 3.2, 방어력 0, 공격력 2, 사거리 2.5, 공격 간격 1.4초, 유지비 1이다.
- Unity Play에서 Support 병영 생산, 체력 30·공격력 2·방어력 0 적용을 확인했다.
- Support 공격 연구 1레벨 뒤 새로 생산한 유닛의 공격력이 4로 적용되는 것을 확인했다.
- 비행 규칙을 고정 고도 직선 이동, 지형·장애물 무시로 확정했다. Ranged/Flying만 공중 대상을 공격하고, Flying은 지상·공중을 모두 공격하며 지상 범위 공격은 공중 유닛에 적용하지 않는다.
- `CombatTargeting`, `IAirborne`, `FlyingAnt`와 `SoldierAnt` 이동 확장 지점을 추가했으며 컴파일 오류는 0건이다.
- Flying 구현은 중간 상태다. 씬 Flying 원본 연결, 선택·어택무브 필터, 몬스터 대공 제한, 지상 범위 공격 제외, Play 검증이 남았다.
- SAVE 캡처: `.unity/capture/image_game_2026-09-08_22-39-12.png`.
- Unity AI Toolkit 계정 API 연결 오류 1건이 있었으나 프로젝트 코드 컴파일과 무관하다.

## 다음 작업 우선순위
1. `CombatRolePrototypeBootstrapper`가 씬 Flying 원본에 `FlyingAnt`를 연결하도록 마무리하고 씬을 저장한다.
2. `UnitSelectionController`와 `AttackMoveController`에 공중 공격 가능 역할 필터를 적용한다.
3. `WildMonster`와 보스의 지상 범위 공격이 공중 유닛을 공격하지 않도록 제한한다.
4. 장애물 횡단·고정 고도 이동, 지상/공중 상호 공격, 지상 범위 공격 면역을 Unity Play에서 검증한다.
5. 임시 역할의 실제 개미 종·외형·공격 연출을 확정한다.
6. 농사·낚시·특수자원, 적 소굴 약탈과 주기적 침공 방어전을 구현한다.

## 구현 시 주의
- `.prefab`/`.prefab.meta`와 `Assets/_TeamImport/`는 Git 커밋 금지.
- 실제 개미 종, 역할별 건물 명칭, Support 액티브 스킬은 기획상 미정이다. 확정 전에는 중립적인 임시 이름과 최소 프로토타입만 사용한다.
- `FlyingAnt.ToFlightPoint`는 공중 대상을 추적할 때 대상 Y에 고도를 다시 더해 상승할 수 있으므로 타겟 추적 고도 계산을 보정해야 한다.
- 기획 변경은 Notion 해당 하위 페이지와 관련 참조 페이지를 함께 갱신한다. 하위 페이지가 있는 기획 부모에는 `replace_content`를 사용하지 않는다.
- 건설 배치 미리보기는 현재 템플릿 크기의 큐브이며 역할별 건물 외형이 추가되면 교체가 필요하다.
- `SnapToTerrainMenu` Raycast는 지형 Collider만 대상으로 제한되지 않아 추후 보정 필요.
- 씬 전환을 추가할 때 `GameManager.UnregisterBuilding`이 전환 중 일시적인 0개 상태를 패배로 오인하지 않는지 재검증한다.
- unity-cli는 포트 16401 사용. 스크립트 재컴파일 뒤 브리지가 끊기면 Unity를 정상 종료 후 재실행하고 창을 활성화한다.
- Unity는 현재 Play 모드가 종료된 상태다.

## 이번 SAVE 개발 일지
- https://app.notion.com/p/3d5c4a0ecd318194b661cca4aa90c2cd
