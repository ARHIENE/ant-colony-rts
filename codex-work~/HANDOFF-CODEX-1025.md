# Codex 그래픽 인계 — 2026-10-25

## 결과

보존식 표시 소스 패치와 괭이 애니메이션 FBX를 **적용 대기 산출물**로 완성했다. 본 프로젝트 원본에는 적용하지 않았다. 모든 작업 파일은 `E:/Git/ant/codex-work~/` 아래에 있다.

- 소스 패치: `GRAPHICS-1025.patch`
- 수정 소스: `src/Assets/_Project/Scripts/World/ProductionProp.cs`
- 최종 Blender 사본: `blender/pk-motions.blend`
- 최종 FBX: `fbx/Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx`
- FBX 대상/크기/SHA-256 목록: `FBX-MANIFEST.json`, 간단 경로 목록: `FBX-PATHS.txt`
- 오프라인 컴파일: `off/Assembly-CSharp.log`, `off/Assembly-CSharp-Editor.log`, `off/compile-results.json`
- 잔여 관통: `reports/remaining-tool-clearance.json`, `reports/remaining-armor.json`, `reports/remaining-armor.csv`

## 1. 보존식 표시

`ProductionProp.ForResource`에 `ResourceType.Food => "Prop_PreservedRation"` 한 줄을 추가했다. 기존 `ResourceNode.ConfigureLoot → ProductionProp.ShowResource → Attach` 경로를 재사용한다. 개인 소지 보존식을 바닥에 내려놓는 기존 `CommanderAnt.DropCarriedRations → DiplomacyManager.DropFloor` 경로도 이 표시 매핑을 사용한다.

표시 코드만 변경했다. 자원 지급/차감, 부패, 저장, 식사, 원래 자원 노드의 collider는 변경하지 않았다. 자연 채집 노드는 `ConfigureNatural`을 사용하므로 이 표시 연결의 대상이 아니다. 현재 데이터에는 별도 보존식 ResourceType이 없어 **바닥 Food 더미 전반이 보존식 모델로 표시된다**. SimpleMeal/FineMeal/RottenFood에는 매핑을 추가하지 않았다. 개인 소지품 모델이나 식사 중 손 소품은 이번 범위에 추가하지 않았다.

기존 FBX 리소스 존재, 기존 호출 연결, 소스 차이가 위 한 줄뿐임을 확인했다. 실제 Unity 표시와 머티리얼은 미검증이다.

## 2. 괭이 손목 보정 — 최종본

기존 `repair_wrist_roll.py`의 AST 함수 재사용(arm_cost/solve_arm/clearance/wrist_bend/tool_length)을 기반으로 제한된 경로 이동을 추가했다. 회전만으로는 60° 미만을 충족하지 못했다. 도구 경로 이동은 최대 약 3cm이며, 원래 오른손 손목이 59° 미만인 프레임의 팔 자세는 보존했다. 나머지 프레임만 두 팔을 다시 풀고 왼손 grip2를 맞췄다. 타이밍/이벤트/클립 길이/리그 구조/도구 메시와 강체 길이는 유지했다.

최종 재현 스크립트: `blender/scripts/repair_wrist_surgical.py`.
검증·출력 스크립트: `blender/scripts/validate_export_till.py`.

| 측정 | 원본 | 최종 |
|---|---:|---:|
| 오른손 최대 손목 각도 | 69.7887° | 56.1501° |
| 왼손 최대 손목 각도 | 27.1637° | 26.6769° |
| 왼손 grip2 최대 오차 | 0.000260mm | 0.000323mm |
| 도구 강체 길이 최대 변동 | 0.000190mm | 0.000114mm |
| 도구 ↔ 오른팔 보호대 관통 | 4/37프레임 | 0/37프레임 |
| 도구 ↔ 하부 흉갑 관통 | 1/37프레임(f15) | 1/37프레임(f15) |

측정은 애니메이션 1~36 및 루프 폐합 37프레임, 모든 정수 프레임을 사용했다. 손/손가락의 정상 도구 접촉은 기존 검사와 동일하게 제외한다. 삼각형 교차를 검사하며 프레임 사이의 연속 충돌이나 완전 포함까지 증명하지 않는다.

