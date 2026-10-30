# 프로젝트 로그

## 현재 상태 — 2026-10-31 SAVE
- 프로젝트 E:\Gitnt, develop(일반)/master(안정). 시작 HEAD e42430d. 목표는 Notion 기획(스펙 문서) 전체 구현. 구조: Assets/_Project/Scripts(Core/Units/World/Buildings/Map/UI/Save/Boss), AgentScripts 검사, docs/IMPLEMENTATION_BOARD.md·OPERATIONS_100_STATUS.md(100행)·DECISIONS_*/GAPS_*/HANDOFF-*.
- 이번 범위(10/29~31, Claude+Codex J1·J2·K1·K2 분업): 100항목 잔여 처리 + 100항목 밖 기획 공백 구현.
  - Claude: 진행 시간표(PACING_1029)·성장/관계/의료 판정, 배우자 독점·애도, 친구 분노 같은 전장만, 위협 정지 중복 억제, 배치 불가 사유, 다이아 역발산·파나케이아·무중생유·무라마사, 전투 다이아 무간·참경·역린(근접20)·엑스텐(사격20)·지휘20=레기온, 병영 훈련(사냥 칸·할 일 없을 때 30초·경험치50%).
  - J1/J2: 경제·식품·산업·교역 / 전투·보스·원정·최종방어·엔딩 100항목(각 HANDOFF·DECISIONS).
  - K1: 다이아 바벨·낙토·태공·천기·식선·화룡·여래장·하멜른, 훈연식(23, 저장22·자원79), 요리 숙련 묶음별 저장, 수송 식품 신선도, 외교 '재료'→'흙'.
  - K2: 보스 지상 AoE 발동 중 저장, 편입지 간 실제 거리 이동, 포로 귀환 대기 사유·대체 진입점.
- 검증(Unity Play): SaveRoundtrip57, Review1031Claude26, Review1029Claude19, Delivery19, K1 87(+실스냅샷 JSON/Preflight), K2 오프라인107+장면22, Combat270, CombatBudget13(10/29: Trade23·Economy21·J2Runtime35). 오프라인 Roslyn 0오류. 플레이 중 recompile 금지(도메인 리로드로 상태 깨짐) → Temp/play.sh로 Play 재시작.
- 버그 수정: 교역소 설계도 판매 vs 보스 D 해금 충돌(구매 거부), K1 옛 저장 cargoFreshness 빈 목록 거부.
- 100항목 상태: 검토 대기0. 잔여: 31 연구소 승급 재료 연결(코드 가능), 77 시대 연구↔산업 배치, 17·28·72 UI 실행 확인, 33·34·44·46·47·54 실플레이/밸런스.
- 남은 공백: docs/GAPS_1029_J1/J2/claude.md 중·하(사막 세수↔학당 흙, 운반 중 완제품 재고, 노년 자연사, 원정 장비 전달, 다중 로켓 예약, milestones 압축 등). 유체 유출·전면 그래픽·대규모 성능은 사용자 보류.
- 사용자 확인 필요: 다이아 획득 5%면 한 판 0.1~0.3개(54). 모든 DECISIONS_*는 추천안 자동 선택(사용자 미확인).
- 보존: prefab/.prefab.meta 커밋 금지. 이전 log 원문 work/save-20261031/log-before-save.md.

