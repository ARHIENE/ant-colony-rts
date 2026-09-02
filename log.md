# 프로젝트 로그

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트: `E:\Git\ant`
- 버전관리: Git/GitHub (`github.com/ARHIENE/ant-colony-rts`, public)
- 기획 원본: 사용자가 전달한 "개미 소굴 RTS(가제) 게임 기획서" 전문 (2026-09-02)
- 상세 작업 히스토리는 `changelog.md` 참고 — 이 파일은 "현재 상태 + 다음 할 일" 요약만 유지

## 한 줄 컨셉
플레이어는 개미 여왕의 지휘자가 되어 일개미로 자원을 캐고 소굴을 확장하며, 병정개미로 야생 몬스터를 상대하거나 다른 개미집을 약탈하고 거대 보스를 물량으로 레이드하는 RTS.

## 현재 상태 (세션 1 종료 시점 — 2026-09-03)
**MVP 수직 슬라이스 스크립트 + 씬 셋업 전부 완료, 컴파일 정상.** `Assets/Scenes/AntColony.unity`에 16개 오브젝트 배치·저장 완료(GameSystems, SelectionSystem, 카메라, 건물 4종, 자원지 4곳, 야생몬스터, HUD, 랜덤맵).

### 구현된 시스템
- **MVP 코어**: 자원(Food/Soil, `ResourceManager`) → 일개미 채집(`WorkerAnt`) → 건물(`QueenChamber`/`Barracks`/`Storage`/`DigSite`) → 병정개미 전투(`SoldierAnt`) → 승리 신호(`GameManager`). ScriptableObject 데이터 드리븐(`Assets/Data/*.asset`, `Tools/Ant Colony/Create Default Data Assets`로 생성됨)
- **랜덤맵**: `MapGenerator`(Perlin 노이즈 지형 + 텍스처 블렌딩 + 오브젝트 랜덤 스폰) — SIMUL-TeaamProject 참고 포팅. `xSize`/`zSize`/`octavesCount`/`noiseScale`/`heightMultiplier`/`lacunarity`/`persistance`에 `[Range]` 제한 있음(안전값: 10/10/1/0.03/7/2/0.5)
- **보스 레이드 시스템**: `Assets/Scripts/Boss/`(AoE 3종 + 텔레그래프 3종 + 패턴 루프 2종) — 스크립트만 준비, 씬에 아직 미배치
- **선택/명령**: `SelectableObject`+`SelectionManager`(드래그/클릭/Shift 추가선택) — **모든 개미가 선택(하이라이트) 가능**, 단 이동/공격 명령은 `SoldierAnt`만 받음(일개미는 자동 채집 유지)
- **비주얼**: 개미=검정(`AntBlack.mat`), 야생몬스터/적=빨강(`EnemyRed.mat`), 자원노드=갈색(`ResourceBrown.mat`)
- **에디터 유틸리티** (`Assets/Editor/`): `Tools/Ant Colony/` 메뉴 아래 — Regenerate Map(자동 재스냅 포함), Bake All NavMesh Surfaces, Snap Scene Objects To Terrain, Create Default Data Assets

### 알아둘 것
- **`HUD`(HUDController)의 `queenChamber`/`barracks`/`digSite` 필드가 비어 있음 — 버튼이 동작하려면 인스펙터에서 직접 드래그 연결 필요** (브릿지 툴이 씬 내부 오브젝트 참조는 못 걸어줌, 에셋 참조만 가능)
- `octavesCount`는 3~4 넘게 올려도 의미 없음(고주파 디테일 안 보임), 값만 커져서 위험 — 낮게 유지
- 지형 텍스처(`terrainLayers[0]`)는 임시 플레이스홀더(URP 로고 아이콘, Read/Write 꺼져있어 코드에서 스킵 처리됨) — 실제 아트 정해지면 교체
- unity-cli 브릿지 포트는 **16401**(FPS Manager는 16400, 프로젝트별로 다르게 설정돼 있음)
- 브릿지가 도메인 리로드 직후 종종 데드락(CPU 0%로 멎음, `Responding`은 계속 True로 나와서 구분 안 됨) — 이번 세션에 5차례 발생, 매번 프로세스 강제종료 후 재시작으로 해결. **재시작하면 마지막 `save_scene` 이후 작업은 유실**되니 씬 변경은 자주 저장할 것. 상세 대응법은 `changelog.md` 참고

## 다음 세션 할 일 (우선순위 순)
1. **HUD 참조 연결**(위 참고) — 이거 안 하면 Play 테스트가 의미 없음
2. Play 모드 전체 루프 실제 테스트(채집→저장→생산→전투→승리), 밸런스(비용/시간/데미지) 체감 튜닝
3. 선택된 유닛 정보 표시 UI(체력바 등) — 필요성 논의 중, 아직 미착수
4. 보스 레이드: 씬에 실제 보스 배치 + Unity Layer("Ants" 등) 설정 + `BossHealth.onDead`에 패턴 루프 정지 연결
5. 지형 아트: 플레이스홀더 텍스처를 실제 텍스처로 교체

## 미정/논의 필요 항목 (기획서 5번)
- [ ] 개미 종족(불개미/베짜기개미 등) 다양화 여부
- [ ] 보스 종류 및 패턴 상세 설계
- [ ] 아트 스타일(로우폴리 / 픽셀 / 스타일라이즈드 등)
- [ ] 세션 길이 목표
- [ ] 세션 내 성장 vs 세션 간 영구 성장 분리 여부(기획서 3.5)
