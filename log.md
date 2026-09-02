# 프로젝트 로그

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트(작업 디렉토리): `E:\Git\ant`
- 버전관리: Git/GitHub 전환 완료(`github.com/ARHIENE/ant-colony-rts`, public). 기존 Plastic SCM 워크스페이스는 제거함
- 기획 원본: 사용자가 전달한 "개미 소굴 RTS(가제) 게임 기획서" 전문(2026-09-02 세션 시작 시 전달)

## 한 줄 컨셉
플레이어는 개미 여왕(플레이어 소굴)의 지휘자가 되어 일개미를 뽑아 자원을 캐고 소굴을 확장하며, 개미 부대로 다른 개미집을 약탈하거나 상대의 공격을 막아내고 거대 보스를 물량으로 레이드하는 RTS.

## 핵심 루프 (기획서 원문)
```
자원 채집 → 소굴 확장 & 건물 건설 → 개미 유닛 생산 & 강화
  → 정찰 & 목표 선택 → 전투(약탈/레이드) → 전리품 획득 → 반복(난이도 상승)
```

## 기획서 시스템 요약
- **자원**: 식량(Food) / 흙-토양-돌(Soil) / 특수자원(적 소굴·보스 전용, 강화용) — MVP는 Food+Soil만 구현, 특수자원은 범위 밖
- **유닛**: 일개미(채집/건설), 병정개미(근접 전투, 계급 세분화 여지), 특수개미(정찰/은신, 미구현)
- **강화 방식**: 개체별 레벨업이 아닌 "종족 단위 진화"(연구소에서 연구 → 이후 생산되는 전체 유닛에 적용) — MVP 범위 밖(연구소 미구현)
- **건설/확장**: 여왕방/창고/병영/연구소/방어시설. 확장은 흙을 소모해 새 방을 뚫는 형태
- **전투/레이드**: 야생 개미집 약탈, 보스 레이드(물량 대 스케일), 부분 자동 전투 + 플레이어 타겟 지정 혼합
- **아키텍처 권장**: 프로토타입은 오브젝트 풀링 + 단순 State Machine(ECS는 이후 검토), NavMesh 기반 이동, ScriptableObject 데이터 드리븐

## MVP 수직 슬라이스 — 이번 세션 구현 완료
기획서 4.2절 범위(1~5) 그대로 구현. 대상: `Assets/Scripts/` 하위, 씬은 아직 사용자가 직접 구성해야 함(아래 체크리스트 참고).

### 폴더 구조
- `Data/` — `ResourceType`(enum), `UnitData`(SO: 비용/스탯/일개미·병정개미 전용 필드), `BuildingData`(SO: 비용/종류)
- `Core/` — `ResourceManager`(싱글턴, Food/Soil 보유량·저장한도), `ObjectPool`(제네릭 풀링), `GameManager`(싱글턴, 루프 완료 신호), `IDamageable`(전투 대상 공통 인터페이스)
- `Units/` — `AntUnitBase`(공통 체력/NavMeshAgent/풀 반환), `WorkerAnt`(Idle→MoveToNode→Gather→ReturnToStorage→Deposit FSM), `SoldierAnt`(Idle→MoveToTarget→Attack FSM, 근접 자동 교전), `UnitSelectionController`(드래그 박스 선택 + 우클릭 이동/공격 명령)
- `Buildings/` — `BuildingBase`(예치 지점 정적 레지스트리 공용), `QueenChamber`(본진, 시작 일개미 스폰 + 추가 일개미 생산), `Barracks`(병정개미 생산), `Storage`(저장한도 증가), `DigSite`(흙 소모로 확장 구역 활성화)
- `World/` — `ResourceNode`(Food/Soil 채집 노드, 유한량), `WildMonster`(전투 대상 더미, 사망 시 `GameManager.ReportWildMonsterDefeated()` 호출)
- `Camera/` — `IsometricCameraController`(고정 각도 X:30/Y:45 직교 카메라, WASD 팬, 스크롤 줌)
- `UI/` — `HUDController`(런타임 생성 Canvas: 자원 표시, 일개미/병정개미 생산 버튼, 확장 버튼, 승리 메시지 — 수작업 UI 제작 없음)