최종 FBX는 기존 exporter와 같은 -Z forward/Y up, no leaf bones, armature only, bake simplify 0 옵션으로 출력했다. Blender 재가져오기 후 **1~36 전체 프레임**의 양손/양손 도구 소켓 위치 최대 오차는 **0.000647mm**, 프레임 범위는 importer의 +1 이동을 반영한 2~37이다. Unity 임포트 통과를 의미하지 않는다.

최종 파일에서 88개 액션의 curve/key/handle 서명을 원본과 대조했다. 변경된 액션은 `PK|Tool_Till` 하나이며 추가 액션은 없다(`reports/action-integrity.json`).

### 폐기한 후보

- 넓은 회전 탐색/실제 관절 각도 탐색: 63.3°, 기준 미달.
- 거친 3cm 이동: 55.1°지만 새 흉갑 관통 발생, 폐기.
- 이동+도구 관통 제한: 67.2°, 기준 미달.
- 전체 프레임을 보정한 세밀한 이동: 54.2°, 도구 기준은 통과했지만 보호대/머리/옷 겹침 증가로 폐기.
- 최종은 문제 프레임만 수정한 56.2° 후보다. `pk-motions-fine-candidate.blend`와 중간 보고서는 최종 적용 대상이 아니다. 중간 JSON의 accepted=true는 당시 제한된 도구 검사 결과이며 최종 승인 의미가 아니다.

## 3. 남은 도구 관통과 판갑 겹침

### 도구

최종 사본으로 기존 2프레임 간격 검사를 다시 실행했다(`reports/remaining-tool-clearance.json`).

| 동작 | 남은 관통 |
|---|---|
| SmithRepair / Tool_Chop / Tool_Hammer / Tool_Harvest | 검사 샘플에서 없음 |
| Tool_Mine | 흉갑·하부 흉갑 각각 1/21 샘플, f17 |
| Tool_Dig | BOB 머리 6/21 샘플, f9/11/19/21/23/25 |
| Tool_Till | 하부 흉갑 1/19 샘플, f15 (별도 전체 프레임 검사에서도 1/37) |

곡괭이/삽도 기존 제한된 회전 후보를 재검사했으나 56.1°/53.7°에서 추가 개선 조건을 만족하지 않아 수정하지 않았다(`reports/wrist-other.json`). 새로운 도구 메시 수정이나 갑옷 형상 변경은 적용하지 않았다.

### 판갑/보호대/거치 장비

원본 전체 1230개 샘플(3프레임 간격)을 다시 검사하고, 변경한 Tool_Till은 원본/최종 사본을 같은 격리 상태·같은 12개 샘플로 비교했다. 해당 클립에서 item-part별 최대 삼각형 교차 쌍 수가 증가한 항목은 없다. 감소한 항목은 다음 둘이다.

- 왼팔 보호대 ↔ LINEN_ARMOR 옷: 최대 31→28쌍.
- 오른팔 보호대 ↔ SIDE 머리: 최대 10→8쌍.

`reports/till-armor-delta.json`은 이 비교 결과다. 전체 원본 검사 결과에서 Tool_Till만 최종 검사로 교체한 잔여 목록은 `reports/remaining-armor.json` 및 CSV이다. **94개 item-part 조합에 교차가 남는다.** 예:

| 장비 ↔ 부품 | 교차가 나타난 클립 수 | 최대 삼각형 교차 쌍 |
|---|---:|---:|
| 하부 흉갑 ↔ LINEN_ARMOR 옷 | 84 | 271 |
| Fauld ↔ BASE_ARMOR 몸 | 56 | 222 |
| 오른쪽 Tasset ↔ BASE_ARMOR 몸 | 31 | 209 |
| 왼쪽 Tasset ↔ BASE_ARMOR 몸 | 33 | 200 |

쌍 수는 관통 깊이(mm)가 아니다. 접촉도 교차로 잡힐 수 있으며, 장비/머리 변형 조합 전체가 실제 게임에서 동시에 보인다는 의미도 아니다. 샘플 사이 무관통을 증명하지 않는다. 형상을 임의로 깎거나 전체 판갑을 이동하지 않았고, 잔여 문제는 해결 완료로 기록하지 않는다.

