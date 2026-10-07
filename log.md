# 프로젝트 로그

## 현재 상태 — 2026-10-07 SAVE(저녁, Claude) — FIX-01~03 완료
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. develop 개발, master 안정. 소스 Assets/_Project/Scripts, 검사 AgentScripts. 기준 기획: Notion '메인 게임 화면 구체화'(3e7c4a0ecd3181e38211f6be9c44637d). 수치 전부 잠정.
- **공동 작업:** Codex·Claude가 `docs/IMPLEMENTATION_BOARD.md`로 담당·파일을 나눈다. 이번 세션: FIX-01은 사용자 지시로 Codex→Claude 인수, FIX-02·03도 Claude. DOC-01(문구 교정)은 Codex 완료(영어 허용, 일괄 번역 중단, 실행 화면 미검증).
- **FIX-01 휴식→숙소:** `CommanderDuty.SendToRest()`가 `Dormitory.Assign`으로 배정 숙소에 감(없거나 못 가면 제자리). Codex 컴파일 복구 후 DayNightChecks 42 두 번 통과. 이전 65행 실패는 재현 안 됨(원인 미확정, 실패 메시지에 목적지·경로 진단값 추가).
- **FIX-02 노화(사용자 결정: 복귀 시 전환):** 매달 성체 전체(`AntPool.Total`) 3%. 대기 몫(대기×3%)만 즉시 늙고 나머지는 `ColonyPopulation.State.agingDue`(저장됨)에 미룸 → `AntPool.ReturnAssigned/ReturnWorkers/ReleaseReserved`가 돌아온 수만큼 `OnAntsReturned`로 전환, 전사 시 동원 수로 상한. SaveValidator 음수 검사.
- **FIX-03 함정(사용자 승인 잠정 수치):** 기존 TrapPit = '끈끈이 함정'(표시명만, 저장 호환). 새 `BuildingKind.SpikeTrap`(enum 끝) = 같은 TrapPit 컴포넌트 가시 모드: 반경 1.2 지상 적(몬스터·보스·적 장수, 아군·공중 제외) 1초마다 피해 6, 8회 피해 후 파손, 수리·자동 재장전·함정 연구 해금 공유. 저장: 종류 이름 `SpikeTrap` + `trapSpikeHits`. 방어 탭 8칸, 카탈로그 '가시/끈끈이' 연결.
- **검사(전부 Play 새 세션·포커스, 순차):** DayNight 42(×2), Phase4 42(미룸·복귀·상한 추가), Workforce 80, Stage2 185(가시 5 추가), FurnitureCatalog 40, BuildCategory 62, SaveRoundtrip 57(×3, 가시·agingDue 왕복 추가). SaveRoundtrip `barracks tier` 실패는 다른 검사 직후 같은 Play 세션에서 돌린 상태 오염 → **저장 왕복 검사는 Play 재시작 후 실행**.
- Notion 반영: 건물·방어시설(함정 표 끈끈이/가시 분리, 7종), 메인 구체화(노화 기준·함정 구현 메모).
- 이전 상태(가구 5~6차 81/239, 휴게 13종, 3×3 명령 칸, 건설 4열, Decoration.MoodAt 성능 개선 Medium 60→24~29ms, 구식 Invasion 검사 삭제, 바이옴 None은 옛 높이색)는 그대로. WorldMapChecks(~750ms/프레임) 재측정 안 함. 회백색 지형 캡처 원인 미확정.
- 남은 결정/후속: VERIFY-01(초반→로켓 실제 성장 흐름 점검), NEXT-01~06(석유·원자 연구, 배관·자동화, 더러움·부패, 전력 소비·난방·조명 효과, 재료 업그레이드·상위 방, 나머지 가구·마을 시설·바이옴 적) — 기획 결정 후 착수. 훈련대 과녁·작전판, 분수대, 옛 저장 바이옴 None, 마을 공공시설 효과 미정. Player 빌드·전체 회귀 미실행.
- 보존(미커밋): Water.mat(_SrcBlend 5→1, 출처 불명), TimeManager.asset, graphify-out, design/·design_skill/, package.json·playwright·tests/·reports/·research_notes/(출처 불명), BiomeEvent/MultiStopRoute/SaveMapCapture/FurnitureShot 초안, Assets/Screenshots. .prefab 커밋 금지.
- 도구: Unity는 공식 MCP(`mcp__unity-editor-mcp__*`, 터미널 `unity pipeline list`는 서버 포트 비어 있어도 MCP는 정상). 검사는 SessionState + `AgentScripts/RunChecks.cs`(eval_file) 후 결과 폴링, 실행 중 editor_focus 반복. Graphify: `C:\Users\Shim Hyeonyeop\AppData\Roaming\uv\tools\graphifyy\Scripts\python.exe -m graphify update .`. python/python3 명령은 스토어 스텁이라 사용 불가.
