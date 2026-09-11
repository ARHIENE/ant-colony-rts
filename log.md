# 프로젝트 로그

## 프로젝트와 작업 규칙
- 개미 소굴 RTS, Unity 6000.5.8f1 URP, `E:\Git\ant`, origin `github.com/ARHIENE/ant-colony-rts`.
- 실제 작업 `develop`, 안정 버전 `master`. 현재는 장수 구조 전환 중간 체크포인트이며 실행 가능한 완료본이 아니다.
- `Assets/Scripts/`: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 `Assets/Scenes/AntColony.unity`, 실행 검사·설정 `AgentScripts/`.
- `.prefab`/`.prefab.meta`, `Assets/_TeamImport/`는 커밋 금지. Ponytail full. Unity 공식 CLI/Pipeline 우선.
- 한쪽 한도 소진 시 현재 상태 SAVE 후 중단. 컴퓨터 종료 요청 없음. 카카오톡 보내지 않음.

## 2026-09-11 한도 소진 SAVE — 미완료 체크포인트
- 사용자 요청: Codex가 종료됐으므로 Claude Code 정상 실행 여부 확인 후 승인한 장수 전환을 재개.
- 기존 Claude 프로세스와 세션 39641은 없었고 장수 파일도 미생성 상태였다. Claude Opus를 재시작해 파일 읽기·수정 실행을 확인했다.
- 재시작 Claude 세션: `011bfda1-3c65-48fc-a4f9-39454bf6873e`.
- Claude가 `You've hit your session limit · resets 1:10am (Asia/Seoul)`로 종료. 09-12 01:10 KST로 해석된다. 계정 전환/추가 개발 없이 현재 상태를 보존했다.

## 이번에 실제 남은 장수 변경 (검증 전)
- 신규 `Assets/Scripts/Core/AntPool.cs`: Free/Assigned/Reserved 합산 수량 풀, 배정/반환·전투손실·건설예약/해제 기반. 아직 씬·생산·장수에 연결되지 않음.
- `AntUnitBase.cs`: Data protected setter, 체력/피해 virtual, 공격/방어 연구 보너스 동적 조회 기반.
- `SoldierAnt.cs`: 역할 기반 비행 전환, 명령 virtual, 전투/비행 tick 분리 시작.
- **복구 완료**: `IAirborne.IsAirborne` 계약과 공중 판정을 연결해 CS0539를 해결했다. 6개 보직+null 판정 검사가 공식 CLI에서 통과했고 재시작 후 Pipeline ready를 확인했다.
- 기존 FlyingAnt와 새 SoldierAnt 비행 코드가 중복될 수 있다. 역할 변경 착륙/NavMesh 복귀도 검토 필요.
- 기존 테스트는 AntUnitBase의 삭제된 private attackDamage/armor를 리플렉션 참조한다. 새 모델에 맞춰 검사 수정 필요. 과거 검사 통과를 현재 코드 검증으로 주장하지 말 것.
- 장수 클래스/병력 배정 UI/여왕방 일반개미 생산/병영 생산 폐지/건설 차출/여왕방 낚시/유지비 전환/씬 연결/새 검사 모두 미완료.

## 직전까지 검증된 기능 (장수 변경 전)
- Food/Soil/Special 채집·반납·저장, 농사 성장/수확 표시, 낚시 연구·재충전, 실제 잔량 약탈.
- 소굴 건물 전멸 후 전리품 해금, 직접 공격과 어택무브.
- ColonyInvasion: 90초 후 최초 출현, 120초 간격, 2→6마리 증가, 동시 생존 상한12(임시).
- 침공 병력이 수비 개미와 교전하고 없으면 본진 건물 공격. 소굴 전멸/비활성 시 추가 출현 중단. 이미 출발한 병력은 유지.
- `InvasionChecks.cs`, `SceneInvasionChecks.cs`, 기존 `RegressionChecks.cs` 43개가 장수 변경 전에 통과. 당시 콘솔 오류0, Play 종료.

## 다음 재개 — 사용자 승인된 범위
1. 컴파일 차단은 복구됨. 남은 비행 코드 중복/역할 전환과 장수 마이그레이션을 이어서 확인. 기존 작업을 되돌리지 말 것.
2. 일반개미를 여왕방에서 식량으로 생산하는 수량 풀로 연결. 유지비는 미배치+배정+건설인력을 중복없이 계산.
3. 장수만 직접 조작. 일반개미 배정/회수, 허용 보직·관직별 지휘한도, 배정 수=병력 체력. 변경으로 회복·복제 금지.
4. 병영 생산을 제거하고 역할 연구 시설로 전환. 연구 완료/보직 변경 즉시 현존 부대에 강화 반영.
5. 건설 시 인력 예약, 완공/취소 시 정확히 한 번 복귀. 낚시 연구는 여왕방에서 글로벌 적용.
6. HUD/선택·명령과 실제 씬을 새 구조에 연결하고 실제 이동/채집/건설/피격/침공을 검증.
7. 번식·영입·포로·랜덤 이벤트는 이번 1차 전환 범위 밖. 병력0 이후 사망/포로 세부 규칙은 미정이며 임시 정책을 확정 기획처럼 기록하지 말 것.

## 기획과 관련 문서
- 기획 부모 `334c4a0ecd3180c4a796e5220302a0bd`, 장수 `3d8c4a0ecd318155a477fda677e21fd1`.
- 새 기획 원문과 하위15개 문서를 읽음. 일반개미=단일 자원, 직접조작 장수만, 병영=연구, 건설차출/복귀, 스킬 장수귀속으로 바뀜.
- 자원/유닛/건물/UI/전투/보스 등에는 구버전 참조가 남음. 명백한 충돌 문구를 기존 하위 페이지에서 교체해야 한다. 본문 경고만 추가해 구규칙을 공존시키지 말 것.
- 관련 문서 정리 재시도는 Claude 한도 응답으로 실행되지 않음. 이번 작업에서 노션 변경 없음.
- 기획 부모에 replace_content+allow_deleting_content 금지. 기존 개발일지는 당시 사실을 현재 구현으로 바꿔쓰지 말 것.

## SAVE 복구 상태
- 사용자 Notion OAuth 승인 완료. 새 Codex CLI 실행에서 Notion 실제 읽기·쓰기가 동작함. 현재 대화의 직접 도구 목록은 갱신되지 않았지만 서버가 없는 것은 아님.
- 개발 일지: https://app.notion.com/p/3d8c4a0ecd31819aa2fcf2fc64bf0020 (2026-09-11, 😓).
- Unity는 실제 에디터가 종료된 상태였고 재실행 시 CS0539로 Safe Mode 진입. 최소 인터페이스 수정 후 정상 종료/재실행해 컴파일 통과, 공식 CLI ready 및 AntColony 씬 열기 확인.
- AgentScripts/AirborneChecks.cs: 공식 unity command eval_file로 6개 보직과 null 판정 통과. 전체 마이그레이션 게임플레이 검증은 미완료.
- 캡처: Assets/.unity/save-2026-09-11-recovery.png. 공식 캡처 전용 명령 부재, 기존 bridge 포트6400 연결 거부로 공식 CLI eval의 SceneView 렌더를 사용. 현재 에디터 씬 복구 화면이며 실제 게임플레이 검증 화면이 아님.
- 노션 복구 결과 갱신 및 PNG 직접 업로드·본문 첨부 완료(136,405 bytes). SAVE 외부 단계 완료.

