# 프로젝트 로그

## 현재 상태 — 2026-11-01 SAVE(밤)
- 프로젝트 E:\Git\ant, develop(일반)/master(안정). 목표는 Notion 기획(스펙 문서) 전체 구현. 구조: Assets/_Project/Scripts(Core/Units/World/Buildings/Map/UI/Save/Boss), AgentScripts 검사, docs/IMPLEMENTATION_BOARD.md·GAMEFEEL_*/DECISIONS_*/HANDOFF-*/FUN_1101.md.
- **방향 전환(사용자 질문 "실제로 재밌는 게임이냐")**: 재미를 검증한 적 없음 — 검사는 규칙 단위, 실플레이 루프 검사(PlayableLoopChecks)는 10/12 이후 갱신 안 됨, 하루 게임성 부족점 ~150건. 이후 원칙: 새 시스템 추가보다 기존 시스템을 재미 장치(긴장·목표·서사·손맛)로 묶고, 실제 플레이로 판정.
- 이번 세션(2차·3차 라운드):
  - 2차 게임성: Codex Q1(전투·원정·보스 32건 중 21 구현 + P3 인계 한국어 8건), Q2(생활·흐름·기획 모순 60건 중 32 구현), Claude(UI·조작 9건 중 6: 장수 이름표 쌓기, 초가집 짚지붕색(FacilityVisual, 뒤집힘은 정상), 월드맵 이름표 겹침 숨김, 부대 지정 Ctrl+1~9/1~9(UI/ControlGroups.cs), 마침표=할 일 없는 장수). 문서 GAMEFEEL_1101_Q1/Q2/claude2.
  - 사용자 버그 "이동시키면 도착 때 쓰러지듯 눕는다": 원인=밤에 자던 장수가 asleep 상태로 걷다 도착 즉시 Sleep 0.1초 전환. 수정=CommandMove→WakeForDuty, 이동 중엔 잠들지 않음(CommanderSleep), 눕기/앉기 0.6초 전환(AntVisual.Play). 검사 Review1101Claude2Checks 4/4.
  - Codex R1 버그 리뷰 8건 반영(날개 장수 이동 취소, 수술 매 프레임 연산, 월드맵 라벨 LINQ, ControlGroups 정리, 보스B 열 피해 안내 등). docs/REVIEW_1101_R1.md.
  - 3차 재미 구현(docs/FUN_1101.md): ①사건 감독 World/Storyteller.cs(위태/보통/번성 → 습격 간격·위기 확률 조절, 위기 뒤 180초 휴식, 첫날 밤 포식자 유예) ②목표 사슬 Codex S2 UI/GoalChain*.cs(현재 목표 1개+진행도+해금, 달성 토스트·4초 강조, 막힘 안내, 저장) ③장수 서사 Codex S3 UI/SpeechBubbles.cs·CampaignHistory.Stories(말풍선·이야기 5줄·동료 부상/사망 반응) ④손맛 UI/Juice.cs(완공 튀김+먼지, 입고 "+N 자원", 피격 움찔).
- 검증(Unity Play, 포커스 없이 runInBackground): Claude2 4·Q1 86·Q2 135·R1 3·S2 59·S3 17·Story 9·NightThreat 18·P1 21584·P2 181·P3 285·UndergroundReach 8/8 통과. 화면: 이름표 쌓기·초가집 렌더·목표 HUD·"+10 식량" 확인. 말풍선·달성 연출·완공 먼지는 실제 사건 화면 미확인. 부대 지정 키 입력 미확인.
- 미해결/다음: **사용자 20분 플레이 5문항(5분 내 할 일 파악/긴장 순간/지루해 배속 올린 순간/짜증/다시 할까) 받고 상위 3개 수정**. PlayableLoopChecks 실패(storage charges once — 10/12 건설비 시공 시작 지불 규칙 미반영). 탐사→채굴→배관 장시간 실행 미실행. 효과음 없음(오디오 자산). 기획 수치 다수 '추천안 자동 선택(사용자 미확인)'.
- 환경 메모: 포커스 없이 Unity 조작 가능 — EnterPlaymode 후 QueuePlayerLoopUpdate 폴링(~20초), Play 중 Application.runInBackground=true. scratchpad(4cdf8dc9) reload.sh(Play 종료→새로고침·재컴파일→Play), runcheck_nf.sh(오류 즉시 출력). Play 중 코드 바뀌면 반드시 reload(예전 코드로 검사 돈 사례). offc.sh 오프라인 컴파일(9cd1f401 scratchpad). Codex worktree는 원본 미커밋 상태 복사 후 시작, 병합은 baseline 사본 기준 3-way(tr -d '\r'). SelectionManager.AddToSelection은 검사들이 private 리플렉션으로 호출 → public 래퍼 Add 사용.
- SAVE: 46cdf45 develop push. Notion 일지 2026-11-01 페이지에 11~18 추가(아이콘 😐) https://app.notion.com/p/40bc4a0ecd31813c80e6f87500308849, Trouble Shooting 2026-11-01 https://app.notion.com/p/40bc4a0ecd318142b5b1cf89f7e6725c(넘어짐·포커스). 기획 반영: 맵·진행·랜덤 이벤트(사건 감독 교체), UI·HUD(부대 지정·목표 사슬·말풍선·손맛·이름표), 장수·유닛(수면 중 이동·서사), 구현 현황 점검(Q1/Q2 결정).
- 보존: prefab/.prefab.meta 커밋 금지. 커밋 제외: 재질/URP/씬/ProjectSettings/graphify/art/Assets 하위 외부 폴더. 이전 log 원문 work/save-20261101b/log-before-save.md.
