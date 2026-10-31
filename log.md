# 프로젝트 로그

## 현재 상태 — 2026-10-31 SAVE(오후)
- 프로젝트 E:\Git\ant, develop(일반)/master(안정). 목표는 Notion 기획(스펙 문서) 전체 구현. 구조: Assets/_Project/Scripts(Core/Units/World/Buildings/Map/UI/Save/Boss), AgentScripts 검사, docs/IMPLEMENTATION_BOARD.md·OPERATIONS_100_STATUS.md·DECISIONS_*/GAPS_*/HANDOFF-*.
- 이번 세션(컴퓨터 종료 후 재개): Codex L1·L2 → M1·M2 → N1·N2 3라운드 분업, Claude가 리뷰·원본 병합·Unity 검사.
  - Claude: 배우자 이혼(-40)·적장수 별점·미니맵 마름모(오전분), 실행 잔여 Play 검사(Residue11·Military9), 리뷰 수정 2건(M2 불러오기 후 AI 편성 재적용 지시, N1 성휘 치환 시 중복 종류 우선).
  - L1: 연구소 실물 승급·노년 자연사15%·재고 유지 운반분·정기 교환 중단·학당/운영비/캐러밴 안내(저장23). L2: AI 6시대 진행(77)·바벨 공통·보스 복구 제작·호위 XP 예산·milestones 압축·현지 장비 전달·시민 임시 거주·다중 로켓 예약.
  - M1: 전자부품·성휘금속·성휘합금 자원/제조법, 시대별 승급 연구 6종, 연구소 승급 정식 조건(저장24). M2: AI 시대별 편성/방어 장갑·로켓 좌석 초과 경고·집 파괴 주소 무효화.
  - N1: 성휘 광석 161 보장(신규 게임 본거지), 29 고정 담당(낙토·식선·화룡) 자동 배정 보호. N1 HANDOFF는 Codex 모델 용량 오류로 Claude 작성. N2: 96 색 외 구분(월드맵 세력 모양·D위험도, 배치 불가 X, 선택 사각 외곽, 범례).
- 검증: 오프라인 N1 25742·N2 77·M2 298·M1 103·L1 76·L2 97, Unity 컴파일 0오류, Play Claude28·Residue11·Military9·L1/L2/M1/M2/N2 RunScene 통과, 콘솔 오류0.
- 미해결: Military 첫 실행 1회 Restore "Missing building: 0"(3회 재실행 비재현, 원인 미확인). 실전투·실파괴·화면 육안·장기 밸런스(33·34·44·46·47·54) 잔여. 각 GAPS_1031_* 참조.
- TC 테스트(신규 요청): Google 시트 ant 게임 테스트 TC 876건(https://docs.google.com/spreadsheets/d/1uFO2jj09xPoA3wRVq8kbIBpLyeUzfwWiNShNlcR4BJc). 사용자 지시: P0·P1 우선, 결과는 시트 H·I열 직접 기록, Fail은 기록만, 판정 Untest/Pass/Fail/N/A(미구현)/NeedtoModify(TC≠기획)/NoData. **스크립트 판정 금지 — 실제 유저처럼 Play 조작**. computer-use 도구가 이 세션에 없어 PowerShell 화면 캡처까지만 확인, 실수행 0건(시트 미기록). 다음: 데스크톱 앱 Computer use 사용 또는 OS 입력(SendInput)+캡처 방식 확정 후 창고(WH) P1부터.
- 환경 메모: Bash의 python은 스토어 별칭(미실행) → node. 에디터 run_script는 TeamImport 전역 타입(ResourceType·SelectableObject) 충돌 → AntColony.* 완전수식. Codex 계정 전환 시 broker 구 토큰 → broker.json pid kill+.stale.
- 보존: prefab/.prefab.meta 커밋 금지. 커밋 제외: 재질/URP/씬/ProjectSettings/graphify/art. 이전 log 원문 work/save-20261031b/log-before-save.md.
