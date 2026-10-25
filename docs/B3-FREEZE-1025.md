# B3 LOAD 약 30초 정지 정적 진단 — 2026-10-25

- 범위: 현재 `develop` 작업 트리(미커밋 수정 포함). Unity 실행·컴파일·캡처 없이 소스만 확인. 이 문서 외 수정 없음.
- **결론: 30초를 만드는 명시적 대기는 검사 코드의 `Busy` 폴링이다. 현재 코드에서 그 조건이 계속 참이 되는 단일 실행 원인은 확정할 수 없다.** 과거의 구체적 로드 결함은 비활성 런타임 적의 `NavMeshAgent` 초기화 누락이며, 현재 작업 트리에는 이미 수정돼 있다. 이를 현재 정지의 미해결 원인으로 재보고하면 안 된다.

## 확인된 대기와 호출 순서

- `AgentScripts/ImplB3Checks.cs:46-49`: `deadline = DateTime.UtcNow.AddSeconds(30)` 후 `while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(20)`. 30초 뒤 `Busy`가 남으면 실패한다. 초기 진입에도 같은 대기가 있다(`:92-94`). **대기 위치 확신도: 높음.** `Task.Delay`는 비동기 대기이며 이 루프 자체가 메인 스레드를 30초 연속 점유하는 코드는 아니다.
- `Assets/_Project/Scripts/Save/SaveSystem.cs:39-49`: 읽기 → 파싱/검증 → `ForScene` → 현재 상태/복구 파일 보존 → `Reload`. `:67-73`에서 `Busy=true`, `timeScale=0`, `PendingLoad` 설정 후 `SceneManager.LoadScene("AntColony")` 호출.
- `Assets/_Project/Scripts/Core/GameBootstrap.cs:21-32`: `sceneLoaded` 구독 → 본거지 생성 → `FinishLoad` 시작. `:36`의 두 프레임 대기 뒤 `:49-63`에서 카탈로그 초기화와 저장 복원 순회. `Busy=false`는 `:78`에 도달해야 설정된다.
- `Assets/_Project/Scripts/Save/SaveSnapshot.cs:168,271`: 복원 자체의 양보 지점은 두 번의 `yield return null`. 호출된 명령 복원도 상태·참조·목적지만 복구한다(`Units/CommanderWork.cs:15-23`, `Units/WorkerAnt.cs:48-62`, `Units/SoldierAnt.cs:45-51`, `World/TransportRoute.cs:42-64`; 모두 `Assets/_Project/Scripts/` 기준).
- 이 경로에서 `WaitUntil`, `WaitForSeconds`, 30초 `Task.Delay`, 동기 `Task.Wait/Result`는 찾지 못했다. `timeScale=0`을 기다리는 조건도 없다. `yield return null`과 실시간 기반 `Task.Delay`만으로는 **timeScale 교착 가설을 지지하지 않는다**. Editor 자체가 일시정지됐거나 메인 스레드가 점유되면 재개가 늦을 수 있지만, 해당 상태가 발생했다는 실행 증거는 없다.
- 별도 30초 값: 자동 저장 실패 재시도 `SaveSystem.cs:18-22`는 로드 대기가 아니다. CLI 호출의 `--timeout 30`도 존재하지만(`AgentScripts/tools/run_checks.sh:19`), 검사 Task는 별도로 await하고 결과를 기록한다(`AgentScripts/RunChecks.cs:20-35`). CLI 제한과 B3 내부 제한은 구분해야 한다.

## 원인 후보 — 현재 코드 기준 우선순위