## 4. 컴파일·패치·원본 보호

- Unity 6000.5.8f1 `DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`과 현재 `Library/Bee/artifacts/1900b0aE.dag/*.rsp`를 재사용했다. 수정 소스 경로만 src 사본으로 대체하고 출력/참조 출력은 off로 옮겼다. 현재 프로젝트의 누락된 새 스크립트도 포함했다.
- Assembly-CSharp: exit 0, 오류 0, 기존 경고 293.
- Assembly-CSharp-Editor: exit 0, 오류 0, 기존 경고 13. Editor 컴파일은 이번에 만든 게임 어셈블리 참조를 사용했다.
- 소스 generator/analyzer는 제외한 오프라인 컴파일이다. Unity 공식 recompile/Play/실행 검사 결과가 아니다. AgentScripts 실행 검사도 하지 않았다.
- `git -c core.whitespace=cr-at-eol apply --check --whitespace=error codex-work~/GRAPHICS-1025.patch` 성공. 현재 원본의 CRLF를 유지한 패치다. 실제 apply는 하지 않았다.
- 수정 대상 원본, 원본 Blender 및 키트 Python, 관련 기존 도구 FBX 등 53개 파일의 SHA-256이 시작 시와 동일함을 확인했다(`reports/original-integrity.json`). UI 파일 수정 없음.
- Unity CLI/Play/eval/캡처/커밋/push/SAVE/Notion 기록은 하지 않았다. 작업 보드는 원본 대신 `codex-work~/IMPLEMENTATION_BOARD.md`에 범위를 적었다. Graphify 읽기 조회만 사용하고 쓰기 제한 때문에 원본 그래프 update는 하지 않았다.
- Blender가 사본 저장 시 외부 썸네일 경로 `\\.thumbnails\\large`에 쓰지 못했다는 비치명적 로그가 있다. blend 저장·재열기·FBX 검증은 성공했다. 출력물로 썸네일을 제공하지 않는다.
- 카카오톡 완료 알림 도구는 현재 세션에서 발견되지 않아 발송하지 못했다.

## 5. 다음 적용·검증

1. `GRAPHICS-1025.patch`를 프로젝트 루트에서 적용한다. CRLF 검사를 쓸 경우 위 `core.whitespace=cr-at-eol` 옵션을 사용한다.
2. `FBX-MANIFEST.json`의 source를 target에 복사한다. 기존 대상 `.fbx.meta`는 유지한다. 이번 바이너리는 1개이며 텍스트 패치에는 포함되지 않는다.
3. 원본 Blender 키트에도 보정을 보존하려면 별도 검토 후 최종 `blender/pk-motions.blend` 사본을 반영한다. 생성기를 다시 돌리면 괭이 후처리를 다시 적용해야 한다.
4. 허용된 별도 세션에서 Unity import/recompile 및 보존식 표시/머티리얼, 괭이 애니메이션·장비 조합의 실제 화면을 검증한다. 현재 캐릭터 키/팔레트/활/시설의 기존 미검증 항목도 여전히 남아 있다.

**이번 결과는 오프라인 검증을 통과한 적용 대기 산출물이며, Unity 화면 검증 또는 전체 그래픽 작업 완료가 아니다.**

## 2차 — 2026-10-25 삽·곡괭이 관통 보정

### 현재 결과

**곡괭이만 검증 완료·적용 대기 FBX로 출력. 삽은 기준 미달로 미적용·미출력.** 2차 전체를 완료/통과로 취급하지 않는다.

사용자 확인: 1차 GRAPHICS-1025 패치와 괭이 FBX는 이미 본 프로젝트에 적용됐고 컴파일 오류 0이었다. 이 정보는 사용자 제공 결과이며 이번 세션에서 Unity로 재검증한 결과가 아니다. 아래 2차 결과가 1차의 당시 '적용 대기' 상태보다 최신이다.

### 곡괭이 PK|Tool_Mine — 통과

현재 원본 Blender 사본에서 **f16과 f17만** 보정했다. f16은 2프레임 샘플에서 빠졌던 하부 흉갑 관통이다. f16의 도구 경로 이동은 1cm, f17은 약 2.45cm이며 손잡이 roll은 둘 다 0°다. 기존 두 팔 solver를 재사용하고, 다른 프레임은 원래 팔 자세를 유지했다.

