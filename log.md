# 프로젝트 로그

## 현재 상태 — 2026-09-24 (KST)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- 일반 개발 `develop`, 안정 `master`. `.prefab`/`.prefab.meta`와 `Assets/_TeamImport`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`.
- 전장의 안개와 탐색 기능은 사용자 요청으로 구현 대상에서 제외했다.

## 구현 상태
- 핵심 루프: 장수 병력 배정 → 채집·반납 → 건설·생산·연구 → 차량/비행기 → 30거점 월드맵 원정 → 전투·약탈·귀환 → 과학 트리 → 비행선 대이주(승리).
- 오늘(Codex) 추가:
  - 캠페인 과학 트리 30기술(`CampaignResearch`), 비행선 조선소(`AirshipYard`): 선체·엔진·고치 건조 → 탑승 → 출발 시 "GREAT MIGRATION — VICTORY" 결과 화면(탑승/잔류 명단, 경과 연도).
  - 장비 기반 역할: 무기(큰턱/산 분사기/방패/페로몬)가 Melee/Ranged/Defense/Support를, 날개 방어구가 비행을 결정. 보직·관직 변경 버튼 제거, `Weapon` 버튼·상세 화면에서 장착/해제.
  - 9종 기술(채집·건설·농사·낚시·제작·연구·근접·원거리·지휘, 최대 20, 시작 합계 40, 열정 반영). 지휘한도 = 10 + 지휘×2(흉부 부상·장신구 보정). 기존 채집 숙련도·관직 한도 대체.
  - 장수 개인 상태: 부상(6부위, 경상/중상/영구), 기분 요인, 관계·친구, 정신 붕괴 6종, 보상(Food 30, 게임 월 1회), 사망 모드(Gentle/Normal/Harsh).
  - 원정 보상 장비(정착지 1~2개, 보스 1개+설계도)가 수송 화물로 귀환 후 인벤토리에 들어온다.
  - 저장 v3: 기술·장비·개인 상태·비행선 저장, v2 세이브 자동 이관(`CommanderMigration`), 적 장수 기술 저장.
- 월드맵: 정착지 18, 보스 둥지 6, 중립 자원지 6. 편입/유기, 현지 생산, 주둔·회수, 침공·상실·포로/탈출·재정복 구출, 단일 거점 자동 수송.
- UI: 메인 메뉴, 새 게임, 설정, 저장 슬롯(수동 3+자동 1), 도감, 장수 관리, 일시정지, 토스트, 툴팁, 현재 목표 HUD, 승패 화면.

## 검증 — 2026-09-24 (Claude Code, Codex 작업 검증)
- Play 모드 공식 CLI `run_script` 전부 통과: RegressionChecks 45, CampaignChecks 86, PlayableLoopChecks 45, FullUIChecks 50, WeaponTalentChecks 91, SaveRoundtripChecks 42.
- `RegressionChecks`는 메인 메뉴 일시정지(timeScale=0) 때문에 실패하던 것을 검사 중에만 timeScale=1로 두도록 수정했다.
- 실행 조건: 각 스크립트는 새 Play 세션에서 실행한다. `SaveRoundtripChecks`만 `SaveSystem.NewGame` 후 실행한다.
- graphify 도구 환경이 사라져 `uv tool install graphifyy==0.9.58`로 재설치 후 갱신했다.

## 제한·미구현
- 중상 치료(의무실) 흐름이 없다: `treating` 플래그를 켜는 코드가 없어 중상은 자연 회복되지 않는다.
- 과학 기술별 게임 효과 적용 범위는 이번 세션에서 확인하지 않았다. 수치 밸런스, Player 빌드, 전체 화면 품질도 후속.

## 다음 작업
- 의무실 치료 연결과 기술별 효과 적용 범위 확인.
- 실제 플레이 기준 초반 자원·생산·연구 비용과 시간 밸런스 조정.
- 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd, 전체 UI: https://app.notion.com/p/3e2c4a0ecd31819eaba5ff850554216c.
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 프로젝트 `E:\Git\ant`.