- **1. 로드 초기화 예외로 `Busy` 해제 누락 — 메커니즘 확신도 높음 / 이번 발생 원인 확신도 중간 이하(가설).**
  - `GameBootstrap.cs:29-32`의 `terrain.ApplyFromSession()`은 보호되지 않으며, 실패하면 `StartCoroutine`에 도달하지 않는다.
  - `FinishLoad`의 첫 두 프레임 뒤 초기화·`SaveCatalog.Initialize()`·`CrashLandingStart.Apply()`도 복원용 catch 밖이다(`:36-56`). 특히 `SaveCatalog.cs:33`은 `WorldMapManager.Instance.Sites`를 직접 사용한다. 이 구간에서 예외가 전파되면 `GameBootstrap.cs:78`의 해제를 건너뛰어 검사 폴링이 30초 제한까지 간다.
  - **어느 초기화가 실제 예외를 냈는지는 미확인.** `WorldMapManager.Start`의 예외만으로 `Busy` 고착을 단정하지 않는다. 반대로 `SaveSnapshot.Restore` 내부 예외는 `GameBootstrap.cs:60-76`에서 잡혀 복구 또는 세션 종료로 진행하므로, 모든 복원 예외가 곧 `Busy` 고착인 것은 아니다.
- **2. 씬 재생성의 동기 지형/NavMesh/모델 처리 — 동기 경로 확신도 높음 / 30초 소요 확신도 낮음(가설).**
  - `Map/HomeMapBuilder.cs:109-124`: 지형 생성 후 NavMesh 재구축. `:185,202-204`는 동기 소스 수집·`BuildNavMeshData`·등록이다.
  - `World/WorldMapManager.cs:34-41,62-68` → `World/WorldGeneration.cs:29-46`은 기본 33개 거점을 연속 생성한다. 각 거점의 `World/ExpeditionSite.cs:83-89`에서 동기 NavMesh 빌드를 수행한다. 작은 본거지 fixture라도 거점 수가 줄어드는 코드는 이 경로에 없다.
  - `World/WildMonster.cs:144` → `Units/AntVisual.cs:27-40`은 시각 모델을 붙이고 로드·생성한다. `Units/KemonoModel.cs:18-22`는 최초 리소스 로드를 캐시하지만 `:25-32`의 모델 생성은 별도다. 건물도 `Buildings/FacilityVisual.cs:56-65,73`에서 동기 리소스 로드·생성을 한다.
  - 프레임 전체가 멎으면 우선 이 후보를 측정해야 한다. 폴링의 deadline도 프레임 점유를 중단시키지 못하므로 실제 실패 시각은 30초를 넘길 수 있다. **빌드/로드 시간 수치는 정적 코드로 알 수 없다.**
- **3. 과거 런타임 적의 Awake 전 null 접근 — 과거 결함 확신도 높음 / 현재 재발 가능성 낮음.**
  - 첫 왕복 fixture는 `B3 Intruder`를 생성한다(`ImplB3Checks.cs:124-129`). 카탈로그에 없는 적은 `SaveRuntime.cs:67-73`에서 비활성 GameObject에 `WildMonster`를 붙인 직후 `m.Agent.enabled=false`로 접근한다.
  - 필드 `agent`는 원래 `WildMonster.Awake`에서 연결된다(`World/WildMonster.cs:34,130-132`). 이전 getter가 필드만 반환하던 결함은 현재 `:47-48`의 지연 조회로 수정됐다. 비활성 회귀 검사도 이미 추가돼 있다(`ImplB3Checks.cs:28-40,85`). 기존 인계 역시 30초 전체 원인은 미확정으로 명시한다(`docs/IMPL-D1-HANDOFF.md:5-6,14-23`).
  - 과거에는 해당 예외 후 같은 Intruder를 가진 롤백을 재로드할 수 있었다(`SaveSystem.cs:47-48`, `GameBootstrap.cs:68-74`). 롤백 참조를 재시도 전에 비우므로 **무한 재시도는 아니다**. 추가 씬 생성 비용 후보일 뿐 정확한 30초나 영구 `Busy`를 입증하지 못한다.

## 배제·제한