| 기준 | 결과 |
|---|---|
| 흉갑 관통 | f17 1/41 → 0/41 |
| 하부 흉갑 관통 | f16·f17 2/41 → 0/41 |
| 다른 도구 관통 | 증가 0. 기존 오른팔 보호대 f24 1/41은 유지 |
| 최대 손목 각도 | 오른손 56.1169°, 왼손 44.1601° (양손 모두 ≤60°) |
| 왼손 grip2 최대 오차 | 0.000454mm (<1mm) |
| 도구 강체 길이 변동 | 0.000215mm |
| 타이밍·이벤트·길이 | 전후 완전 일치: 40프레임/24fps, loop=true, hit=f19. 원본 action key 범위 1~41 유지 |
| 액션 변경 범위 | 원본 88개 중 PK\|Tool_Mine 하나만 변경. Tool_Dig와 다른 액션은 그대로 |

도구 검사는 **1~40 및 루프 폐합 f41 전체 정수 프레임**을 검사했다. 별도로 기존 fit_check의 판갑·보호대·투구·거치대·휴대 장비까지 재사용해 두 클립의 80프레임(1~40, step=1)을 원본/최종 사본에서 비교했다. 프레임별 item-part 삼각형 교차 쌍 수를 비교한 결과 **새 교차·교차 쌍 증가 0**이었다(원본 650개 비영(非零) 프레임-조합 기록, 최종 647개). 폐합 f41도 탐색기에서 도구/착용 장비 조건을 확인했다. 기존 접촉·겹침이 모두 사라졌다는 뜻은 아니다.

- 최종 FBX: `fbx/Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Mine.fbx`
- Assets 대상: `Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Mine.fbx`
- 크기: 534,348 bytes
- SHA-256: `49bc9a17ffdbf2ec9729350fe009b38eb9913f484a4eff2ab6b62692955f138d`
- 기존 `.fbx.meta` 유지. 텍스트 소스 변경/새 패치는 없다.

FBX는 임시 위치에 출력한 뒤 Blender로 다시 가져와 **40개 정수 프레임 × 60개 뼈**의 위치·회전을 원본 사본과 비교했다. 최대 위치 오차 **0.000764mm**, 측정 회전 차이 0rad, importer 범위 2~41(시작 +1 이동)로 통과했다. 검증 후에만 위 fbx 경로에 게시했다.

### 삽 PK|Tool_Dig — 실패, 출력하지 않음

원본을 모든 정수 프레임으로 다시 확인하니 BOB 머리 관통은 기존 6/21 샘플보다 많은 **14/41프레임**이었다.

- 관통 프레임: **f8~12, f18~26**.
- 원본 최대 손목: 오른손 **53.6976°**, 왼손 **47.4278°**.
- 원본 왼손 grip2 오차: **0.000308mm**.
- f18의 원본 삽↔BOB 교차는 **삼각형 9쌍**. 이는 관통 깊이(mm)가 아니다.

f8~12에서는 안전 조건을 만족하는 개별 후보를 찾았지만 f18에서 클립 전체 통과를 막았다. **부분 후보는 채택하지 않았으며 최종 사본의 Tool_Dig도 원본 그대로다.**

완료된 f18 탐색(이동 반경 3cm, 1cm 격자, roll -45°~45°/15° 간격):

| solver | 실제 팔 해를 계산해 검사한 후보 | 착용 장비 정적 교차 증가로 제외 | 나머지 착용 장비 교차 증가 | 도구 교차 증가/목표 잔존 | 손목 >60° |
|---|---:|---:|---:|---:|---:|
| 기존 최소 손목 solver | 590 | 351 | 73 | 84 | 82 |
| 원래 팔꿈치 위치 선호 solver | 590 | 364 | 69 | 102 | 55 |

위 제외 수는 서로 배타적인 마지막 실패 분류이며 각 행 합이 590이다. 그 외 목표 BOB와의 정적 교차를 먼저 검사해 탈락한 위치 후보도 있다. **이 범위에서 안전 후보를 찾지 못한 것이지, 모든 가능한 보정이 불가능함을 증명한 것은 아니다.**

