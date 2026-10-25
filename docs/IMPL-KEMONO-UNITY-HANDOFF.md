# KEMONO-UNITY-1024 인계 — 2026-10-24

## 상태
- **미완료: 에셋 반입·새 코드 작성·정적 검증 완료, 기존 코드 연결과 Unity 실행 검증은 차단됨.**
- develop 작업. 커밋·push·prefab 생성 없음. UI/** 및 AgentScripts/UiButtonAudit.cs 수정 없음. log.md·README·Notion SAVE 기록 없음.
- 기존 `Units/AntVisual.cs`, `World/Corpse.cs`, `Buildings/FacilityVisual.cs`는 **미수정**. 현재 게임에서 kemono 교체나 새 시설 연결이 완료된 상태가 아니다.

## 차단 원인
1. 사용자 제공 공식 CLI `C:/Users/Shim Hyeonyeop/AppData/Local/Unity/bin/unity.exe` 접근이 현재 셸에서 `Access is denied`. PowerShell Get-Item에서도 접근 거부를 확인했다. 실행 실패를 CLI 미설치나 에디터 종료로 해석하지 않는다.
2. 기존 AntVisual.cs 덮어쓰기는 Copy-Item 및 apply_patch 모두 접근 거부. 파일 속성에 ReadOnly는 없었다. 신규 파일·에셋 및 문서 작성은 가능했다. 기존 파일 삭제·재생성 등으로 제한을 우회하지 않았다.
3. 마지막 확인(한국 시간 19:18) `Temp/unity-lock.txt`는 `Claude 18:54 UiButtonAudit`. Codex는 잠금을 만들거나 삭제하지 않았고, Play/eval/recompile을 실행하지 않았다.

## 실제 추가 파일
- `Assets/Resources/Kemono/Kemono_Character.fbx`: 원본 캐릭터.
- `Assets/_Project/Art/Kemono/Anim/*.fbx`: base 44 + production 44, 총 88개.
- `Assets/Resources/Kemono/Handheld/*.fbx`: 71개 도구·무기·보상.
- `Assets/Resources/Facilities/CC_<이름>.fbx`: 아래 15개 신규 사본. 기존 CC_MushroomFarm 재사용.
- `Assets/_Project/Scripts/Units/KemonoModel.cs`: 선택형 머리·귀·꼬리, LINEN/중립 표정 활성화, 갑옷·부상·오버레이 숨김, 월드 키 1.6, 오른손 도구, 자원별 채집 클립. 시드 계산의 음수/오버플로 인덱스 문제 보완.
- `Assets/_Project/Editor/KemonoImport.cs`: Generic avatar 생성/공유, 88클립 임포트·반복 설정, Resources AnimatorController 생성, URP Lit 머티리얼 생성·리맵, 도구·새 시설 임포트. ManualWell은 원본 Fantasy Village 텍스처 사용. **Run() 미실행.**
- `.meta`는 열린 에디터의 자동 반입 과정에서 일부 생성됨. 수동 작성하지 않음.

## 적용 대기 코드
`docs/handoff-kemono/KEMONO-UNITY-1024.patch` — 기존 파일 3개만 수정하는 패치. `git apply --check --whitespace=error` 통과. 아직 적용하지 않음.

- AntVisual: 논리 StateName 유지. 채집 자원별 도구/클립, 건설 망치, 전투 검. 같은 논리 상태에서도 실제 작업 클립이 달라지면 전환. 동일 도구 반복 생성 방지. 사망 복제본의 손 도구 정리. QuirkyAnt fallback 유지.
- Corpse: 야생동물 외 복원 시 KemonoModel 사용. 기존 Death 마지막 프레임 고정 유지. 야생동물 분기는 기존 유지. 외형 선택 시드는 저장하지 않고 복원 시 0을 사용(기존 저장 포맷은 변경하지 않음).
- FacilityVisual: Models 표에 16행만 추가. 위치·회전·스케일 로직 변경 없음.
- Animator: `Idle_A`, `Hit` 호환 상태 추가. 원래 `Sit` 클립 상태는 보존하고 논리 Sit은 Downed/Captive로 매핑. 실제 `Base Layer.Attack`의 motion은 SwordAttack으로 연결하여 기존 검사와 호환. 컨트롤러 생성 자체는 미확인.

| BuildingKind | Resources/Facilities ID |
|---|---|
| OilDrill | CC_OilDerrick |
| OilRefinery | CC_Refinery |
| FluidTank | CC_LiquidTank |
| FluidPipe | CC_PipeL_Straight |
| OilDeposit | CC_Deposit_Oil_Normal |
| FuelCharger | CC_FillingStation |
| ManualWell | CC_ManualWell (ThirdParty Well 사본) |
| CrashWreck / School / CitizenClinic / CivicPublicService | 각각 CC_동일이름 |
| PlanterBed / WaterSource / WaterPump / SteamGenerator | 각각 CC_동일이름 |
| StarterMushroomBed | 기존 CC_MushroomFarm |

## 검증 결과
- **PASS 정적 컴파일**: Unity 6000.5.8f1 내장 DotNetSdk Roslyn + 현재 Bee rsp를 사용. Temp의 적용 대기 파일 3개로 소스 경로만 교체하고, 새 KemonoModel을 포함한 전체 Assembly-CSharp 컴파일 exit 0. 해당 참조 DLL과 새 KemonoImport 포함 Assembly-CSharp-Editor 컴파일 exit 0. 기존 obsolete/비교 경고 존재, 컴파일 오류 0.
- 로그: `Temp/kemono-pending/game-compile.log`, `Temp/kemono-pending/editor-compile.log`. rsp·임시 DLL은 같은 Temp 디렉터리. **Unity 공식 recompile 결과가 아니다.**
- **PASS 정적 반입 검사**: `docs/handoff-kemono/KemonoAssetChecks.py`. 복사 FBX 175개 원본 SHA-256 일치, 88 고유 클립, production manifest의 반복 25개 일치, 도구 리소스, 16개 매핑·Attack 호환 코드 확인.
- **PASS 패치 적용 사전 검사**. 원본 AntVisual의 혼합 LF/CRLF를 보존한 패치다. 임의로 줄바꿈을 정규화하면 적용 검사에 실패할 수 있다.
- Graphify AST update 완료: 11418 nodes / 24683 edges (최종 update exit 0). 커뮤니티 변경으로 기존 라벨 일부가 hub 이름으로 대체되었다는 안내가 있음. 코드 그래프 갱신과 별도로 문서 의미 추출은 실행하지 않음.
- **NOT RUN**: KemonoImport.Run, 공식 CLI recompile/recompile_status, avatar 유효성·애니메이션 바인딩·발 접지·도구 크기/회전·URP 색상 실제 확인, 새 게임 Play, idle/walk/run/gather/build/attack/death/corpse/facility 화면 확인.
- **NOT RUN**: BetaChecks, AntWorkVisualChecks, FacilityVisualChecks. 통과로 보고하지 않는다.
- **스크린샷 경로: 없음**. Unity 제어와 캡처를 실행하지 못했다. 일반 화면이나 이전 캡처로 대체하지 않았다.

## 재개 절차
1. 위 CLI 실행 및 기존 담당 소스 파일의 접근 제한을 해소한 실행 환경에서 재개. 현재 에디터가 열린 상태는 유지한다.
2. Claude 잠금이 있으면 30초마다 확인하며 기다린다. 없을 때만 `Codex <시각>`으로 잠금을 잡고, 각 Unity 단계 종료 시 본인 잠금만 삭제한다. 실제 Play/검사 중에는 잠금을 유지하며 다른 검사를 병렬 실행하지 않는다.
3. 패치를 다시 사전 검사하고 적용한다: `git apply --check --whitespace=error docs/handoff-kemono/KEMONO-UNITY-1024.patch` → `git apply docs/handoff-kemono/KEMONO-UNITY-1024.patch`. 최신 담당 소스가 바뀌었다면 필요한 부분만 재대조한다. 원래 docs/handoff-kemono/AntVisual.cs 초안을 그대로 덮어쓰지 않는다.
4. 공식 CLI `cmd set_autotick --enable true`, `cmd recompile` → `cmd recompile_status` 오류 0 확인. `cmd eval --code "return AntColony.EditorTools.KemonoImport.Run();"` 실행 후 컨트롤러·avatar·URP 머티리얼 확인, 다시 컴파일 상태 확인. 임포터 오류가 나면 해당 원인을 먼저 수정한다.
5. 새 Play에서 SaveStorage.RootOverride를 임시 디렉터리로 지정하고 NewGame(Small) 실행. 검사 종료 후 RootOverride·설정을 복원한다. 실제 저장을 건드리지 않는다. Stage7UIChecks 패턴 참고. 캐릭터 이동/채집/건설/공격/사망 및 복원된 시체, 신규 시설 16종을 실제로 확인한다. 공식 CLI screenshot으로 각 장면을 캡처하고 이미지도 직접 확인한다.
6. 기존 FacilityVisualChecks는 mapped == 104를 하드코딩한다. 새 매핑은 120개이므로 담당자와 협의해 기대 개수만 120으로 갱신해야 한다. 검사 파일은 이번 소유 범위에 없어서 수정하지 않았다. 다른 검사 실패도 실제 결과를 보고 분리한다.
7. 공식 CLI가 PATH에 있는 bash에서 `CHECK_POLLS=90 bash AgentScripts/tools/run_checks.sh BetaChecks AntWorkVisualChecks FacilityVisualChecks`를 순차 실행한다. 이 도우미는 자체 잠금 기능이 없으므로 실행 전체를 공유 잠금으로 보호한다. AntWorkVisualChecks는 자체 새 게임/RootOverride를 만들지 않으므로 격리된 시작 상태를 보장한다. editor_stop 후에도 SessionState 등을 통한 테스트용 RootOverride 적용이 필요할 수 있으니 실제 저장 경로를 확인한다.
8. 사용자 요청의 완료 조건은 **공식 recompile 오류 0 + 위 세 검사 통과 + Play/스크린샷 직접 확인**이다. 아직 충족하지 않았다. 검증 후 이 문서와 보드 상태를 갱신한다. 커밋은 하지 않는다.

## 남은 위험
- Generic avatar 경로·본 계층과 88개 실제 클립 바인딩은 Unity에서 아직 확인하지 않았다.
- 손 소켓에 부모 스케일이 적용되므로 도구 비율과 방향을 Play에서 확인해야 한다.
- 새 시설은 기존 footprint 정규화를 그대로 사용한다. kit front(-Y)의 Unity 변환 방향과 수동 우물 텍스처/배율은 화면 확인이 필요하다.
- 기존 테스트의 상태명 호환을 코드로 준비했지만, 실제 Animator 평가 및 최신 게임 규칙/UI 변경과의 회귀 통과 여부는 미확인이다.
- 완료 카카오톡 도구가 현재 세션에 없어 발송하지 못함. 작업 자체도 미완료 상태다.