- `SaveSnapshot.RestoreColony`의 while(`SaveSnapshot.cs:139-140`)은 `World/EnemyColony.cs:151-160`에서 배열이 1 증가하거나 null 반환 시 예외로 종료된다. 정상 경로의 무진행 루프 근거 없음.
- `GameBootstrap.cs:57-64`의 `while(true)`도 복원 enumerator가 끝나거나 예외가 나면 종료한다. 복원 enumerator의 실제 yield는 위 두 지점이며 시간 조건 폴링이 아니다.
- 수송의 while은 `World/TransportRoute.cs:119-128`에서 커서 증가·중단·반환 중 하나로 진행한다. 로드가 호출하는 `RestoreProgress(:60-64)`는 이 Tick 루프를 호출하지 않는다.
- 검증은 동기 검사 후 실패를 반환한다(`SaveValidator.cs:11-30,268-269`, `SavePreflight.cs:219-251`). 저장 I/O도 동기 파일 연산이다(`SaveStorage.cs:32-56`). 큰 입력/느린 디스크 비용은 가능하지만 전용 30초 대기는 없고, `TryLoad`의 읽기·검증은 `Busy=true` 설정 전이다(`SaveSystem.cs:43-49,67`).

## 최소 수정 제안 — 적용하지 않음

- 과거 null 결함의 최소 수정은 아래 1줄이며 **현재 작업 트리에 이미 존재**한다. 재적용할 패치가 아니라 수정 전후 설명이다.

```diff
--- a/Assets/_Project/Scripts/World/WildMonster.cs
+++ b/Assets/_Project/Scripts/World/WildMonster.cs
@@ -48 +48 @@
-        public NavMeshAgent Agent => agent;
+        public NavMeshAgent Agent => agent != null ? agent : agent = GetComponent<NavMeshAgent>();
```

- 현재 잔여 정지는 원인 미확정이므로 시간 제한 증가·강제 `Busy=false`를 해결책으로 제시하지 않는다. 우선 복원 catch가 스택을 버리는 부분(`GameBootstrap.cs:61`)에 아래 **진단용 최소 변경**을 제안한다. 초기화 catch 밖의 예외는 Unity Console의 원래 스택도 함께 확인한다. 이 변경만으로 성능/고착이 해결되지는 않는다.

```diff
--- a/Assets/_Project/Scripts/Core/GameBootstrap.cs
+++ b/Assets/_Project/Scripts/Core/GameBootstrap.cs
@@ -61 +61 @@
-                    catch (Exception e) { error = e.Message; break; }
+                    catch (Exception e) { Debug.LogException(e); error = e.Message; break; }
```

## Unity에서 후속 검증할 방법 — 이번에는 미실행

- 새 Play 세션에서 `ImplB3Checks.Main()` 단독 실행. `:23`의 마지막 phase·elapsed·busy·scale 및 최초 예외 전체 스택을 보존한다. Editor Pause 상태도 확인한다. 실제 저장 경로 대신 기존 임시 `RootOverride`를 유지한다(`:86-89,221`).
- `B3 load finished`가 약 30초 뒤 `busy=true`이면 후보 1의 초기화 구간 예외와 `sceneLoaded` 진입/`FinishLoad` 시작 여부를 먼저 확인한다. `PendingLoad`가 비워지는 지점은 `GameBootstrap.cs:50-51`이다.
- Profiler Timeline에서 씬 로드, `HomeMapBuilder.Apply/RebuildNavMesh`, `WorldMapManager.Start/GenerateWorld`, `ExpeditionSite.Initialize`, `SaveSnapshot.Restore`를 나눠 시간을 측정한다. 프레임이 멎는지, UI는 계속 갱신되면서 Busy만 남는지 구분한다. 단계별 Stopwatch 계측이 필요하면 별도 승인 범위에서 추가한다.
- 기존 비활성 적 검사 2건과 첫 왕복의 적 체력/목표·이동·건설 검사를 확인한다(`ImplB3Checks.cs:28-40,130-143`). 롤백이 실제 발생했는지와 `Load failed`도 확인한다. 현재 Ready는 실패 후 롤백 성공도 정상 로드와 구분하지 못한다(`:49`; `GameBootstrap.cs:68-78`).
- 성공 기준: 모든 B3 assertion 통과, 실제 요청 상태 복원, 로드 예외·불필요한 롤백 없음, `Busy` 해제, 단계별 소요 시간 확인. 정적 진단만으로 해결/성능 개선 완료를 선언하지 않는다.