5cm 이동과 작은 X/Z 기울임도 탐색했지만 통과 후보를 확보하지 못했다. 이 두 추가 탐색은 **일부 후보만 검사하고 종료했으므로 전체 범위 검증 완료로 기록하지 않는다.** 마지막 진행 로그는 각각 450번째/600번째 해를 시작하기 직전이며, 직전까지 제외된 후보는 각각 449개/599개다. 5cm/기울임 전체 후보가 실패했다는 주장도 하지 않는다.

- 실패/부분 탐색 로그: `round2/reports/search-fast.log`, `round2-nearest/reports/search.log`, `round2-dig5/reports/search.log`, `round2-digtilt/reports/search.log`.
- 위 탐색 폴더의 후보/중간 파일은 적용 대상이 아니다.
- **Tool_Dig FBX를 새로 출력하거나 FBX manifest에 추가하지 않았다.** 남은 작업은 f18 안전 해 확보 후 f19~26 및 전체 클립 재검증이다.

### 2차 산출물·검증 근거

- 통합 목록 갱신: `FBX-PATHS.txt`, `FBX-MANIFEST.json`. **1차 괭이(이미 적용) + 2차 곡괭이(이번 적용 대기)** 두 항목이다.
- 최종 사본: `round2/blender/pk-motions-round2.blend`.
- 종합 결과: `round2/reports/ROUND2-RESULTS.json`.
- 도구 독립 재검사: `round2/reports/baseline-tools-all-frames.json`, `final-tools-all-frames.json`.
- 프레임별 착용/거치/휴대 장비 비교: `round2/reports/fit-frames-before.json`, `fit-frames-after.json`, `fit-delta.json`.
- 타이밍/이벤트 비교: `round2/reports/timing-events.json`.
- FBX 전체 뼈 재가져오기: `round2/reports/fbx-validation.json`.
- 액션 변경 범위: `round2/reports/action-integrity.json`.
- 재현용 곡괭이 탐색기: `round2-mine/repair.py`. 사본에서만 실행한다.

이번 최종 Blender 사본은 **현재 art 원본의 사본**에서 곡괭이만 바꾼 것이다. 이전의 괭이 최종 사본 `blender/pk-motions.blend`와 별개다. 이번에는 곡괭이 FBX만 반입하고 이미 적용된 괭이 FBX는 유지한다. Blender 원본을 통째로 교체하려면 앞선 괭이 보정과 합치는 별도 검토가 필요하다.

2차 시작 시 확보한 원본 Blender·해당 소스·관련 도구 FBX 총 8개 파일의 SHA-256이 종료 시에도 일치했다(`round2/reports/original-integrity.json`). 기존 프로젝트 파일/UI를 수정하지 않았다. Unity CLI/Play/eval/캡처/컴파일/커밋/Notion 작업을 하지 않았다. 소스 변경이 없어 2차 오프라인 컴파일은 별도로 반복하지 않았다. Graphify는 읽기 조회만 사용했다. 카카오톡 도구는 제공되지 않아 완료 알림은 발송하지 못했다.

**Unity 임포트·실제 실행 화면·정수 프레임 사이 연속 충돌은 미검증이다. 삼각형 교차 검사에는 접촉도 포함될 수 있으며 완전 포함을 검출하지 못할 수 있다.**

## 검사 — 2026-10-25 D4 병합 후 실패 원인 분석

### 산출물과 상태

**검사 전제 3건을 수정한 적용 대기 패치다. 게임 코드의 결함을 확정할 증거는 없어 게임 코드는 수정하지 않았다. Play 재실행·통과는 미검증이다.**

- 패치: `CHECKS-1025.patch` — 현재 본 프로젝트 파일에 적용 가능함을 `git apply --check --whitespace=error`로 확인. 실제 apply 안 함.
- 사본: `src/AgentScripts/WorldMapChecks.cs`, `SettlementDefenseChecks.cs`, `TransportRouteChecks.cs`.
- 세 원본 모두 LF이며 사본/패치에서도 LF 및 원래 BOM 유무를 보존했다(`checks/reports/source-manifest.json`).
- 전체 게임 + 검사별 별도 어셈블리 오프라인 컴파일 결과: **모두 오류 0**.
- 기준: `docs/IMPL-D4-HANDOFF.md`, `docs/PLAY_CHECK_DECISIONS_1023.md`의 G02/G05/G08/G11/G13/G29/G69. 사양을 바꾸지 않았다.
- 사용자 확인: 곡괭이 FBX는 본 프로젝트에 적용 완료. 이 검사 작업에서는 그래픽/FBX를 변경하지 않았다.

