# 환경·건물·자원 에셋 개선 — 2026-11-10

## 범위와 예산
- 사용자 요청: Meshy 크레딧으로 게임에 어울리는 고품질 에셋 제작·Unity 반영. 자연물·건물·여러 지형 바닥·자원 모두 포함. 무료 Asset Store/기보유 에셋도 허용.
- 시작 잔액 755. 최대 600 사용, 최소 155 유지로 해석하여 사용자에게 알림. 계정 잔액 증가를 추가 예산으로 보지 않는다. 생성별 사용량 별도 추적.
- 다른 채팅의 캐릭터·손 리깅·애니메이션 파일은 수정하지 않는다. 기존 미커밋 변경 보존. SAVE 아님.

## 기획 근거
- [자원 시스템](https://app.notion.com/p/3d0c4a0ecd31812d80fbeceabb70193e): 온대 숲, 초원, 한랭, 열대·습지, 건조, 해안, 고산·특수 지역. 목재·석재·구리/주석/철 등 광물·작물 분포. 주산지이지 독점 산지가 아님.
- [맵·진행](https://app.notion.com/p/3d4c4a0ecd318162a5e0c6f5b51e62dc): 지역·기후·계절을 조합. 현행 배경은 초가집 수준 지구 부족. 이전 불시착 배경과 혼재하므로 게임 규칙은 이번 외형 작업에서 변경하지 않음.
- [건물·방어시설](https://app.notion.com/p/3d0c4a0ecd3181c28b59e26337c15aba): 초가집·저장·생산·방어시설, 주재료 변경과 격자 배치 유지.
- [자연물 도감](https://app.notion.com/p/3fbc4a0ecd3181fca35ecd6d4e5dac90): 활엽수·키 큰 나무·바위·광석·수풀·야생 버섯. 기존 모델은 단색 저폴리 조립 모델. 도감의 오래된 Unity 미연결 표기는 현재 코드와 다름.
- [자원 305종 제작 도감](https://app.notion.com/p/401c4a0ecd3181b9a5a5f22d1f51c39a): 확장 에셋 후보 목록. 전 품목을 게임에 구현했다는 뜻이 아님.

## 아트 기준
- 현재 애니메 캐릭터와 어울리는 절제된 동화풍 자연 재질. 큰 실루엣은 단순하고 나무결·짚·광맥은 텍스처로 읽힌다.
- 지면은 유닛 가독성을 해치지 않는 낮은 대비. 반복 경계·밝기 편향 확인. 작은 물체는 실루엣과 색으로 종류 구분.
- 첫 묶음은 숲 나무, 초가집, 광석과 여러 지면. 이후 건물·자원 비중을 유지하며 예산 내 확장.
- 기존 Resources 연결을 재사용하고 새 게임 규칙·채집 종류·생물군계 enum은 자동 추가하지 않는다.
- 현 코드의 Forest/Garden/Waterside/Desert/Cave/City 스타일 및 별도 기후 변형을 유지한다. 기획의 7개 지역군과 1:1 enum 관계가 아님.

## 제작 내역
| 분류 | 항목 | 수량 | 크레딧 |
|---|---|---:|---:|
| 이미지 | 3D 참고 시안 10장, 바닥 텍스처 8장 + 숲/초원 수정 2장 — Nano Banana Pro | 20 | 180 |
| 3D | Meshy 7.1 Ultra 2K + Meshy 7 2K 텍스처 | 10 | 350 |
| 경량화 | 건물·나무 10K, 작은 자연물·광석 3K 목표 | 10 | 0 |
| 합계 | 최대 600 중 사용 | | **530** |

시작 755 → 현재 화면 잔액 325. 도중 계정에 100 증가가 관측되어 755−530+100=325로 일치한다. 이 증가분의 원인은 확인하지 않았으며 추가 예산으로 사용하지 않았다. 본 작업에서 충전·구독 변경 없음.

- 건물: Hut(초가집), Storage(저장 창고), Watchtower(목조 망루).
- 자연·자원: Oak(활엽수), Pine(침엽수), Bush(관목), Rock(화강암), Mushrooms(야생 버섯), CopperDeposit(녹색 구리 광맥), IronDeposit(녹갈색 철 광맥).
- 바닥: ForestFloor, Grass, Sand, Wetland, Alpine, Snow, Soil, Coast.
- 원본 PNG와 다운로드 ZIP에서 푼 FBX/PBR 파일: `art/environment-1110`.
- Unity용 모델·URP 재질: `Assets/Resources/Environment1110/Models`.
- Unity용 지면 8종: `Assets/Resources/Environment1110/Ground`.

## 연결 범위
- ResourceVisual: 기존 식량·목재·수지·잎·석재·구리광석·철광석 채집 노드 외형. 바닥 운반 물품·낚시·농장 건물은 기존 경로 유지. 새 자원 규칙을 추가한 것은 아니다.
- FacilityVisual: 기본 재료(Soil)의 초가집·창고·망루. 기존 건물 footprint/재고/배치 규칙 유지. 새 모델은 단일 아틀라스이므로 기본 이외의 주재료는 벽/지붕 재질이 분리된 기존 모델을 사용하며, 개조 시 양방향 전환한다. 새 모델 자체의 재료별 버전은 제작하지 않았다.
- ExpeditionTerrain: 기존 원정 외곽 장식의 나무·관목·바위 외형. 원정 신규 출발 중지 정책 유지.
- 6개 BiomeMapStyle의 texture/snowGround/leafGround 교체. 높이·물·생성 확률 유지. 원본 스타일 백업은 `work/environment-1110/biomes-before`.
- 현 환경의 바이옴 장식 prefab 참조는 기존부터 모두 누락되어 있음(Inventory 검사). 이 작업에서는 외부 패키지의 누락 GUID를 임의 복원하지 않고 현재 채집 노드와 원정 장식 경로를 사용한다.

## 검증
- 공식 CLI recompile/recompile_status: 완료, 오류 0.
- `Environment1110Checks.Main`: **69개 통과**. 10개 모델/URP BaseMap/폴리곤 한도, 7개 실제 ResourceNode 연결·재고 보존·선택 collider, 3개 건물 로더 및 개조 재료 fallback, 6개 바이옴의 비텍스처 필드 보존.
- 모델 삼각형: Hut 9,224 / Storage 9,822 / Watchtower 9,797 / Oak 9,297 / Pine 10,400 / Bush 2,918 / Rock 3,102 / CopperDeposit 3,090 / IronDeposit 3,065 / Mushrooms 3,053.
- `Environment1110Preview.Main`: Unity URP에서 실제 임포트 모델과 기존 캐릭터를 렌더링. 지면 8종 2×2 반복 렌더링 확인. 첫 숲 이미지의 흰 테두리와 첫 초원의 사선 무늬를 발견하여 재생성 후 교체. 이전 소스는 `*.source-v1.png`로 보존.
- 화면: `art/environment-1110/Unity_Models.png`, `Unity_Ground.png`, Meshy 제작 화면 `Meshy_Completed.png`.
- 상세 검사 출력: `work/environment-1110/validation.txt`, `model-import.txt`, `spawn-inventory.txt`.
- 실행 중 읽기 전용 확인: Food 24, IronOre 3, CopperOre 2, Stone 4, Resin 2, Wood 5, Leaf 3 = **새 외형 43개**. 임시 별도 카메라로 `Unity_LiveWorld.png` 캡처. 그 실행은 애니메이션 검사에서 생성한 biome=None(스타일 미지정)이므로 흰 기본 바닥이며, 6개 정식 바이옴 지면 검증으로 간주하지 않는다.
- `Environment1110BiomeView.Main`: 정식 6개 스타일을 실제 `AntColony/TerrainBlend` 셰이더와 Texture2DArray 복사 경로로 렌더링한 `Unity_Biomes.png` 확인. 숲·초원·모래·암반·흙 지면 표시 정상. 계절/기후 규칙은 기존 코드 유지.
- 외형 검증용 임시 오브젝트·카메라·렌더 타깃은 사용 후 제거하고 전역 조명 상태를 복구한다. 사용자 저장 슬롯 및 씬 저장 없음.
- 다른 채팅이 애니메이션 Play 검증 잠금을 보유 중. 본 작업은 해당 게임을 재시작/중지하거나 카메라를 이동시키지 않았다. 6개 바이옴을 각각 새 게임으로 실행하는 검사는 아직 수행하지 않았다.
- Graphify AST update 완료(최종 보조 검증 스크립트 포함 재갱신). SAVE/커밋/push/Notion 쓰기 없음.
