# D1 인계 — B3 불러오기 정지 조사

## 결과

- 정적 분석으로 B3 첫 저장 왕복에 포함된 런타임 적 복원을 중단시키는 **Awake 이전 NavMeshAgent null 접근**을 확인하고 수정했다.
- **관측된 약 30초 전체의 실제 소요 원인은 확정하지 못했다.** Unity 실행·프로파일링 없이 이 결함을 30초 메인 스레드 정지의 유일한 원인으로 단정하지 않는다. 수정 후 Play 회귀 실행과 시간 측정은 남아 있다.
- 수정 범위는 D1 worktree뿐이다. 원본 프로젝트 쓰기, 커밋, SAVE, 캡처, Notion 기록은 하지 않았다.

## 호출 경로와 결함

1. `ImplB3Checks.Main`은 비활성 Capsule에 WildMonster를 붙이고 활성화한 `B3 Intruder`를 만든다. 첫 `Roundtrip`은 이동·건설·이 적을 함께 저장한다.
2. `SaveSystem.TryLoad`는 파일 검증 후 현재 상태를 Rollback/복구 파일에 보존하고 `Reload`에서 Busy=true, timeScale=0, 씬 재로드를 수행한다.
3. `GameBootstrap.Loaded`가 본거지 지형/NavMesh를 생성하고, `WorldMapManager.Start`가 원정 거점/NavMesh를 생성한다. `FinishLoad`가 카탈로그를 초기화한 뒤 `SaveSnapshot.Restore`를 순회한다.
4. 복원 마지막 부분의 `SaveRuntime.RestoreMonsters`는 카탈로그에 없는 일반 런타임 적을 `SetActive(false)` → `AddComponent<WildMonster>()` 순서로 만든다. 곧바로 `m.Agent.enabled = false`를 실행한다.
5. 기존 `WildMonster.Agent`는 private agent 필드만 반환했다. 비활성 GameObject의 Awake는 아직 실행되지 않았으므로 이 필드는 null이다. RequireComponent로 실제 NavMeshAgent는 존재해도 필드에는 연결되지 않았다. 따라서 이 접근에서 NullReferenceException이 발생한다.
6. `FinishLoad`는 복원 예외를 잡아 이전 상태로 씬을 다시 로드한다. B3의 이전 상태에도 같은 Intruder가 있으므로 복구 역시 같은 결함을 만난다. 실패·복구 재로드는 불필요한 지형/원정지 재생성을 추가한다. 예외 처리 자체는 유한하며 무한 재시도라고 볼 근거는 없다.

## 30초와 다른 후보의 구분

- `ImplB3Checks.Ready`의 30초는 Busy가 풀리기를 기다리는 **실시간 제한**이다. 반복마다 `await Task.Delay(20)`을 사용한다. 메인 스레드를 붙잡는 Sleep/Wait/Result 호출이 아니다. `RunChecks.cs`도 반환 Task를 await한다. 제한 시간을 늘리거나 검사를 완화하지 않았다.
- `SaveSnapshot.RestoreColony`의 확장 while은 `RestoreExpansion`이 배열 길이를 매번 1 증가시키거나 null 반환으로 예외를 내므로 정상 경로에서 진행한다.
- 소굴 노드 검색·수송 승무원 검색·건설 IndexOf 등 제곱 비용 후보는 있지만 B3의 작은 fixture에서 30초를 설명한다는 근거가 없다. 건드리지 않았다.
- 본거지와 각 원정지의 동기 NavMesh 빌드는 씬 재생성 경로에 존재한다. 엔티티 복원마다 재빌드하는 코드는 찾지 못했다. 해당 빌드의 실제 시간과 반복 검사 메모리 영향은 오프라인에서 측정하지 못했다.

## 수정 파일

- `Assets/_Project/Scripts/World/WildMonster.cs`: Agent getter가 캐시가 없으면 기존 NavMeshAgent를 가져와 캐시한다. 활성화·게임 명령 재실행 없이 Awake 전 로드 접근을 지원한다. 기존 Awake 및 복원 순서는 유지한다.
- `AgentScripts/ImplB3Checks.cs`: 비활성 상태의 Agent 접근과, Awake/OnEnable 없이 죽은 런타임 적 상태를 복원하는 회귀 검사 2건을 추가했다. internal RestoreRuntime 호출은 리플렉션을 사용한다. 기존 첫 왕복은 살아 있는 Intruder의 체력·목표·진영 복원을 계속 검사한다.
- `docs/IMPLEMENTATION_BOARD.md`: D1 담당·수정 파일·검증 상태 표시.
- `docs/IMPL-D1-HANDOFF.md`: 이 인계.
- Graphify update는 exit 0(9,278 노드/21,234 간선)으로 완료했다. 최소 diff를 위해 갱신 출력은 `Temp/off/graphify-update/graphify-out`에 보관하고 원래 graphify-out 내용은 복원했다. 캐시·백업 신규 생성 파일도 정리했다.

## 검증

- 지정 Unity 6000.5.8f1 내장 dotnet/Roslyn과 원본 Bee `1900b0aE.dag/Assembly-CSharp.rsp`를 사용했다.
- 기존 소스 항목과 out/refout/analyzer/generatedfilesout을 제거하고 Library 참조를 원본 절대 경로로 변환했다. D1의 Assembly-CSharp 대상 소스 327개를 다시 열거했다. Editor 및 별도 선행 어셈블리 영역은 제외했다(D1에는 asmdef/asmref 없음).
- D1에서 누락된 git 제외 서드파티 `Assets/Planet Earth Free/SpinFree.cs` 때문에 첫 컴파일은 CS0246 1건으로 실패했다. 원본 파일을 읽어 **수정 없이 D1 `Temp/off/SpinFree.cs`에 복사**해 검증 입력에 추가했다. 가짜 타입이나 스텁을 만들지 않았다.
- 게임: `Temp/off/a.rsp` → `Temp/off/Assembly-CSharp.dll`, **exit 0 / 오류 0**. 출력 `Temp/off/compile.txt`. 기존 obsolete 경고 등은 남아 있다.
- B3: `Temp/off/check.rsp` → `Temp/off/ImplB3Checks.dll`, 새 게임 DLL을 참조하는 **별도 어셈블리 컴파일 exit 0 / 오류 0**. 출력 `Temp/off/check-compile.txt`. 런타임 eval의 internal 접근 제한을 컴파일로 확인했다.
- Play 실행·추가 회귀 검사 실행·실제 save/load 성공·성능 개선은 **미검증**이다.

## 다음 실행에서 확인할 것

1. Unity에서 ImplB3Checks를 실행한다. 추가한 비활성 적 검사 2건과 첫 왕복의 Intruder 검사 통과를 확인한다.
2. Busy 대기가 여전히 30초를 초과하면 마지막 B3 phase와 예외 스택, 씬 로드/본거지 생성/원정지 생성/Restore의 소요 시간을 함께 확인한다. 이 패치를 실제 정지 시간 해결로 기록하는 것은 그 이후다.