### 1. WorldMapChecks — 돌 8000→8010

**코드로 확정한 사실**

1. `Core/CrashLandingStart.cs:41`이 시작 바닥 돌 20개를 생성한다(G02).
2. 검사에서 장수들의 기존 작업을 중단한 뒤, 건설 담당자에게 Building과 **일반 Hauling도 켠다**(원본 검사 125~126행).
3. `Units/CommanderDuty.cs:279` 이하의 운반 작업 후보에는 바닥 물자가 포함되며 `JobFor`는 loose cargo를 Hauling으로 분류한다. 바닥 돌을 창고로 옮기면 `WorkerAnt.Deposit → ResourceManager.AddTo`로 보유 재고가 늘어난다.
4. 연구대 고정 비용은 `Buildings/EarlyConstruction.cs`의 목재 20·돌 10이다(G29).
5. 건설 재료는 `WorkerAnt.TickFetching → TakeFrom`에서 창고에서 꺼내고, `TickDelivering → BuildingConstructionSite.Deliver`에서 배송 수량으로 옮긴다. `BuildingConstructionSite.BeginWork:135`는 소비 기록을 남긴다. 정상 완공 경로에서 돌을 되돌려 주는 호출은 찾지 못했다. `Cancel:183`은 도착 자재를 바닥으로 내놓고, `ReturnWorkforce:228`은 시민 예약만 해제한다. 이것을 돌 환불 함수로 볼 수 없다.

**실제 관측 원인: 강한 추정**

`8000 + 시작 바닥 돌 20 - 건설 돌 10 = 8010`이 보고된 값과 정확히 일치한다. 나무만 정상 차감된 사실도 이 경로와 맞는다. 다만 해당 Play 실행의 자원 입출고 로그를 받지 않았으므로 실제 20개가 그 바닥 더미에서 왔다는 것까지 실행으로 확정하지 않는다. 중복 반환 버그를 발견했다고 보고하지 않는다.

**수정**

검사의 일반 Hauling 활성화 한 줄을 제거했다. Building 작업은 `CommanderDuty.cs:242` 이하에서 `site.PendingPickup → CommandFetchFor`를 통해 건설 자재를 직접 배송하므로 시공·배송은 실제 경로 그대로 검사한다. 기존의 정확한 `기존 재고 - 고정 비용` assertion을 유지하며, 시작 돌이나 게임의 운반 동작을 삭제·변경하지 않는다.

### 2. SettlementDefenseChecks — one commander captured and hidden

**코드로 확정한 결함: 난수에 대한 검사 가정**

검사는 시드를 찾아 연속 두 `Random.value`가 `<.5`, `>=.5`이면 첫 장수가 포로, 둘째가 탈출한다고 가정한다. 실제 `World/SettlementDefense.Lose:167`은 각 장수마다 다음을 실행한다.

`포획 난수 → TakeDamage(float.MaxValue) → CommanderAnt.OnDowned → 부상 생성/추가 부상 난수 → OnCaptured/CapturedBy → 다음 장수의 포획 난수`

`Units/CommanderPersonal.cs:216~222`와 `CommanderHealth.cs`의 `MedicalRules.AddRandomWound`가 추가 Unity 난수를 소비한다. 건강한 포로라면 `World/DiplomacyPrisoners.cs:56~58`도 별도 부상 난수를 소비할 수 있다. 따라서 **두 번째 미리 뽑은 값은 실제 두 번째 포획 값이 아니다.** 사망 설정/기존 중상에 따라 첫 포획 후보가 사망하는 경로도 존재한다.

**실제 실패 하위 분기: 미확정**

