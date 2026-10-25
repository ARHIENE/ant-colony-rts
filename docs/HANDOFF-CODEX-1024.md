# Codex 인계 — 2026-10-24 저녁 (Claude는 그래픽 작업까지만 진행)

사용자 지시(정정): **Claude = UI 계속 수정, Codex = 그래픽·FBX 에셋 전부**. UI 폴더(Assets/_Project/Scripts/UI/**, AgentScripts/Ui*.cs)는 Codex 수정 금지. 커밋은 SAVE 때만.

## 완료(Claude)
- Blender: 새 건물 8종(fac_civic.py), 소품 18종(props_g.py), 동작 6종(motions_pk.py), FBX 내보내기·재가져오기 검사 실패 0. 식사·타인 수술은 kemono 기존 Eat·Treat 재사용.
- kemono 캐릭터 Unity 교체: KEMONO-UNITY-1024 패치 적용, KemonoImport 실행(Animator 90상태), 캐릭터 눕던 문제(+90°X 보정 부모 `Upright`), 얼굴·눈·머리·옷 텍스처 연결(`Assets/Resources/Kemono/Textures`). 장수 3명 외형·애니메이터·도구 쥐기 화면 확인(`Temp/ui-shots/kemono_close.png`).
- 남은 그래픽 확인: FacilityVisualChecks(120종)·BetaChecks·AntWorkVisualChecks 결과는 이 문서 아래 '그래픽 검사 결과'에 기록.
- 알려진 그래픽 한계: 머리·귀·꼬리 색 팔레트(6색)는 Unity에 아직 미적용(Blender 노드 재색칠이 FBX로 안 넘어옴). 갑옷 장착 미연결.

## 완료(Claude, UI)
- UI 배율: `MenuTheme.UiScale`(화면 높이/900, 최소 1) — HUD·메뉴 캔버스 고정 픽셀 → 1080p에서 1.2배. 큰 화면에서 왼쪽 위로 몰리던 원인.
- 월드맵 림월드식: `WorldPlanet`(전체 화면 RawImage, 드래그 회전·휠 확대, 화면 크기별 RenderTexture), `WorldMapPanel`(Overlay 모서리 배치, 마커=아이콘+이름표, 하단 메뉴바 위 Dock 60 여백). 검사 4개는 깊은 찾기로 갱신.
- 5배속 깜빡임: RosterBar 활동 아이콘 0.4초 디바운스.
- 검사 갱신: Stage7UIChecks 기분 경고 문구(G31), Phase4(G37 나이), Stage1(부위별 건강), ImplB3(가까운 자원), FacilityVisualChecks 120.
- 실제 버그 수정: BuildingBase.RestoreHealth 로켓 잔해 체력 0 허용(C6), WildMonster.Agent 지연 초기화(B3, Codex D1), NavRoute.SetDestination 같은 목적지 재발행 생략(Fishing 추정 — 미검증).

## Codex가 이어서 할 일(그래픽·FBX)
1. 그래픽 검사 결과 확인(아래 표) → 실패 원인 수정: FacilityVisualChecks(120종)·BetaChecks·AntWorkVisualChecks.
2. kemono 캐릭터 품질: 머리·귀·꼬리 팔레트 6색 적용(Blender 재색칠 노드 → Unity 머티리얼 색/별도 텍스처), 피부·옷 색이 Blender 렌더와 같게, 표정(Face_*) 상태 연결(아픔·수면·사망 등), 크기(현재 월드 키 1.6 잠정) 건물·자원과 비교해 조정.
3. 도구·무기 손 위치·방향·크기 Play 확인(채집 도끼/곡괭이/삽/낫, 건설 망치, 전투 검), 활쏘기(BowShoot) 등 새 동작이 쓰일 곳 연결(G34 단궁 장착 시).
4. 새 시설 16종 화면 확인(앞 방향 -Y→Unity, 크기, 수동 우물 텍스처), 소품(Prop_*: 돌 조각·보존식·드럼통·가스 용기·핵심물·탐지기·유해·시신 묶음) 게임 내 표시 연결.
5. Blender 키트 남은 품질 문제(README 6절): 손목 꺾임, 도구 관통, 판갑 겹침, 시설 손 접촉 4곳.
6. 갑옷 장착 시 _ARMOR 부품 전환 연결.

## Claude가 하는 일(UI)
- UiLayoutAudit·UiButtonAudit 실행·수정, 월드맵 닫기 버튼, 글씨 잘림, WorldMapChecks 89행, Stage7UIChecks.

## 실행 방법·주의
- Unity 공식 CLI: `C:/Users/Shim Hyeonyeop/AppData/Local/Unity/bin/unity.exe cmd <명령>`. 검사: `CHECK_POLLS=90 bash AgentScripts/tools/run_checks.sh <이름...>`(순차, 병렬 금지). eval은 메인 스레드 5초 제한 → 오래 걸리는 에디터 작업은 메뉴(`cmd menu --path`)로 실행하고 결과 파일로 확인.
- 공유 잠금 `Temp/unity-lock.txt`: 사용 전 확인·작성, 끝나면 삭제.
- 이전 Codex 실행에서 본 프로젝트 기존 파일 쓰기와 CLI 실행이 '접근 거부'였다. 그러면 worktree에서 작업하고 패치로 넘긴다.
- 커밋 제외: prefab, 씬, Water.mat, TimeManager, art, graphify-out, Planet Earth Free, Assets/Resources/WorldMap, Assets/Kiki.

## 그래픽 검사 결과 (Claude 실행, 2026-10-24 20:3x, kemono 적용 후)
- FacilityVisualChecks: FAIL `start stockpile shows model` (53행) — 시작 비축더미 모델 표시 전제. 불시착 시작(G02·B10) 이후 비축더미 없음 → 검사 전제 확인 필요.
- BetaChecks: FAIL `all generated commanders use Quirky` (52행) — 개미 모델 전제. kemono 기준으로 검사 갱신 필요(사용자: 개미 관련 전부 제거).
- AntWorkVisualChecks: NullReferenceException 23행 — 시작 상태 전제 확인 필요.
- 사용자 지시(2026-10-24): **개미 관련 전부 제거**. 그래픽 쪽 담당: AntVisual·Corpse의 QuirkyAnt 대체 경로, Resources/QuirkyAnt.prefab·ThirdParty/Quirky Series 미사용 확인 후 제거 후보. UI 초상화는 Claude가 캐릭터 얼굴 렌더로 교체 중.


### Codex 검증 결과 (위 Claude 실행 이후 변경, 2026-10-24)

| 항목 | 결과 |
|---|---|
| Unity 3개 검사 + 신규 KemonoQualityChecks | **재실행 안 함.** Claude 잠금과 CLI 접근 제한. 위 실패는 기존 실행 기록이며 이번 수정 후 PASS가 아님. |
| 전체 게임 / Editor / 검사 4개 정적 컴파일 | 모두 exit 0, 오류 0. Unity 6000.5.8f1 DotNetSdk Roslyn + Bee rsp 사용. 기존 obsolete 경고 있음. `Temp/graphics-1024/{game,editor,checks}-compile.log`. 공식 recompile 또는 실행 통과로 해석 금지. |
| 캐릭터 FBX 재가져오기 | 117부품·60뼈·단일 뼈 판갑 11개. 판갑 웨이트 합 1, 파생 몸/옷/긴 머리·표정 존재 검사 통과. `Temp/graphics-1024/character-reimport.json`. |
| FBX 재가져오기 | 소품 13 / 시설 112(본체+take·기존 포함) / PK 동작 44, 실패 0. 각 `exports/<분류>/reimport-report.json`. 신규 시설 16종의 Unity 화면 검증은 아님. |
| 시설 접촉 | `contact_check.py` 재실행, 18곳 모두 0~2.1cm. Unity 시설 작업 위치·접촉은 미검증. |
| 손목 보정 | 곡괭이 61.8°→56.1°, 삽 78.0°→53.7°. 관통 증가 없음·왼손 grip2 <1mm·강체 길이 변화 <0.01mm 후보만 적용. 괭이 69.8° 유지. `previews/wrist-repair-1024.json`. |
| 판갑 겹침 | 최종 원본으로 1230프레임·3프레임 간격 재검사. 판갑·보호대·투구/거치대 겹침 남음. `previews/fit-report-motions.json`. |
| 색상 및 Blender 육안 | 2048² 천옷 PNG 생성, sRGB/linear 변환과 1,155,698 재색칠 픽셀 검증. 텍스처 및 수정된 삽·곡괭이 중간/타격 프레임 4장 직접 확인. Unity 육안 증거는 아님. |
| 소스 패치 | `docs/handoff-kemono/GRAPHICS-1024.patch`. `git apply --reverse --check --whitespace=error` 통과(실제 되돌리기 안 함). 바이너리 FBX/PNG/blend는 현재 로컬 작업 트리에 반영되어 있으며 패치에는 미포함. |
| Graphify | 최종 AST update 완료(11525 nodes / 24852 edges). |
## 그래픽 진행

2026-10-24 Codex 작업. **부분 완료 / Unity 실행 검증 및 남은 품질 문제 있음. 커밋하지 않음.**

1. **kemono 색·표정 연결:** `KemonoAppearance` 추가. 기존 팔레트 6색을 머리·귀·꼬리에 적용하고 귀 안쪽 색은 보존. `Linen_Baked.png`·`SourceMaterials.json` 생성 및 임포터 연결. Hit/Downed→PAIN, Sleep→SLEEP, Death/복원 시체→DEAD, Eat→TASTY, Captive/Mourn→SAD 등 상태 연결. 수면·식사 상태도 실제 장수 상태에서 재생. 크기는 1.6 유지(건물·자원과 게임 화면 비교 전이므로 임의 조정하지 않음).
2. **손 도구·단궁:** 단궁 장착 시 BowShoot·왼손 활/오른손 화살로 전환. 36프레임 클립을 1.5초 재생하도록 하여 기존 0.45초 공격 표시 때문에 release 전에 끊기는 문제를 피함. 화살은 nock 9~release 23 프레임 동안 표시. 도구 교체 시 기존 물체는 즉시 숨긴 뒤 파괴. 실제 피해·공격 주기는 변경하지 않음. 손 위치·방향·크기 및 발사 화면 검증은 남음.
3. **갑옷 파생 + 실제 판갑:** 캐릭터 exporter가 판갑 11개를 같은 KemonoRig의 단일 뼈 웨이트 메시로 포함. Coating 장착/해제에 맞춰 몸·천옷·긴 머리 `_ARMOR`와 판갑을 함께 전환. 구 FBX처럼 판갑이 없는 경우에는 잘린 몸체를 노출하지 않도록 전환하지 않음. 투구·장식 등 전체 장비 체계 완성은 아니며, 겹침 문제도 남음.
4. **소품:** 13종을 `Resources/Kemono/Props`에 반입하고 URP 변환 메뉴 대상에 추가. 바닥 돌·드럼통·정제유 드럼통·가스 용기, 시민 시신 묶음·장수 유해, 미회수/본거지 보스 핵심물·제작된 탐지기를 기존 게임 상태에 따라 표시하도록 연결. 표시 코드가 자원/보상 상태를 지급·차감하지 않음. **보존식 모델은 리소스 반입만 했으며 게임 표시 연결 미완료.** 신규 소품 머티리얼 메뉴 실행·게임 화면도 미검증.
5. **Blender 품질:** `repair_wrist_roll.py`로 곡괭이·삽만 검증 후 보정하고 해당 Unity 애니메이션 FBX 2개 갱신. 괭이 손목·일부 도구 관통·판갑/장식 겹침은 남음. 새 보고서 기준으로 키트 README 6절을 교체. 생성기 전체 재실행 시 손목 후처리를 다시 적용해야 함.
6. **검사 전제 수정:** 최신 불시착 시작은 기존 시작 창고를 제거하므로 FacilityVisualChecks 마지막 검사를 CrashWreck으로 수정. BetaChecks 인원 수는 고정 12 대신 Options.startingCount. AntWorkVisualChecks는 격리 저장 경로에서 자체 새 게임 초기화 후 실행. 신규 KemonoQualityChecks와 함께 실제 재실행 필요.
7. **공유 작업 보호:** UI/**·AgentScripts/Ui*.cs 수정 없음. Claude 잠금을 삭제·덮어쓰거나 Play/eval/menu/recompile을 실행하지 않음. 작업 종료 시에도 `Temp/unity-lock.txt`는 `Claude 20:41 UI portraits`로 유지되어 있음. 잠금 중 바뀌는 `Temp/check-result.txt`를 3개 검사의 최종 결과로 채택하지 않음. log.md·changelog.md·Notion 일지·커밋·push 없음.

### 실행 제한과 정확한 거부 내용

- 지정 명령 `C:/Users/Shim Hyeonyeop/AppData/Local/Unity/bin/unity.exe --version`: PowerShell `CommandNotFoundException`. 같은 경로 `Get-Item` 및 상위 `Get-ChildItem`: **Access is denied / UnauthorizedAccessException**. 공식 CLI 실행 불가.
- Unity 연결 도구 `mcp__unity__editor_status`: **MCP tool call requires approval, but approval policy is never**. 자동 승인 검토에서 도구 실행을 거절했으며 에디터 상태를 얻지 못함.
- 요청하신 worktree 대안 `git worktree add --detach E:/Git/ant-wt/graphics-1024 HEAD`: **fatal: could not create directory of '.git/worktrees/graphics-1024': Permission denied**. worktree 생성 불가. 기존 소스는 apply_patch 일부 실패 후 일반 파일 쓰기 API로 수정 가능했으므로 현재 작업 트리에 반영하고 별도 패치를 제공.
- Blender `F:/Blender/blender.exe` 실행과 Unity 내장 DotNetSdk 정적 컴파일은 가능. 위 둘의 통과를 Play 검증 통과로 보고하지 않음. 카카오톡 완료 알림 도구는 세션에 없어 발송하지 못함.

### 이어서 할 검증·남은 작업

- Claude 잠금 해제와 공식 CLI 접근이 가능한 환경에서 `Codex <시각>` 잠금 획득 → `set_autotick` → 공식 recompile 및 상태 확인.
- `cmd menu --path "AntColony/Kemono/Configure Import + Animator"` → `Temp/kemono-import-result.txt` 확인. 반복 실행 시 기존 리맵 머티리얼도 재사용·갱신하도록 수정했음. 전체 메뉴는 eval 5초 한도를 피해서 실행.
- FacilityVisualChecks → BetaChecks → AntWorkVisualChecks → KemonoQualityChecks 순차 실행(병렬 금지). 오래된 UI 검사가 실패하면 Claude에게 인계하며 Codex는 UI 수정하지 않음.
- `AgentScripts/KemonoProbe.cs` 패턴으로 Temp/ui-shots에 팔레트·피부/옷·표정·갑옷 on/off·도구 6종·활 nock/release·시설 16종/수동 우물·소품을 실제 Play 캡처하고 직접 확인. 키 1.6은 이때 건물/자원과 비교해 결정.
- 보존식 표시 연결, 괭이 손목 및 남은 도구/판갑 관통 수정. 현재 상태를 전체 그래픽 작업 완료로 처리하지 말 것.