# 프로젝트 로그

## 현재 상태 — 2026-09-25 (KST)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- 일반 개발 `develop`, 안정 `master`. `.prefab`/`.prefab.meta` 및 `Assets/_TeamImport`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`.
- 핵심 루프: 병력 배정 → 채집·건설·생산·연구 → 월드맵 원정·귀환 → 과학 트리 → 비행선 대이주 승리.
- 기존 구현: 장비 기반 역할, 9종 기술, 부상·기분·관계·정신 붕괴, 저장 v3/v2 이관, 30거점 원정·편입·주둔·포로·자동 수송. 전장의 안개·탐색은 구현 대상에서 제외.

## 이번 작업
- 의무실: Infirmary 연구 후 과학 화면에서 건설(Food 40·Soil 40·인력 4·8초). 8m 이내 유휴 장수를 상세 화면에서 배정, 동시 2명. 중상 4분 치료, 영구 손상 유지, 치료 중 명령 차단·기분 -5, 완료 충성심 +3. 중단·붕괴·시설 파괴 시 해제하며 진행도 보존. 환자 배정·회복 시간 저장 복원, 기존 저장 필드 누락 호환.
- 압축 저장(Fermentation): 기존·신규 창고의 Food·Soil·Special 한도 기여분 +50%(소수점 버림). 비활성화·철거 시 실제 증가분 회수. 반복 저장 복원 시 중복 방지, 이전 저장에서 누락된 연구 효과는 한 번만 보정.
- 과학 기술 30개 점검: 직접 효과 연결 8개(Vehicle, Aircraft, Infirmary, Fermentation, MigrationTheory, Hull, Cocoons, Engine), 나머지 22개 고유 효과 미연결. 선행 조건·연구·완료 저장은 공통 구현. README에 기술별 적용 범위 명시.
- 관련 코드: `Buildings/Infirmary.cs`, `Buildings/Storage.cs`, `Core/CampaignResearch.cs`, 건설·장수·UI·저장 연결. 검사는 `AgentScripts/InfirmaryChecks.cs`, `StorageResearchChecks.cs`.

## 검증
- 공식 Unity CLI, 각각 새 Play 세션: InfirmaryChecks 35, CampaignChecks 86, StorageResearchChecks 33, PlayableLoopChecks 45 통과. 컴파일 및 git diff --check 통과.
- StorageResearchChecks는 재로드 여러 회로 30초 기본 제한을 초과하므로 `--timeout_ms 180000 --timeout 190` 필요.
- graphify update . 완료(AST-only). 기존 graphify 변경과 신규 아트/프리팹은 작업 시작 전부터 존재했으므로 SAVE 시 구분한다.
- 카카오톡 완료 알림 도구는 현재 미연결.

## 제한 및 다음 작업
- 고유 효과 미연결 과학 기술 22개를 README 표와 Notion 기획을 대조해 순차 구현.
- 곰팡이 감염·치료, 약초 회복 보너스, 부위 재생 등 미구현. 영구 손상 치료 수치를 임의로 추가하지 않았다.
- 연구 비용·시간, 연구소 승급 조건, 비행선 비용·고치 한도·건조 중 침공은 기획과 차이 있음. 전체 밸런스·Player 빌드·화면 품질 검증은 후속.
- SAVE 진행 중: 변경 기능이 보이는 화면 캡처, develop 커밋·origin/develop push, 같은 한국 날짜의 Notion ant 개발 일지에 이어쓰기 또는 신규 작성 필요.
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd
- 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 프로젝트 `E:\Git\ant`.