제공 메시지는 복합 조건 하나여서 실제 `Prisoners.Count`, 사망 여부, Captor, activeSelf 중 무엇이 달랐는지 알 수 없다. 두 명 모두 포로였는지, 후보가 사망했는지 등을 확정하지 않는다. 런타임 포로 처리나 포획 확률을 바꿔 검사를 맞추지 않았다.

**수정**

- 난수 고정 전에 첫 수비자를 실제 `TakeDamage`로 쓰러뜨린다. 준비 중에만 사망 모드를 Gentle로 바꾸고 `finally`에서 즉시 복원한다. 병력 소진이 정확히 한 번 반영되는지도 별도 assertion으로 확인한다.
- 무작위 출혈/감염을 제거하고, 고정된 비출혈 타박상 하나를 둔다. 이렇게 하면 포획 시 `TakeDamage`가 이미 쓰러진 장수를 다시 처리하지 않고, `CaptureInjury`도 이미 부상 중이므로 추가 난수를 소비하지 않는다.
- 주둔 명부가 `[captive, escapee]` 순서인지 확인한 뒤 기존 bounded 시드 탐색과 포로/탈출 두 분기 assertion을 유지한다. 전역 난수 상태는 기존 `finally`에서 복원한다.
- 복합 실패 메시지에 포로 수·포로/사망 상태·개인 체력·Captor 일치·활성 상태를 추가했다.
- 구출 이후 옛 '60초 회복' 전제를 G11의 `GameCalendar.SecondsPerDay + 1`로 교체했다. 식량 부족이 회복 검사를 방해하지 않도록 포만 fixture도 준비한다. `CommanderRecovery.cs:18~30`의 현재 회복 조건을 따른다.
- 뒤의 대피 화물 검증에서 Food 17을 Wood 17로 바꿨다. 같은 G05 보급 소비가 대피/중복 하역 검사를 오염시키지 않게 한 것이다. 포로·탈출자의 Food 4.5/2.75 바닥 보존 검사는 유지한다.

이 검사는 이제 **이미 쓰러진 장수의 포획/구출**을 결정적으로 검사한다. 건강한 장수를 포획하면서 발생하는 의료 난수·사망 확률까지 이 검사로 검증한다고 주장하지 않는다. 병력 소진은 사건 직전 별도로 확인하고, 포획 이후 이중 손실이 없는지도 계속 비교한다.

### 3. TransportRouteChecks — both resources remain cargo until arrival

**코드로 확정한 사실**

`World/ExpeditionTransport.cs:213`은 Home이 아닌 모든 상태(Outbound/Deployed/Returning)에서 `TickSupplies(seconds)`를 호출한다. `World/ExpeditionSupplies.cs:31`은 수요를 FoodDebt에 누적하고, 정수 수요가 생기면 **식량 화물을 먼저 소비**, 부족하면 장수 개인 보존식을 소비한다. `UpkeepManager.enabled=false`로 이 별도 보급 경로가 꺼지지 않는다. G05는 병력을 포함한 원정 식량 소비를 명시한다.

따라서 Food 12가 도착 전까지 반드시 12라는 검사의 전제가 성립하지 않는다. 귀환 때의 `ship.Tick(ship.TravelSeconds)`도 보급을 진행하므로 뒤의 Food +11 정확 반입 기대값 역시 같은 문제가 있다.

**실제 실패 원인: 유력 추정**

원본 실패에는 실제 화물·본거지 수치가 없어 Food 소비 때문이었다고 단정할 수 없다. Soil 부족, 조기 귀환 등 다른 하위 조건의 실패 가능성은 Play 진단 전까지 남겨 둔다. 조기 화물 지급/중복 하역 버그를 확인한 것은 아니다.

**수정**

- 거점 검사 fixture의 식량 노드를 공개 `ConfigureLoot(Wood, 12)`로 목재 더미로 바꾸고, 두 비소모 자원 **Wood 12 + Soil 7**로 동일한 수집·반입·중단·초과 하역 검사를 수행한다. 다음 왕복도 Wood 11 + Soil 6으로 정확히 비교한다.
- 기존 본거지 식량 8000 공급은 유지한다. 원정 보급 코드를 끄거나 식량 소비 규칙을 바꾸지 않았다.
- 목재와 흙은 `ResourceManager.Key`에서 같은 재료 한도를 공유하므로 한도는 한 번만 추가한다. 두 번째 반입 공간도 각 자원용 공개 `TrySpend(type, 100)`으로 마련한다.
- 진단 문자열과 실패 메시지에 Wood/Soil 실제 화물, 본거지 목재 전후, FoodDebt를 남긴다. 수량 assertion을 느슨하게 바꾸지 않았다.

