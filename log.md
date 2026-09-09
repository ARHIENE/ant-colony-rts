# 프로젝트 로그

## 개요
- 프로젝트: 개미 소굴 RTS(가제), Unity 6000.5.8f1 URP
- 루트: `E:\Git\ant`
- 저장소: `github.com/ARHIENE/ant-colony-rts`, 작업 브랜치 `develop` (`master`는 안정 버전)
- 목표: Notion `게임 개발 노션 정리 > 기획(스펙 문서)` 전체 구현
- 상세 이력: `changelog.md`

## 현재 구현 상태 (2026-09-09 SAVE)
- 자원: Food/Soil 채집·반납, 저장 한도, 유지비, 식량 부족 시 아사·반란 구현.
- 농사: 밭 자유 건설, 성장 후 Food 수확, 소진 후 반복 재성장 프로토타입 구현.
- 유닛 조작: 클릭/드래그/Shift 선택, 이동·채집·직접 공격, A키 어택무브 구현.
- 생산/건설: 여왕방 일개미 생산, 자유 건설, 역할별 병영 1~3티어와 연구소 구현.
- 전투 역할: Melee/Ranged/Defense/Flying/Support 유닛·병영·연구소 프로토타입 구현.
- 비행: 고정 고도 직선 이동과 장애물 무시, 역할별 대공 제한, 지상 범위 공격 면역 구현.
- 전투/보스: Soldier Ant 전투, WildMonster 추적·공격, MiniBird 텔레그래프 패턴 구현.
- 건물 전투: 건물 피해·파괴와 전체 건물 파괴 시 패배 구현.
- 맵/카메라: 중앙 플레이 클러스터와 NavMesh, 카메라 맵 범위 제한 구현.

## 이번 SAVE 작업과 검증
- 씬 `FlyingAnt` 원본을 실제 `FlyingAnt` 컴포넌트로 연결하고 Ground 레이어 마스크를 설정했다.
- Ranged/Flying만 공중 대상을 공격하며 Melee/Defense/Support와 WildMonster는 공중 대상을 공격하지 않도록 적용했다.
- 보스의 지상 타겟 탐색과 원·원뿔·직선 범위 공격에서 공중 유닛을 제외했다.
- 공중 대상을 추적할 때 비행 고도가 누적되지 않도록 지면 기준 고도를 다시 계산한다.
- `FarmData`, 비활성 `FarmTemplate`, HUD 건설 버튼을 추가했다. 밭은 Soil 30, 건설 4초, 성장 20초, 수확량 Food 100의 임시값을 사용한다.
- 기존 `ResourceNode`에 선택적 재성장을 추가해 기존 일반 자원은 그대로 두고 밭만 반복 수확하도록 했다.
- Unity 컴파일 오류 0건, 비행 컴포넌트·대공 판정·고정 고도 검증 통과.
- Unity에서 밭 첫 성장·수확 소진·반복 재성장 검증 통과.
- 공식 Unity CLI 1.0.0-beta.8과 `com.unity.pipeline` 0.6.0-exp.1을 설치·연결하고 Codex/Claude Code용 Unity Pipeline 스킬을 추가했다.
- Codex와 Claude Code에 Ponytail 4.9.0을 적용했다.

## 다음 작업 우선순위
1. 실제 마우스 조작으로 밭 배치·일개미 건설·수확·창고 반납 전체 흐름을 확인한다.
2. 밭의 성장 중/수확 가능 상태를 최소 시각 피드백으로 구분한다.
3. 낚시 최소 프로토타입을 구현한다.
4. 특수 자원과 적 소굴 약탈의 최소 흐름을 구현한다.
5. 주기적 침공 방어전과 적 AI 성장 기반을 구현한다.

## 구현 시 주의
- `.prefab`/`.prefab.meta`와 `Assets/_TeamImport/`는 Git 커밋 금지.
- 실제 개미 종, 역할별 건물 명칭, Support 액티브 스킬, 작물 종류와 농사 밸런스는 미정이다.
- 기획 변경은 Notion 개별 하위 페이지와 관련 참조 페이지를 함께 갱신한다. 기획 부모에는 `replace_content`를 사용하지 않는다.
- 건설 배치 미리보기는 현재 템플릿 크기의 큐브이며 외형 확정 뒤 교체가 필요하다.
- `SnapToTerrainMenu` Raycast는 추후 지형 Collider만 대상으로 제한해야 한다.
- Unity 제어·테스트·캡처는 공식 `unity` CLI와 `com.unity.pipeline`을 우선 사용한다.
- Unity는 Play Mode가 종료된 상태로 유지한다.

## 이번 SAVE 개발 일지
- https://app.notion.com/p/3d6c4a0ecd3181a58580d3087cd58029
