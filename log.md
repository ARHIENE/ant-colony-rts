# 프로젝트 로그

## 현재 상태 — 2026-09-21
- 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 개발 develop, 안정 master.
- 주요 경로: Assets/Scripts/{Core,Data,Units,Buildings,World,Boss,UI,Map}, Assets/Scenes/AntColony.unity, AgentScripts/. 조작·구현 상태는 README.md.
- 검증된 구현: 30거점 월드맵·과학·동시 원정·편입/유기·현지 생산/주둔/회수·침공/상실/포로/탈출/재정복 구출·단일 거점 자동 수송·거점 난이도 보상 차등.
- 이전 검증: SettlementRewardChecks 50개, AnnexedSettlementChecks 51개, WorldMapChecks 636개, RegressionChecks 전체 통과. 이전 로그는 changelog.md에 날짜와 함께 요약 이관.

## 진행 중 작업 — 전체 UI (미완성)
- 사용자 요청: Notion 「게임 전체 UI 구성」 https://app.notion.com/p/3e2c4a0ecd31819eaba5ff850554216c 전체 구현. 넓은 수정 범위 확인 완료.
- 코드 작성은 사용자가 지정한 **Claude Opus 5**. 설치된 Claude Code 2.1.276에서 --model claude-opus-5 응답으로 실제 모델 확인. Codex가 불가능하다고 잘못 답했으나 기존 CLI 실행 경로를 찾아 정정함. 다른 모델로 임의 대체하지 않는다.
- 승인 범위: 메인 메뉴·새 게임(크기/난이도/시드)·설정·자동/수동 저장/불러오기·도감·관직순 장수 관리·일시정지·비차단 토스트·툴팁. 승리/패배 화면은 기획상 보류. 기분/부상/장비 게임플레이는 이번 UI 범위가 아님.
- 작성 중이던 내용: 옵션/세션/설정, JSON 저장 스키마·슬롯·파일 교체·검증 기반, 자원/장수/건물 생산·연구/거점·원정 복원 API, 난이도 배수, 지형 생성 연결. 메뉴·저장 통합·테스트는 아직 없음. 기본 슬롯(수동 3+자동 1), 자동저장 5분은 초안 값이며 확정 스펙으로 기록하지 않음.
- Opus 사용 한도 도달 응답: `You've hit your session limit · resets 2:10am (Asia/Seoul)` — 다음날 2026-09-22 02:10 KST 안내. 사용자 규칙에 따라 SAVE 후 종료. 코드 구현 완료가 아니다.

## 초안 보존과 재개
- **초안은 실행 프로젝트에서 분리했다.** `.unity/ui-opus-checkpoint-2026-09-21/files/`에 변경/신규/ignored 지형 코드 총 45개를 복사하고 원본과 SHA256 일치를 확인. 목록은 같은 폴더 manifest.json.
- 이번 초안으로 변경한 tracked 코드는 기존 검증된 인덱스 내용으로만 복귀했고, 이번 신규 코드도 위 백업 확인 후 실행 경로에서 분리. 기존 staged 작업은 보존. ignored TerrainGenerator 원본도 Opus 최초 Edit 응답의 originalFile로 복구했으며 사본은 TerrainGenerator.original.cs.txt에 남김.
- Claude 세션 ID: `9fb438fc-726a-40af-99da-0361e03f63a5`. 재개 시 백업과 현재 코드 차이를 먼저 검토하고 초안을 복원한 뒤 `claude --resume ... --model claude-opus-5`로 계속한다. 이후 작업이 있으면 파일을 무조건 덮어쓰지 말 것.
- `.unity/ui-opus-task.txt`: 승인 범위·기획·제약. `.unity/ui-review-notes.txt`: 실제 씬 기반 검토 사항. `.unity/ui-opus-output.jsonl`, `ui-opus-output-2.jsonl`: 실행 기록. `.unity/`는 git 제외이므로 초안은 로컬에만 보존되어 있다.

## 재개 시 해결할 문제
- 초안 컴파일 실패: `HomeMapBuilder.cs(86,49) CS0246 TerrainGenerator not found`. 실제 씬 루트는 imported TerrainGenerator이며 `.asmdef` 등 어셈블리 경계를 확인해야 함. Assets/_TeamImport는 커밋 금지이므로 해당 ignored 코드에만 추가한 API에 의존하는 설계를 저장소에서 재현 가능한 형태로 정리해야 한다.
- 맵 크기/시드가 실제 지형·본거지 위치·NavMesh와 일치하는지, 생성 순서와 재현성을 검증. ProjectSettings/EditorBuildSettings에는 현재 SampleScene만 있어 AntColony 재로드/빌드 경로 점검 필요.
- 저장 파일 ID/참조/수치 검증과 불러오기 실패 시 기존 게임 보존, 진행 데이터 누락 방지, 입력 차단/시간 복원/중복 UI 방지 필요. 미검증 초안을 완료 상태로 커밋하지 않는다.
- 공식 Unity CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. 현재 Editor 포트 7801, Play 정지, autotick 켜짐. Unity AI Assistant 서버 대화 갱신 오류는 기존 오류로 코드 오류와 구별한다.

## SAVE 결과
- log 먼저 기록 → changelog 요약 이관 → README에 UI 미완료 상태 표시. 기존 검증 변경만 develop 커밋/push 대상으로 유지.
- 오늘 Notion 일지 https://app.notion.com/p/3e2c4a0ecd318118bc0bfa3a36369418 기존 내용을 보존해 중단 기록을 이어 작성한다.
- 새 UI가 구현·실행되지 않아 이번 UI 기능 캡처는 불가. 오늘 일지의 기존 난이도 보상 캡처는 보존하며 새 UI 캡처로 오인시키지 않는다.
- 카카오톡 완료 알림 도구 미연결.
- 복귀 후 컴파일 검사·graphify 갱신·develop push 결과는 아래에 갱신한다.