이 변경은 일반 화물 보존 검사다. **식량 소비량의 정확성을 별도로 검증한 것은 아니다.**

### 오프라인 검증·원본 보호

| 대상 | 컴파일 | 오류 | 기존 경고 |
|---|---|---:|---:|
| 현재 게임 전체 Assembly-CSharp (376 소스) | exit 0 | 0 | 293 |
| WorldMapChecks — 외부 검사 어셈블리 | exit 0 | 0 | 15 |
| SettlementDefenseChecks — 외부 검사 어셈블리 | exit 0 | 0 | 0 |
| TransportRouteChecks — 외부 검사 어셈블리 | exit 0 | 0 | 0 |

- Unity 6000.5.8f1 내장 DotNetSdk Roslyn + 현재 Bee 응답 파일을 사용했다. 게임은 **현재 본 프로젝트 소스**만 사용하고, 이전 그래픽 작업의 src 사본을 게임 소스로 섞지 않았다.
- 검사 3개는 각각 수정 사본 한 파일만 포함한 별도 DLL로 컴파일하고 이번에 만든 게임 DLL을 참조했다. 따라서 같은 게임 어셈블리에 넣어 internal 접근을 우회하지 않았다. private/internal 조작이 필요한 기존 경로는 리플렉션을 유지했다.
- analyzer/source generator를 제외한 오프라인 정적 컴파일이다. Unity recompile 또는 eval 실행 결과가 아니다.
- 명령: `& 'C:/Users/Shim Hyeonyeop/AppData/Roaming/uv/tools/graphifyy/Scripts/python.exe' codex-work~/checks/compile.py`.
- 로그/응답 파일/DLL: `checks/off/`, 결과 집계: `checks/off/compile-results.json`.
- 패치 확인: `checks/reports/patch-check.txt`. 수정 대상 원본 3개 검사의 SHA-256은 시작 시와 동일하다. 금지된 `ImplG06Checks.cs`와 `FishingChecks.cs`는 최종 확인 때 시작 해시와 달라 외부 동시 변경으로 기록했다. 두 파일에 대한 쓰기는 수행하지 않았고 패치에도 포함하지 않았다. UI 폴더·게임 파일·기존 문서·.git 수정 없음.
- 쓰기는 codex-work~/ 내부에서만 수행. 작업 보드 표시는 이 폴더의 사본에만 남겼다. Graphify 읽기 조회만 했으며 원본 그래프를 갱신하지 않았다.
- Unity CLI/Play/eval/캡처/커밋/push/Notion/SAVE를 수행하지 않았다. 카카오톡 알림 도구는 제공되지 않아 전송하지 못했다.

### 다음 실행과 남은 불확실성

1. 본 프로젝트 루트에서 `git apply --check codex-work~/CHECKS-1025.patch` 후 적용한다. 패치에는 AgentScripts 3개만 있다.
2. AgentScripts는 eval 실행 검사이므로 이 패치만으로 Unity 게임 코드 재컴파일이 필요하지 않다. 허용된 별도 세션에서 각 검사를 새 Play 상태로 실행한다.
3. 포로 assertion이 계속 실패하면 새 메시지의 실제 상태를 확인한다. 수송 assertion도 Wood/Soil 화물·본거지 재고·FoodDebt를 보고 조기 복귀/화물 누락 여부를 구분한다.
4. WorldMapChecks 뒤쪽에도 식량 화물 수량을 비교하는 별도 구간이 남아 있다. 이번 세 실패 수정의 Play 재실행 전 해당 구간까지 통과한다고 보고하지 않는다. 새 실패가 나오면 현재 보급 규칙과 실제 수치를 대조해야 한다.

**이번에 확인한 통과는 오프라인 컴파일과 패치 적용 가능 여부뿐이다. 세 Play 검사 실패의 실제 해소 여부는 미검증이다.**