### 설계 메모
- 확장(흙 소모)은 실제 지형 파괴 대신 `DigSite`가 흙을 소모해 미리 배치된 `expansionZone` 오브젝트를 활성화하는 방식으로 단순화(지형 메시 변형은 범위 밖)
- 일개미는 완전 자동(채집 대상 자동 탐색), 병정개미만 플레이어가 드래그 선택 + 우클릭으로 이동/공격 명령
- Input System 패키지가 이미 설치돼 있어 `Mouse.current`/`Keyboard.current` 직접 폴링 방식 사용(별도 Input Actions 자산 없음), HUD 버튼 클릭 인식을 위해 `InputSystemUIInputModule` 사용
- Cinemachine 미설치 상태라 카메라는 고정 회전 + 직접 스크립트로 구현(의존성 추가 안 함)

## 아직 사용자가 직접 해야 하는 작업 (Unity 에디터)
1. `Assets/Scenes/AntColony.unity` 신규 씬 생성(기존 `SampleScene`은 그대로 유지)
2. 바닥 지면(Plane 등) 배치 후 **NavMesh Bake**(AI Navigation 패키지의 Surface 컴포넌트 사용)
3. 빈 GameObject에 `ResourceManager`, `GameManager`, `ObjectPool` 각각 부착(싱글턴이므로 씬에 하나씩만)
4. 메인 카메라에 `IsometricCameraController` 부착, 별도 빈 GameObject에 `UnitSelectionController` 부착
5. 일개미/병정개미 프리팹 제작(캡슐/구체 등 임시 메시로 충분) — 각각 `WorkerAnt`/`SoldierAnt`(`AntUnitBase` 상속) + `NavMeshAgent` 컴포넌트 부착
6. `Data/` 폴더 우클릭 → Create → AntColony → Unit Data / Building Data로 SO 에셋 생성, 인스펙터에서 비용/스탯 값 채우기(현재 기본값은 임시 플레이스홀더)
7. `QueenChamber`(본진), `Barracks`(병영), `Storage`(창고), `DigSite`(확장 지점) 오브젝트를 씬에 배치하고 각각 컴포넌트 부착 + 인스펙터에서 SO/프리팹/ObjectPool 참조 연결
8. `DigSite`의 `expansionZone`에 연결할, 처음엔 비활성화된 확장 구역 오브젝트(추가 건물 배치 가능 구역) 제작
9. 야생 자원지에 `ResourceNode`(Food/Soil 각 1~2곳) 배치, 전투 대상으로 `WildMonster` 1개 배치
10. 빈 GameObject에 `HUDController` 부착 후 인스펙터에 `QueenChamber`/`Barracks`/`DigSite` 참조 연결
11. Play 모드로 전체 루프(채집→저장→생산→전투→승리 메시지) 실제 테스트, 밸런스(비용/시간/데미지) 체감 튜닝

## 미정/논의 필요 항목 (기획서 5번 그대로 이관)
- [ ] 개미 종족(불개미/베짜기개미 등) 다양화 여부
- [ ] 보스 종류 및 패턴 상세 설계
- [ ] 아트 스타일(로우폴리 / 픽셀 / 스타일라이즈드 등)
- [ ] 세션 길이 목표 (1회 플레이 몇 분~몇 시간)
- [ ] 세션 내 성장과 세션 간 영구 성장 분리 여부(기획서 3.5)

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/진행상황 파악할 것
- 스크립트는 컴파일 통과 여부만 이번 세션에 확인 가능(씬 미구성) — 위 체크리스트 진행 후 실제 플레이 테스트는 다음 세션에서 확인
- MVP 이후 우선순위(기획서 대비 아직 없음): 연구소/종족 진화 시스템, 특수자원, 야생 개미집 약탈, 보스 레이드, 정찰/시야, 지형 파괴형 확장, 멀티플레이 대비 구조 분리
- 다른 두 프로젝트(1VS1 Game, FPS Manager)처럼 unity-cli 브릿지를 이 프로젝트에도 도입할지는 아직 미정 — 필요해지면(Claude Code가 직접 Play/스크린샷 확인해야 할 때) 논의
