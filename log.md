# 프로젝트 로그

## 현재 상태 — 2026-09-30 SAVE 진행 중
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 개발 develop, 안정 master.
- 주요 경로: Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}, AgentScripts(회귀 검사), Assets/Scenes/AntColony.unity.
- SAVE 범위: Claude Code의 생활 4~7단계(식사·오락·야간 위협·바이옴)와 관련 검사 수정, Codex의 Stage4Checks 수정. 기존 미커밋 작업을 보존한다.
- Claude Code 전달 결과: Meal 347, Joy 45, NightThreat 16, Biome 22 통과. Fishing/DayNight/Workforce/AutonomousDuty/SaveRoundtrip/HudV2/Stage1/Commander/EnemyColonyEconomy 통과 보고. Fishing은 첫 어획 뒤 반납하도록 검사 수정. 휴게실 유지.
- Codex 직접 검증: Stage4Checks 130개 통과. 장수 없는 밭 성장 정지·작업 장수 배치·가뭄 작업률·무간호 치료 속도·감염 치료 저장 기대값 수정. 게임 로직 변경 없음. 기존 obsolete API 경고 1건. Play 종료 완료.
- Graphify 갱신: 5083 nodes, 10221 edges. 그래프는 커밋 제외.
- 시체·치우기: Notion 기획과 사망 경로 조사만 완료, 코드 미구현. 청소 시간/장소/소멸 규칙은 제안만 했으며 확정하지 않았다. 장의사·동족 포식·결벽 후속.

## SAVE 진행 결과
- 이전 log 요약 changelog 이관·양쪽 코드 범위 검토·README 최신화 완료. Meal 347/Joy 45/NightThreat 16/Biome 22/SaveRoundtrip 42 재검증 및 기존 Stage4 130: 직접 확인 총 602개 통과.
- 실제 식사/오락 상세·바이옴 HUD·야간 경고 캡처 2종 직접 확인 및 Notion 첨부 완료. 일지: https://app.notion.com/p/3eac4a0ecd31814e9835d4ef35fd577b . 원본 .unity/save-2026-09-30/{life-detail,night-detail}.png. 공식 CLI run_script/ScreenCapture 사용. Play 종료 완료.
- 검증한 코드·검사·SAVE 캡처 도구·문서를 develop에 커밋/push 진행. master 변경 안 함. 카카오톡 도구 미연결로 미전송.
- 사용자 컴퓨터 종료 요청: 2026-09-30 00:45 KST부터 1시간 유효. SAVE 후 종료, 새로운 작업 요청 시 취소.

## 다음 세션·주의
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd
- Notion 일지 상위: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. 명령 run_script로 검사는 하나씩 실행.
- .prefab/.prefab.meta와 기존 에셋·복구·그래프·디자인 작업은 커밋 제외. SAVE 외 일지·캡처·커밋/push 금지.
- 검증 전 구현 완료를 단정하지 않음. 기존 실패/미실행 InvasionChecks/RaidChecks/SceneInvasionChecks/AirborneChecks 및 전체 Player 빌드는 별도.
