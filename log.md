# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant. 일반 개발 develop, 안정 버전 master. Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 검사 AgentScripts/.
- .prefab/.prefab.meta, Assets/_TeamImport/ 커밋 금지. 공식 Unity CLI/Pipeline 우선. Notion 수정·일지·캡처는 SAVE 때만.

## 2026-09-18 SAVE — 본거지 + 월드맵 원정 구조 전환 완료
- 기획: https://app.notion.com/p/3dec4a0ecd318158b488fa05230889be (월드맵 / 원정 시스템).
- 본거지에서 적 소굴·고정 보스를 제거하고 원정 거점으로 옮겼다. 성장형 주기 침공은 LocalIncursions의 드문 소규모 침입(고정 3기, 적 장수 포함 → 처치 시 포로)으로 교체했다.
- WorldMapManager가 거점 3곳(Amber Colony / Azure Colony / MiniBird Nest)을 본거지와 NavMesh가 끊긴 70m 섬으로 생성한다. 거점은 해금 전에도 60초 주기로 자체 성장한다.
- ScienceLab: 인구 60 + 낚시 해금 + T2 병영을 요구한다. 차량 연구 → 차량 건조 → (선행 완료 후) 비행기 연구 → 건조 순서이며, 첫 수송수단 완공 시 월드맵이 해금된다. 연구 중 연구소 비활성 시 연구는 취소(환급 없음), 재시작 가능.
- ExpeditionTransport: 차량 40/편도 20초, 비행기 100/편도 10초(임시값). 장수는 GameObject를 끄지 않고 탑승 상태(IsEmbarked)로 전환해 병력·성장·숙련도를 보존한다. 탑승 중 병력 배정/반환·공격 대상 지정·중복 탑승을 막는다.
- 동시 원정 지원: 거점 1곳당 수송수단 1대(Visitor)만 점유한다. 도착 시 장수가 착륙해 기존 전투·채집 조작을 그대로 쓰고, 전리품은 수송수단 화물로 보관했다가 귀환 시 1회만 본거지 자원에 합산된다.
- UI: WorldMapPanel(World / Science 토글)에서 과학연구소 건설·연구·건조, 거점 선택, 탑승/출정/전장 전환/귀환을 처리한다. HUD 보스 체력은 현재 보고 있는 전장 기준으로 바뀐다.

## 검증
- WorldMapChecks 81개, RegressionChecks 44개, WorkProficiencyLootChecks 68개, LabUpgradeChecks 전체 PASS(각 새 Play 세션). 컴파일 오류 0건.
- 이어받기 중 3건을 수정했다. ① 검사에서 2번 장수에게 병력 미배정 → 탑승 거부. ② 거점 수비 몬스터가 채집 중 장수 병력을 전멸시켜 약탈 검사 실패 → 검사를 전장 정리 후 약탈로 변경. ③ 제품 버그: 운반 용량에 딱 맞춰 캐면 노드에 부동소수 잔량(약 2e-6)이 남아 영원히 채집 가능 상태로 남음 → ResourceNode.IsDepleted에 0.001 epsilon 적용.
- 기존 검색 API obsolete 경고는 남아 있다. 검사 후 Play 종료.

## 다음 작업과 미정 사항
- 월드맵 기획 미정 목록에서 다음 후보: 원정 중 본거지 방어(병력 비율에 따른 위협/경보), 거점별 난이도·보상 차등, 정복 후 거점 상태(소멸 vs 재건), 건물 티어 단계 수. 사용자 선택 대기 중.
- 과학/수송 밸런스(연구·건조 비용, 적재량, 이동 시간)는 전부 임시값이다. 스카우트 영입 재설계와 최종 승리 조건(과학 탈출 엔딩 연결 포함)은 보류.
- 플레이어 장수 사망·포로 규칙, Special 소비처, 유지비 정책은 미정. 문서의 옛 로컬 침입/포로 설명은 새 맵 기획 기준으로 통일해야 한다.
- Notion 기획 부모 334c4a0ecd3180c4a796e5220302a0bd에 replace_content+allow_deleting_content 금지.
- 공식 CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. 제한된 셸에서 에디터 감지가 실패하면 승인된 셸에서 실행한다.
