# 에셋 필요성 재검토 — 2026-11-14

사용자 요청으로 다운로드·생성·추가 적용 중단. 이전의 ‘필수 에셋’ 목록은 모델 부재와 외형 교체를 혼동했으므로 아래로 정정한다. 기존 원본/웹 생성물은 보존한다.

## 기획 근거

Notion [기획](https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd), [건물·방어시설](https://app.notion.com/p/3d0c4a0ecd3181c28b59e26337c15aba), [메인 화면](https://app.notion.com/p/3e7c4a0ecd3181e38211f6be9c44637d)을 읽었다.

- 숙소·식당은 벽/문으로 구획하고 가구로 판정하는 방이다. ‘초가집 수준의 기술’은 완성형 집을 추가 제작하라는 요구가 아니다. 시민 주거 정원 설명은 장수 단일 유닛 재설계 이전 기록이다.
- RoomRoofs.cs는 방 칸에서 자동 지붕을 생성한다. 단독 경사 지붕을 가져오는 것만으로 현재 방 지붕에 적용되지 않는다.
- Notion 11월14일의 토착 부족/외계 침공/본선 최종전과 GAME_DIRECTION.md 11월12일 기준은 차이가 있다. 규칙이나 코드를 자동 변경하지 않는다.

## 신규 제작 제외: 기존 자산

| 대상 | 기존 자산 (Assets/Resources 기준, art는 별도) | 판단 |
|---|---|---|
| 초가집 | Environment1110/Models/Hut/Hut.fbx | 이미 보유. 시민용 집 확대는 현행 목표 아님 |
| 침대·식탁·책장·목공대·목책 | Furniture1113/Models | 기존 사용/접점 검수 우선 |
| 조리대·모닥불·연구대·용광로·공방 | Release1112/Models | 연결 존재 |
| 치료 침대·약제대 | Life1112/Models | 연결 존재 |
| 창고·감시탑 | Environment1110/Models/Storage, Watchtower | 연결 존재. 감시탑은 공격탑과 다름 |
| 식량 저장고·화덕 | Facilities/CC_FoodStore.fbx, CC_Hearth.fbx | 외형 교체 후보일 뿐 부재 아님 |
| 석재 작업대 | Facilities/CC_Stonecutter.fbx | 기존 석공대와 같은 대상 |
| 숯가마 | Facilities/CharcoalKiln.fbx | 연결 존재 |
| 우물·세면대·화장실 | Facilities/CC_ManualWell.fbx, CC_Washbasin.fbx, CC_Toilet.fbx | 연결 존재 |
| 문·성문·방벽·함정 | Facilities/CC_DoorFrame.fbx, CC_CastleGate.fbx, CC_SoilWall.fbx, CC_StickyTrap.fbx 등 | 기존 모델/기능 존재 |
| 재봉 작업대 | art/production-kit-20261019/exports/facilities/SewingTable.fbx | 원본/미리보기 존재. FacilityVisual 전용 연결 없음. 기능·메뉴 연결 조사 먼저 |
| 대장간 | art/production-kit-20261019/exports/facilities/Blacksmith.fbx, art/release-assets-1112/download의 Compact_medieval_blacksmith 모델 | 원본 이미 보유. 공방 모델/생산 역할 대조 먼저 |

work/building-inventory-1114/buildable.tsv의 124종/고유117개 경로는 모두 실제 파일이 있다(누락0). FacilityVisual.cs도 대조했다. 공방 Workshop.fbx와 기존 Meshy 대장간 원본은 SHA256이 일치한다. 재봉대/대장간/저장고 기존 미리보기도 확인했다. 파일 존재·연결은 전체 플레이/시각 품질 완료를 뜻하지 않는다. catalog의 plan_only는 별칭/중복을 포함하므로 제작 수로 계산하지 않는다.

## 이번 5종 처리

- 벽: 중단 요청 전에 Tribal1114 임포트/기본 흙벽 모델 연결 완료. 컴파일 오류0, 기본/석재 모델 선택 및 Unity 미리보기 확인. 자연 건설·인접 이음새는 미검증. 추가 적용 중단.
- 지붕: 웹 생성물 보존, 다운로드/적용 보류. 자동 지붕 호환 검토 먼저.
- 목책문: 웹 생성물 보존, 다운로드/적용 보류. 기존 성문/문 재사용 및 개폐·격자 호환 검토 먼저.
- 집 형태 저장고: 웹 생성물 보존, 다운로드/적용 보류. 기존 저장고가 있어 필수 신규 목록에서 제외.
- 돌 화덕: 웹 생성물 보존, 다운로드/적용 보류. 기존 화덕/조리대/모닥불과 역할 대조 먼저.

## 실제 신규 후보

1. **외계 무인기 + 파괴 잔해:** 최신 Notion의 공격/잔해 관찰 흐름에 필요. Assets/art 이름·연결 검색에서 해당 모델을 찾지 못했다. 기존 곤충 적이나 불시착 잔해와 다르다. 외형/크기/이동·공격·사망 표현을 정한 뒤 정상/파괴 상태를 한 세트로 재사용할 후보. Meshy 계정 전체 보유분 감사는 하지 않았으므로 제작 전에 계정 중복도 확인해야 한다.
2. **지금 확정할 신규 생활·생산 건축 모델은 없음.** 기존 원본 연결과 가구 접점 검수가 우선이다.
3. 무전기·운송수단·본선/최종 보스는 후속 진행 설계와 기존 자산 대조 후 판단. 이번 제작 목록으로 확정하지 않는다.

감사 중 신규 생성/결제/Notion 수정/게임 규칙 변경은 하지 않았다.
