# 프로젝트 로그

## 개요
- 프로젝트: 개미 소굴 RTS(가제), Unity 6000.5.8f1 URP
- 루트: `E:\Git\ant`
- 저장소: `github.com/ARHIENE/ant-colony-rts`, 작업 브랜치 `master`
- 목표: Notion `게임 개발 노션 정리 > 기획(스펙 문서)` 전체 구현
- 상세 이력: `changelog.md`

## 현재 구현 상태 (2026-09-05 SAVE)
- 자원: Food/Soil 보유량·저장 한도, 수동 채집 지시, 반납, 유지비와 식량 부족 시 아사/반란 구현.
- 유닛 조작: 클릭/드래그/Shift 선택, 우클릭 이동·채집·타겟 공격, A키 어택무브, 이동 확인 마커 구현.
- 선택 정보 UI: 단일 선택 이름·체력·체력바, 다중 선택 수·합산 체력 표시. 사망·비활성화 선택 대상 자동 정리.
- 생산: 여왕방 일개미 생산, Melee 병영 Soldier Ant 생산. 역할군 Worker/Melee/Ranged/Defense/Flying/Support 데이터 기반 마련.
- 병영 티어: 역할군별 병영이 독립적인 1~3티어를 가지며 비용 증가, 유닛 요구 티어 검사, 생산/업그레이드 동시 실행 차단 구현.
- 전투: Soldier Ant 직접 공격/어택무브, WildMonster 탐지·NavMesh 추적·주기 공격·대상 재탐색 구현.
- 반란: 개미가 WildMonster로 바뀐 뒤 이동 에이전트를 복구하고 플레이어 개미를 공격.
- 보스: MiniBird 체력과 원형/부채꼴/직선 텔레그래프 공격 순환 구현.
- 맵/카메라: 지형 중앙에 플레이 클러스터와 NavMesh 배치. 카메라 초점은 지형 0~400 범위로 제한.

## 이번 SAVE 검증
- 선택 유닛 UI Play 검증 10개 항목 통과.
- 실제 UI 클릭으로 Soldier Ant 생산, 병영 1→2티어 업그레이드, 비용 증가와 자원 부족 차단 확인.
- WildMonster가 WorkerAnt를 탐지해 추적·처치하고 대상 제거 후 탐색 상태로 복귀하는 것을 확인.
- 최종 Unity Play 실행의 콘솔 오류/예외 0건.
- 캡처: `.unity/capture/image_game_2026-09-05_19-30-32.png`(Git 제외, Notion 개발 일지 첨부용).

## 다음 작업 우선순위
1. 연구소와 역할/카테고리별 공격력·방어력 강화 시스템.
2. 일개미 건물 배치·건설 과정과 역할군별 병영 실제 건물 추가.
3. 농사·낚시·특수자원.
4. 적 소굴 약탈과 주기적 침공 방어전.
5. 대형 개미·특수 배양소와 추가 보스/레이드 맵.

## 구현 시 주의
- `.prefab`/`.prefab.meta`와 `Assets/_TeamImport/`는 Git 커밋 금지.
- 기획 변경은 Notion 해당 하위 페이지와 관련 참조 페이지를 함께 갱신한다. 하위 페이지가 있는 기획 부모에는 `replace_content`를 사용하지 않는다.
- `SnapToTerrainMenu` Raycast는 지형 Collider만 대상으로 제한되지 않아 추후 보정 필요.
- unity-cli는 포트 16401 사용. 스크립트 재컴파일 뒤 브리지가 끊기면 Unity를 정상 종료 후 재실행한다.
- Unity는 현재 Play 모드가 종료된 상태다.

## 이번 SAVE 개발 일지
- https://app.notion.com/p/3d2c4a0ecd318170aa12e388e5f9bd51
