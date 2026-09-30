# 프로젝트 로그

## 현재 상태 — 2026-09-30 추가 SAVE 완료
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 일반 개발 develop, 안정 master.
- 주요 경로: Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}, AgentScripts(회귀 검사), Assets/Scenes/AntColony.unity.
- 이번 SAVE 범위: 시체·현장 치우기·동족 포식, 장의사/결벽 특성 효과, 작업표·대상 패널·우클릭·저장 v11 연결 및 관련 검사·README.
- 실제 사망만 시체 생성(회복 가능한 쓰러짐/포로 전환 제외), 현장 기본 5초 청소, 게임 시간 300초 소멸. 일반개미 병력 손실은 수량 묶음이며 청소는 전체, 포식은 1구당 5초/Food +10/포만 회복.
- 사냥 사체는 기존 Food 20 운반 보존, 우선 지정한 경우만 자동 청소. 시체와 자원 노드 중복 저장 방지. 담당 장수·청소/포식 진행도 복원.
- 핵심 신규 파일: World/Corpse.cs, Units/CommanderCorpseWork.cs, AgentScripts/CorpseChecks.cs. 새 작업 Cleaning, 저장 v1~v10은 v11로 이관.
- 이전 작업 전달 검증: Corpse 71/Workforce 79/Meal 347/SaveRoundtrip 42/DayNight 37, 총 576개. Notion 장수·유닛·메인 화면 기획 반영 보고.
- 이번 세션 직접 검증: CorpseChecks를 84개로 확장해 Play 모드 통과. 화면 좌표 기반 선택/우클릭/Alt 포식, 실제 업데이트 이동·포식 완료, 사냥 운반 버튼·자동 청소 제외, 청소 중 저장 복원 포함. OS 마우스 직접 조작/화면 외관은 당시 미검증.
- 검사 도구의 동명 타입 충돌을 프로젝트 타입 명시로 수정. 기본 30초 검사 반환 제한은 timeout_ms=120000으로 해결(최종 약 39초). 게임 로직 추가 수정 없음.
- Graphify 5157 nodes / 10443 edges로 갱신. Play 종료 확인. 콘솔에 Unity AI Assistant 서버 대화 갱신 오류가 있으나 컴파일 실패 없음.

## SAVE 진행 결과
- 현재 상태 log 우선 기록·이전 로그 changelog 이관·README 최신화 완료. 기능 화면 3장 직접 확인 및 동일 날짜 Notion 일지 7~10번 추가 완료(기존 1~6번·이미지 보존, 시체 미구현 설명 갱신). 시체 기능 커밋 1dc71be(34개 파일)를 origin/develop에 push하고 원격 SHA 일치를 확인했다. 이 완료 로그는 후속 문서 커밋으로 보존한다.
- 직전 HEAD 5ef4704(이전 구현 통합 b78f21b와 SAVE 기록 후속 커밋). .prefab/.prefab.meta 및 기존 에셋·복구·그래프·디자인 작업은 커밋 제외.
- SAVE 캡처: .unity/save-2026-09-30/corpse-cleaning.png, corpse-eating-verified.png, corpse-work-schedule.png. 청소 3/5초·포식 2/5초·작업표 Cleaning/결벽 ×1.5 확인. SaveCorpseCapture 도구 추가, 사용자 슬롯 미변경, 설정 복원·Play 종료 완료.
- 카카오톡 완료 알림 도구 미연결. 컴퓨터 종료 요청 없음(이전 요청은 새 작업으로 취소됨).

## 다음 세션·주의
- 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd (하위 페이지 있는 상위 문서 replace_content+allow_deleting_content 금지).
- 개발 일지 상위: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040 . 오늘 기존 ant 일지: https://app.notion.com/p/3eac4a0ecd31814e9835d4ef35fd577b . 동일 날짜 최신 ant 페이지에 이어 쓰기.
- 공식 CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. run_script로 검사 하나씩 실행, 긴 검사는 --timeout_ms 120000 --timeout 150.
- 전체 회귀·Player 빌드는 이번에 미실행. 기존 실패/미실행 InvasionChecks/RaidChecks/SceneInvasionChecks/AirborneChecks 별도.
- SAVE 외 일지·캡처·커밋/push 금지. master 반영은 별도 요청 시에만.
