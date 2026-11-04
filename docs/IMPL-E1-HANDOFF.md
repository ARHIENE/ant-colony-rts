# E1 — D04 진형 시스템

- 기준: `DECISIONS_1025.md` D04 Codex A, `DECISIONS_1025_codex.md` D04 원문, Notion [전투·보스](https://app.notion.com/p/3d0c4a0ecd31818a90e5d1ffac4ca4ec)의 진형 표(2026-10-25 읽기 확인).
- 작업 브랜치: `codex/e1-formation`. UI·철수/파손 로직·prefab 수정 없음. Unity 실행·캡처·SAVE·push 없음.

## 구현

- 저장 번호 고정: 평진 0, 어린·봉시·방원·추행·안행·학익·장사 1~7, 횡대·종대·쐐기·산개·엄호·화력집중 8~13.
- 삼국지풍 수치는 Notion 표 그대로. 현대식 수치는 Codex A 그대로. 진형 수치·조건·레기온의 유리한 폭 ×1.5·방향 경계·최대 75% 피해 감소·지휘한도 공식은 `FormationRules`에 모았다.
- 횡대·종대·쐐기 각각 증기 400/식량100/기본재료100/Special10, 산개·엄호·화력집중 각각 석유 800/150/150/20. 독립 연구이며 추가 선행 연구 없음. `ScienceTechnology` 끝에만 추가하고 `CampaignResearch.Technologies`의 enum 인덱스 대응 유지.
- 장수별 영구 적성: 지휘 0/10/20에서 미보유 삼국지풍 중 하나씩 획득. 지휘 감소·재도달로 중복 지급하지 않는다. 레기온은 삼국지풍 전부 사용하며 현대식은 연구 필요. 기존 특성 enum 끝에 `Legion`을 추가했다(군세 계승은 이 작업 범위 밖).
- 기술·장비 기초 능력에 일반 가산과 기초×진형 보정을 더한 뒤 기존 건강 등 상태 배율 적용. 공격/방어/이동/사거리/공격 주기 연결. 받는 피해는 방어 이후 진형 보정 1회 적용. 유한 내구 갑옷의 실제 내구 소모는 기존 처리 유지.
- 횡대: 마지막 유효 이동/공격 방향 기준 측후방 >90°. 쐐기: 지정 공격 대상과 전방 ≤45°. 엄호: 공격 대상 없는 직접 이동 중에만 원거리 감소. 산개: 범위 피해 감소. 출처 없는 피해는 방향 보정 없음.
- 화력집중: 일반 공격 실제 적중 뒤 1회 누적, 다음 타격부터 +5%씩 최대 +20%. 강타 시전 타격은 누적에서 제외. 대상 변경·3초 무적중 시 해제. 범위 무기의 일반 공격도 지정 대상 기준 한 번만 누적.
- 종대의 월드 속도는 도보 편성에만 적용. 차량/항공 시간은 기존 값. 이동 중 남은 시간 저장 유지.
- 지휘한도: `max(1,floor((10+2×지휘+장비 정수)×건강×레기온 배율))`. 문서 예시 지휘7/장비3/건강0.8/레기온은 32. **문서에 별도 초과 감쇠식은 없다. 초과 병력은 보존하고 신규 편성·증원을 막는다.**

## 저장

- 기존 저장 버전 유지. `CommanderPersonalState.formation`에 선택·적성·지급 횟수·전방·연속 적중 횟수/남은 경과 시간을 저장한다. 필드 없음/null이면 평진.
- 화력집중 대상은 실행 중 EntityId로 비교하고 저장 시 기존 `SaveReferences` 키로 바꾼다. 모든 건물/몬스터 등록 뒤 캡처하고, 모든 대상/명령 복원 뒤 다시 연결한다. 손상된 대상 참조는 기존 사전 검증에서 거부한다.
- 개인 상태 복사 경로(포로·외교 등)는 기존 JSON을 사용하며 선택/적성을 보존한다. 새 개체로 전환돼 이전 적중 대상이 없으면 연속 적중만 초기화한다.

## 자동 선택

- **진형 수치 자동 선택 없음:** 삼국지풍 7종의 확정 수치를 Notion에서 확인했고 현대식은 확정 Codex A를 사용했다.
- 장사진의 산·숲 판정은 기존 로컬 바이옴 `Forest`와 고산·지상 동굴 `Cave`를 사용했다. 수송 중 현지 판정은 `Transport.Site` 또는 주둔 거점의 기존 바이옴 매핑을 따른다. 별도 산/숲 지형 셀 분류를 새로 만들지 않았다.
- 도보 편성 속도는 동행 장수 중 가장 느린 진형 보정으로 맞췄다. 종대와 평진이 섞이면 전체 편성은 기본 속도, 전원 종대면 +20%(레기온 +30%). 새 수치는 추가하지 않았다.
- 기존 `rageRemaining`은 친구 복수의 +30% 상태이며 역린 전용 상태가 아니다. 이 기존 기능은 보존했다. 역린 분노/탈진 구현 시 가산 계산 뒤 ×3/×0.5를 넣어야 한다. `FormationRules.Stat(..., health, state)`의 순서를 검사에 포함했으며 역린 발동·탈진 시스템 자체를 구현한 것으로 보지 않는다.

## 연결 요청 — Claude UI

UI 파일은 수정하지 않았다. 징집소 출전 편성의 장수별 드롭다운에 다음 API를 연결한다.

1. 후보: `AntColony.Units.FormationRules.All`, 표시: `FormationRules.Name(f)`.
2. 선택값: `commander.SelectedFormation`. 변경 가능 여부: `commander.CanSelectFormation`.
3. 해금/적성 사유: `commander.FormationBlockReason(f)` — 빈 문자열이면 사용 가능. 선택 불가 항목은 비활성/사유 표시.
4. 선택 확정: `commander.TrySelectFormation(f)`의 반환값 확인. **기존 `ConscriptionPost.TryDeploy(commanders, troops)` 호출 전에 적용**한다. 출전 후에는 변경 거부. 출전 확정 시 연구/적성을 다시 검증한다.
5. 적성 목록: `commander.FormationAffinities`(읽을 때 미지급 구간을 반영). 레기온 여부: `commander.HasLegion`.
6. 현재 상황의 수치: `commander.CurrentFormationModifiers`, `EffectiveAttackRange`, `EffectiveAttackInterval`, `FormationWorldMoveMultiplier`, `CommandLimit`.
7. 선택 전 정보: `FormationRules.Modifiers(f, legion, ranged, forestOrMountain, front, withdrawing, hits)`의 값은 증감 비율이다. 공격·방어·이동·사거리·공속·도보 이동·원거리 감소·범위 감소·측후방 증가를 역할에 맞게 표시한다. 조건부 수치는 조건(산/숲·전방·철수·적중 횟수)을 함께 적는다.

연구 화면은 기존 `CampaignResearch.Technologies` 등록 목록을 사용한다. UI에서 여섯 연구를 별도 하드코딩할 필요는 없다.

## 변경 파일

경로 접두사: `Assets/_Project/Scripts/`.

- 신규: `Units/FormationRules.cs`, `Units/CommanderFormation.cs`, `Core/FormationResearch.cs`, `Core/FormationDamage.cs` 및 각각의 `.meta`.
- 능력/명령 연결: `Units/CommanderAnt.cs`, `CommanderPersonal.cs`, `CommanderPersonalState.cs`, `SoldierAnt.cs`, `CommanderDeparture.cs`.
- 일반/범위 피해 연결: `Units/CommanderBossEquipment.cs`, `CommanderAdvancedSkills.cs`, `Boss/AoE/BossCircleAoE.cs`, `BossConeAoE.cs`, `BossLineAoE.cs`, `Buildings/AcidTower.cs`, `AreaAcidTower.cs`, `MineField.cs`, `TrapPit.cs`, `World/WildMonster.cs`.
- 등록/편성/저장: `Core/CampaignResearch.cs`, `Core/TraitRules.cs`, `Units/CommanderTraits.cs`, `Buildings/ConscriptionPost.cs`, `Save/SaveSnapshot.cs`, `Save/SaveOrderValidation.cs`.
- 도보 연결만: `World/ExpeditionTransport.cs`의 `TravelSeconds` 한 줄, `World/ExpeditionSupplies.cs`의 편성 최저 보정/권장 식량. E2 철수/파손 부분은 수정하지 않음.
- 검사·문서: `AgentScripts/ImplD04Checks.cs`, `docs/IMPLEMENTATION_BOARD.md`, `README.md`, 이 문서.

## 컴파일 결과·남은 검증

- Unity 6000.5.8f1 내장 .NET SDK 8.0.318 Roslyn + 원본 `1900b0aE.dag/Assembly-CSharp.rsp` 참조로 worktree 전체 게임 소스 컴파일: **exit 0, 오류 0**. 기존 obsolete 경고는 존재.
- 검사: `Assembly-CSharp-Editor.rsp`에서 원본 `Assembly-CSharp.ref.dll` 참조를 제거하고 위 출력 DLL로 교체, `ImplD04Checks.cs` 별도 컴파일: **exit 0, 오류 0**. top-level `RunChecks.cs` 미포함, internal 호출은 리플렉션, 게임 타입은 전체 이름 사용.
- worktree에서 빠진 기존 무시 자산 `Assets/Planet Earth Free/SpinFree.cs`는 원본 `E:/Git/ant/Assets/Planet Earth Free/SpinFree.cs`를 컴파일 입력으로만 추가했다. UI/외부 자산 소스 수정 없음.
- 재현 파일은 무시 경로 `Temp/d04-game.rsp`, `Temp/d04-checks.rsp`, `Temp/d04-game.log`, `Temp/d04-checks.log`. 일반 명령은 사용자 지정 `dotnet.exe exec .../csc.dll -nologo @Temp/d04-game.rsp` 및 `@Temp/d04-checks.rsp`.
- `git diff --check` 통과. Graphify AST 갱신 수행. 자동 생성 graphify 결과/캐시는 기존 커밋 제외 방침에 따라 구현 패치에서 제외한다. 마지막 AST 결과는 9820 nodes/22432 edges. 커뮤니티 라벨 수 변경 경고로 CLI exit 1이었으나 graph.json/보고서 갱신 완료 메시지를 확인했다. LLM 라벨 갱신은 하지 않았다.
- **Unity 실행·`ImplD04Checks.Main()` 실행·실제 저장 왕복·화면 확인은 미수행.** Claude에서 Play 진입 후 검사 실행 필요. 검사는 전체 14종 수치/레기온/방향 경계/3초/연구 비용·해금/적성 영구 유지/실제 적중/디스크 저장·로드/구저장 평진/초과 병력 보존을 포함한다.
- 카카오톡 MemoChat 도구는 이 세션에 제공되지 않아 완료 알림을 전송할 수 없다.

## 커밋 차단·대체 산출물

- `git add` 실행 결과 exit 128: `Unable to create 'E:/Git/ant/.git/worktrees/E1/index.lock': Permission denied`.
- 소스 작업 경로는 쓰기 가능하지만 실제 worktree Git 메타데이터는 허용된 쓰기 루트 밖이다. 승인 정책도 `never`여서 이 세션에서 staging/커밋할 수 없다. **커밋 없음, push 없음.**
- 변경사항은 현재 worktree에 그대로 있고, 신규 파일을 포함한 검토/이관용 패치 `docs/patches/IMPL-E1.patch`도 제공한다. Graphify 자동 생성물·Temp·prefab은 패치에 넣지 않는다.
- 예정 커밋 메시지:

```text
feat: implement D04 formation rules, research and persistence

Co-Authored-By: Codex
```

- Git 메타데이터 쓰기가 가능한 환경에서 이 문서의 변경 파일과 패치를 staging한 뒤 위 메시지로 커밋한다. 이미 구현된 이 worktree에 패치를 재적용하지 않는다. 다른 깨끗한 동일 기준 checkout으로 옮길 때만 `git apply docs/patches/IMPL-E1.patch`를 사용한다.

- 패치 검증: 신규 10개를 포함한 구현/문서 35개 파일. git apply --reverse --check docs/patches/IMPL-E1.patch exit 0으로 현재 변경사항과 일치함을 확인했다.
