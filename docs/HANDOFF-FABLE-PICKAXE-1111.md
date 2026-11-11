# Fable 인계: 곡괭이의 뾰족한 날 끝으로 실제 바위 타격

사용자 요청: 곡괭이 작업은 Codex가 중단하고 Fable이 맡는다. 사용자는 “곡괭이 제일 끝 뾰족한 부분이 부딪혀야 한다”고 명시했다. 손잡이가 손에 붙었다는 검사만으로 완료 처리하면 안 된다.

## 현재 상태와 재현
- 프로젝트 F:/UnityProjects/ant-colony-rts, develop, Unity 6000.5.8f1.
- 공식 CLI: `& 'F:\_tools\unity-official\unity-managed.ps1' ... --project-path F:\UnityProjects\ant-colony-rts`.
- 첫 장수에 원본 Meshy 메시/리그를 사용하는 시험 연결. 기존 Kemono 리타게팅으로 되돌리지 않는다. 머리/외형 조합의 전체 이전은 아직 미완료다.
- 실제 채굴 행동: AntVisual.MotionName=Tool_Mine, HeldTool=Tool_Pickaxe_Stone, native clip=Heavy_Hammer_Swing.
- 왼손이 머리 쪽 앞손, 오른손이 손잡이 아래쪽이다. FBX를 기본 회전으로 놓으면 머리는 -Z 쪽, 곡괭이 양 끝은 X 양쪽이다.
- 현재 수정은 손잡이 접점과 축 정렬까지만 맞췄다. 실제 화면에서 바위와 날 끝 사이에 간격이 남는다. 사용자는 타격면/타격점이 틀렸다고 재차 지적했다.
- `AgentScripts/Animation1110Game.cs` PrepareAndGather로 격리 새 게임의 실제 채굴을 재현한다(사용자 저장 슬롯 덮어쓰기 금지). Gather는 시작 이후 수면으로 바뀔 수 있으므로 반환 문자열의 actualGather/state도 확인해야 한다.
- `AgentScripts/Meshy1111NativeActions.cs` Actual은 실제 행동 재생, Preview는 시뮬레이션을 멈추고 모션만 반복, Phase(float)는 고정 시점 확인이다. Preview는 현재 실제 HeldTool을 사용하므로 채굴 상태부터 준비해야 한다.

## 관련 파일 / 협업 경계
- `Assets/_Project/Scripts/Units/MeshyNativeEquipment.cs`: 장비 방향·길이·파지. Fable의 곡괭이 수정 대상. 활/화살·다른 도구 처리와 빈손 전환 수정은 보존한다.
- `Assets/_Project/Scripts/Units/MeshyNativeMotionPlayer.cs`: 원본 재생, 수평 root motion 고정, 원본 모델 방향을 실제 타깃 방향에 맞춤. Codex가 다른 모션 검수 중이므로 수정 필요 시 구현 보드에 먼저 표시하고 조율한다.
- `Assets/_Project/Scripts/Units/WorkerAnt.GatherApproach.cs`: 실제 작업 접근 거리. 다른 채팅이 최근 지면 투영을 수정했다. 기존 NavMesh 접근 가능성·지면 투영을 보존하고 담당 범위를 확인한다.
- `Assets/_Project/Scripts/Units/MeshyNativeFingers.cs`: 손가락만 보정. 팔·손목 궤적과 뼈 길이는 변경하지 않는다.
- `Assets/Resources/MeshyNative/Runtime.asset`: 원본 FBX 참조. 불필요한 모션 재다운로드·유료 결제 없이 현재 보유 소스부터 사용한다.

## 꼭 보존할 최근 수정
- SetHandSpan이 held=null로 무효화하던 방식은 빈손 전환 시 NullReferenceException을 냈다. 별도 rebuild 플래그로 고쳤고 Lifecycle 검사 통과했다. 되돌리지 않는다.
- 원본 캐릭터 확대 배율이 장비에 중복 적용되지 않도록 장비 월드 크기를 분리했다.
- 작업 도구는 손 사이 거리에 맞춰 길이축(Y)만 늘리고 두께/머리 폭은 확대하지 않는다. 접점 계산은 item.lossyScale.y를 쓴다.
- 손잡이/손가락 검사 통과가 날 끝의 바위 타격 검증을 대신하지 않는다.

## 요청하는 수정 및 완료 기준
1. 실제 곡괭이 메시의 뾰족한 끝을 타격점으로 정한다. 손잡이 끝·머리 중앙·바운딩 박스 중심을 타격점으로 쓰지 않는다.
2. 실제 바위의 보이는 표면과 해당 날 끝의 한 주기 궤적을 비교한다. 들어 올림→내려침→날 끝 접촉→회수의 시점을 확인한다.
3. 도구 회전/오프셋과 필요하면 작업 접근 거리를 맞춘다. 몸 전체를 순간이동하거나 팔을 임의 사인 곡선으로 대체해 문제를 가리지 않는다. 불가피한 보정은 작은 범위로 하고 팔 길이·무릎·손가락을 검증한다.
4. 날 끝이 닿는 순간 손잡이는 두 손 안에 있고, 머리 중앙/손잡이가 먼저 바위를 관통하지 않아야 한다. 캐릭터 몸과 바위도 겹치면 안 된다.
5. 실제 게임의 채굴 명령으로 반복 재생하고, CLI로 타격점-표면 거리와 접촉 시점을 기록한 다음 Unity 화면에서 실제 접촉을 확인한다. 테스트 전용 프리뷰만 통과해서 완료 처리하지 않는다.
6. 돌과 광석 크기가 다른 최소 두 대상에서 확인한다. 이미 정상인 달리기·다른 무기·수확 동작은 회귀시키지 않는다.
7. 결과 보고에는 변경 파일, 실제 화면에서 확인한 동작, 남은 문제를 구분한다. 미확인 사항을 완료로 쓰지 않는다.

Unity는 공유 에디터이므로 `Temp/unity-lock.txt`와 `docs/IMPLEMENTATION_BOARD.md`를 먼저 확인한다. 현재 Codex가 다른 모션을 검사 중이다. 서로 Play/컴파일을 겹치지 않는다. SAVE/커밋/push는 이번 요청에 포함되지 않는다.

추가: Codex가 다른 모션 검수 중 NativeMotionPlayer의 수평 기준을 클립 첫 골반 위치에서 skin bindpose 골반 위치로 수정했다. 일부 클립의 시작Z=-1.76이 몸을 선택 원 밖으로 밀던 공통 문제이다. 곡괭이 날 끝/타격점 수정을 한 것은 아니다. 현재 곡괭이 접촉 실패 상태와 인계 범위는 그대로다.

**최신 사용자 지시: Claude 수정 전으로 원복 완료. 이 인계의 수정 작업은 중단 상태이며 Fable 패치를 다시 적용하지 않는다. 추가 작업은 새 사용자 지시 후 진행.**
