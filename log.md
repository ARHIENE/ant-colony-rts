# 프로젝트 로그

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트: `E:\Git\ant`
- 버전관리: Git/GitHub (`github.com/ARHIENE/ant-colony-rts`, public)
- 기획 원본: Notion "게임 개발 노션 정리 > 기획(스펙 문서)" 페이지 참고
- 상세 작업 히스토리는 `changelog.md` 참고 — 이 파일은 "현재 상태 + 다음 할 일" 요약만 유지

## 한 줄 컨셉
플레이어는 개미 여왕의 지휘자가 되어 일개미로 자원을 캐고 소굴을 확장하며, 병정개미로 야생 몬스터를 상대하거나 다른 개미집을 약탈하고 거대 보스를 물량으로 레이드하는 RTS.

## 목표 (2026-09-04 확정)
**MVP가 아니라 Notion "기획(스펙 문서)" 페이지에 적힌 내용 전체 구현이 목표.** 지금까지 구현한 건 그 중 극히 일부(여왕방/병영/창고/자원노드 각 1개, 일개미·병정개미 단일종)뿐 — 역할군별 병영 분리, 카테고리별 강화 트리, 농사/낚시, 특수자원, 약탈, 보스 레이드, 유지비/반란 등 대부분 미구현. 기획이 개발 중 바뀌면 Notion 해당 하위 페이지를 교체(추가 아님)하고 관련 페이지도 같이 동기화할 것.

## 현재 상태 (세션 2 종료 시점 — 2026-09-04)
- **`AntColony.unity` 씬이 SIMUL-TeaamProject의 실제 3DScene(400x400, 텍스처/장식 프리팹 1만개+ 이미 배치됨) 기반으로 완전히 교체됨.** 기존 세션 1의 10x10 자체 맵은 `Assets/Scenes/AntColony_MVP_backup.unity`로 보존(롤백용).
- 건물(QueenChamber/Barracks/Storage/DigSite)/자원노드(FoodNode1-2/SoilNode1-2)/WildMonster/GameSystems/SelectionSystem/HUD/Main Camera 전부 새 씬으로 이전 완료.
- NavMesh는 건물 밀집구역만(`NavMeshBakeArea`, Volume 모드 60x60) 베이크됨 — 전체 400x400은 안 구움.
- HUD 버튼 참조(`queenChamber`/`barracks`/`digSite`)는 `FindFirstObjectByType`로 자동 탐색하도록 수정 완료 — 인스펙터 수동 연결 불필요.
- 카메라(`IsometricCameraController`)를 스타크래프트식(마우스 가장자리 스크롤 팬 + Q/E 궤도 회전)으로 전면 재작성 완료.
- 컴파일 에러 0개, 씬 저장 완료. **Play 모드 실전 테스트(전체 루프)는 아직 사용자 확인 대기 중** — unity-cli 브릿지가 Play 진입 시 데드락 나서 자동 검증 불가(알려진 미해결 이슈, 아래 참고).

## 알아둘 것
- `Assets/_TeamImport/`는 SIMUL-TeaamProject(팀 원본 저장소, private) 반입본 — 1.65GB, 유료 에셋스토어 패키지 다수(사용자 본인 구매분이라 라이선스 문제는 아님, 용량 때문에 제외). **git에 절대 안 올라감**(`.gitignore` 처리 완료). 스크립트는 `_TeamImport.asmdef`로 격리돼 우리 `AntColony.*` 코드와 네임스페이스 충돌 안 남.
- unity-cli 브릿지는 Play 진입/스크립트 재컴파일(도메인 리로드) 시마다 데드락 나는 게 사실상 일상화됨(이번 세션 8회 이상 재현, 맵 크기와 무관함 확인됨) — 코드/씬 수정 후 컴파일만 확인하고 실제 동작 검증은 사용자가 Play로 직접 하는 흐름.
- NavMesh는 건물 구역만 좁게 베이크돼 있음 — 플레이 영역을 넓히면 `NavMeshBakeArea`의 `size`/`center`를 넓혀서 `Tools/Ant Colony/Bake All NavMesh Surfaces`로 재베이크 필요.
- 건물들이 400x400 지형의 원점 구석(로컬 좌표 0~10 구간)에 몰려있음 — 지형 위에 정상적으로 얹혀있긴 하지만 위치 자체는 아직 재배치 안 함.
- SAVE 시 ant 프로젝트도 Notion "개발 일지" 페이지(FPS Manager와 공유, https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040)에 날짜 하위 페이지 생성 + unity-cli 스크린샷/영상(mp4/webm) 첨부하도록 CLAUDE.md에 추가됨(2026-09-04).

## 다음 세션 할 일 (우선순위 순)
1. Play 모드 전체 루프 실제 테스트(채집→저장→생산→전투→승리), 카메라 조작감(가장자리 스크롤/Q,E 회전) 확인
2. 건물들을 400x400 지형 중앙 쪽으로 재배치 검토(현재 원점 구석에 몰려있음)
3. 선택된 유닛 정보 표시 UI(체력바 등) — 필요성 논의 중, 아직 미착수
4. 보스 레이드: 씬에 실제 보스 배치 + Unity Layer("Ants" 등) 설정 + `BossHealth.onDead`에 패턴 루프 정지 연결
5. 지형/장식 아트는 이미 실제 팀 프로젝트 에셋으로 교체됨 — 추가로 필요하면 `Assets/_TeamImport/Prefabs/`에서 더 가져다 쓸 수 있음(SimpleNaturePack, TerrainSampleAssets 등)

## 미정/논의 필요 항목 (기획서 5번)
- [ ] 개미 종족(불개미/베짜기개미 등) 다양화 여부
- [ ] 보스 종류 및 패턴 상세 설계
- [ ] 세션 길이 목표
- [ ] 세션 내 성장 vs 세션 간 영구 성장 분리 여부(기획서 3.5)
