# 주민 키트(resident-kit) — 시설·캐릭터 부품·공통 동작·도구·미리보기 분리

## Codex 검수 보완 — 2026-10-18

- 원본 몸 메시 11,906개 정점과 키트의 대응 위치 오차 0. 체형을 다시 만들지 않았다.
- 원본 부품 보드에서 누락된 TEARY·SOBBING·NERVOUS·DEAD 4종 복구. 부품 총 94개, Expr 15개. 공허함(EMPTY)은 별도 유지하며 Death에는 DEAD를 연결한다.
- `kit_restore_expressions.py`는 기존 키트에 표정만 추가하며 장비를 보존한다. 새 빌드에도 같은 복구 단계를 연결했다.
- 캐릭터 FBX 재내보내기 및 최신 미리보기 재생성: 동작 44개, 검수 장면 156개(빈 인덱스 제외). 카메라는 애니메이션 5시점의 변형된 외곽을 기준으로 잡는다.
- 물뿌리개는 원본 전시 자세의 부품 위치를 유지한 탓에 손잡이·본체가 손과 어긋났다. `kit_watering_can.py`에서 손잡이 기준의 작은 로우폴리 도구로 정리하고 도구 FBX를 갱신했다. 캐릭터 팔 길이와 공통 동작의 원래 손 이동량은 유지한다.
- 복구 표정은 현재 몸 표면 안으로 파묻히지 않도록 깊이만 보정했다. 모양·색·눈물 등 원본 디자인은 유지한다.
- 아래 과거 표의 90개 부품·34개 동작·94개 장면 수치는 초기 제작 당시 값이다. 기존 GIF는 이번 카메라/표정 변경을 아직 반영하지 않은 자료이며 최신 확인은 `resident-preview.blend`를 사용한다.
- Unity 적용·전체 외형 조합 전수 검증은 하지 않았다.
- 최종 재검사: 가중치 메시 127개 오류 0, FBX 부품 94/94, 동작 44개 왕복 최대 오차 0.0, 시설 141개 지점 오차 0.0001 이하·경첩 14/14. 관통 표본은 화분·냉장고·샤워 3곳을 계속 표시한다. 화분 도구 침투 표시는 수정 전 50~94정점에서 22~23정점으로 줄었지만 완전 해소 판정은 아니다. 냉장고의 숙임 접촉과 샤워 내부 겹침도 남아 있다.

2026-10-18 Claude. 요청서 「조합형 캐릭터와 가구 작업 애니메이션 분리」 결과물.
원본(`art/resident-anim`, `characters/residents-24`, `characters/parts-board`, `colony-core`, `ant-anim/ant-tools.blend`)은 **읽기만** 했고 수정하지 않았다. Unity 씬·코드·HUD는 건드리지 않았다.

## 1. 파일 구분

| 파일 | 들어 있는 것 | 들어 있지 않은 것 |
|---|---|---|
| `resident-character-kit.blend` | 공통 리그 `ResidentRig`, 부품 90개(`Parts`, 장비·장신구 6 포함), 수동 조합 4종(`Combos`), 털 팔레트 재질 24쌍 | 시설, 동작(Action) |
| `resident-animations.blend` | 공통 리그 + 공통 동작 34개(`Resident|<이름>`, 기존 30 + 추가 4) | 메시, 시설 |
| `resident-tools.blend` | 도구 8종 `Tool_*`, 손 소품 6종 `Prop_*`, 손 장비 2종 `Equip_*` | 리그, 캐릭터, 시설 |
| `resident-facility-kit.blend` | 시설 79개(배율 적용 완료), 작업 지점 Empty, 문·뚜껑·성문·포탑 경첩 동작 | 주민 메시·리그·동작 |
| `resident-preview.blend` | **검수 전용.** 위 4개 파일을 *링크*해 조립한 장면 94개 | 납품 자산 아님(여기서 아무것도 떼어 쓰지 않는다) |
| `exports/character/` | `Resident_Parts.fbx`(리그 + 부품 90개 전부, 엔진에서 켜고 끔), `Resident_Combo_A~D.fbx`(샘플) | |
| `exports/anim/` | `Resident@<동작>.fbx` 34개(리그만, 소켓 본 포함) | |
| `exports/tools/` | `Tool_*.fbx`, `Prop_*.fbx`(원점 = 손잡이) | |
| `exports/facilities/` | `CC_*.fbx` 79개(원점 = 시설 바닥 중심, 작업 지점 Empty 포함, 경첩 시설은 동작 포함) | |
| `facility-points.json` | 시설별 배율·외곽·작업 지점(시설 로컬, 최종 배율 기준) | |
| `parts-manifest.json`, `tools-manifest.json` | 부품·도구 목록과 부착 정보 | |
| `previews/gif/` | 시설 76개 + 외형 교체 4개 + 기본·추가 동작 11개 + 장비 3개 움직이는 미리보기 | |
| `validation.json` | 아래 7절 검증 원본 수치 | |
| `scripts/` | 재생성 스크립트(아래 8절) | |

## 2. 좌표·단위

- 단위 = 캐릭터 원래 크기(키 약 2.24, 몸통 폭 1.76). 정면 = 로컬 **-Y**, 위 = +Z, 24fps. FBX는 `-Z forward / Y up`.
- **캐릭터·시설·도구 FBX는 같은 단위다. Unity에서는 세 종류 모두 같은 Scale Factor 하나를 쓴다.** 시설만 따로 키우거나 줄이면 작업 지점이 어긋난다.
- 시설 배율은 메시에 *적용*돼 있다(루트 scale = 1). 그래서 `facility-points.json` 좌표 = FBX 안 Empty 좌표 = 시설 루트 로컬 좌표.

## 3. 공통 리그 (Generic, 손가락 없음)

변형 본 12개(기존 `resident-anim/rig.py`와 이름·휴식 자세 동일 → 기존 30개 동작이 그대로 재생):
`Root, Body, Head, Arm.L/R, Hand.L/R, Foot.L/R, Ear.L/R, Tail`.

비변형 **소켓 본 4개**(본 머리 = 부착점, 본 +Y = 손잡이 방향):

| 소켓 | 부모 | 머리 위치 | 용도 |
|---|---|---|---|
| `Socket.Tool.R` | Hand.R | (0.50, -0.56, 0.62) | 오른손 도구·창 |
| `Socket.Tool.L` | Hand.L | (-0.50, -0.56, 0.62) | 왼손 방패 등 |
| `Socket.Carry` | Body | (0, -0.78, 0.62) | 두 손 운반물·바구니 |
| `Socket.Hat` | Head | (0, 0, 2.07) | 모자·머리 장식(예약) |

기존 `ToolGrip/CarryAnchor` Empty(뼈 위치만 복사, 오프셋 힌트 미적용)는 쓰지 않고 이 소켓 본으로 대체했다.

## 4. 부품 규격 (`parts-manifest.json`)

- 모든 부품: **원점 = 캐릭터 원점(발 사이 바닥)**, 축·배율 = 리그 휴식 자세, 리그에 직접 가중치. 24종 원본의 몸 메시가 정점 수·외곽이 완전히 같아 서로 섞어도 위치가 맞는다(확인함).
- 가중치 규칙: 몸통 표면 부품(얼굴·표정·무늬·옷·몸통/얼굴 상처)은 몸 메시와 같은 **목 혼합(z 0.66→0.98에서 Body→Head)**, 머리 장식·머리카락·안경 = Head, 귀 = 좌우 Ear, 꼬리 = Tail, 손발 = Hand/Foot, 팔·다리 상처 = 해당 Hand/Foot.
- 몸 부품(`Part_Body_01`)에 **부드러운 팔 연결 메시를 포함**했다(기존엔 미리보기에서만 후처리하던 것).

| 범주 | 개수 | 슬롯 규칙 |
|---|---|---|
| Body | 1 | 필수 1개. 색 = 털 팔레트 |
| Tail | 1 | 필수 |
| Ears | 5 (낮은/높은 늘어진 귀, 둥근 귀, 뾰족 귀, 큰 뾰족 귀) | 1개 |
| TopCrown / TopCheek / TopLeaf / TopFlower | 1 / 1 / 3 / 4 | 0~여러 개 |
| Hair | 6 | 0~1개 |
| Mark(얼굴 무늬) | 7 | 0~1개 |
| Face(기본 얼굴) / Expr(표정) | 6 / 11 | **둘 중 하나만** 켠다 |
| Overlay(땀·눈물·홍조) | 3 | 표정 위에 겹침 |
| OutfitBag / OutfitCape / OutfitCollar | 1 / 3 / 6 | 각 0~1개 |
| Glasses | 1 | 0~1개 |
| Injury(흉터·긁힘·멍·밴드 × 이마·볼·몸통·왼팔·오른팔·다리) | 24 | 표정과 **별개 부품**, 여러 개 |
| Equip(보호 갑옷 `Equip_Armor`, 비행 장비 `Equip_FlightGear`) | 2 | 갑옷 칸 1개(둘 중 하나) |
| Trinket(발목 띠·감각 보조 고글·향기 주머니·지휘 표식) | 4 | 장신구 칸 1개 |

- **털 팔레트:** 재질 이름이 `Fur_NN`, `Inner_NN`인 슬롯은 몸 색을 따라간다. 조합할 때 같은 번호 NN(01~24)으로 함께 바꾼다(Blender 조합은 오브젝트 단위 재질 덮어쓰기로 처리). 그 외 재질(옷·표정 잉크 등)은 부품 고유색.
- **상태 → 표정 연결:** Sleep=`Expr_SLEEP`, Death=`Expr_DEAD`, Hurt=`Expr_PAIN`, Eat=`Expr_TASTY`, Mourn=`Expr_SAD`, Rest=`Expr_SLEEPY`. 동작이 끝나면 원래 Face/Expr로 되돌린다. 미리보기 장면은 이 규칙대로 조립했다. 공허한 표정인 `Expr_EMPTY`와 사망 표정은 구분한다.
- 수동 조합(자동 랜덤 생성 아님): A(팔레트3·둥근 귀·꽃·가방), B(16·뾰족 귀·잎·망토·안경·곱슬·볼 흉터·왼팔 밴드), C(22·높은 늘어진 귀·볏 머리·졸린 표정·옷깃·무늬·이마 멍), D(11·낮은 늘어진 귀·볼털·가방·망토·앞머리).

## 5. 공통 동작 30개 (`exports/anim/Resident@<이름>.fbx`)

Idle, Walk, Run, StandEnter, StandExit, StandIdle, Pickup, PutDown, Carry, GroundWork, BenchWork, Hammer, Chop, Mine, Treat, Operate, Stir, Grind, Saw, Stamp, Wash, Eat, Sit, Rest, Sleep, Mourn, Inspect, Attack, Hurt, Death. 길이·반복 여부는 `art/resident-anim/clips.json`.
- **추가 4개(기획에 있으나 없던 동작, `clips-extra.json`, `scripts/kit_extra_clips.py`):** Fly(비행 장비 이동, 떠 있기 0.55) / Downed(쓰러짐, 살아 있음, 표정 `Expr_PAIN`) / Captive(포로 앉음·팔 뒤로, `Expr_SAD`) / CarryPerson(쓰러진 장수 구조 운반, 대상은 `Socket.Carry`에 Downed 자세로 부착).
- **가구용 추가 10개(FURNITURE-97, 2026-10-18, 같은 스크립트):** 기존 동작으로 표현할 수 없던 가구용. PlayBoard(앉아 말 옮기기: 놀이판·장기·바둑·도박판) / Bathe(앉아 몸 담그기, `Expr_SLEEPY`: 목욕통·온천) / Wrestle(씨름판) / Throw(가시 다트) / PlayMusic(악기) / Exercise(운동기구) / Swing(그네, 그네 피벗에 함께 탐) / RockCradle(요람) / Spin(앉아 물레) / WaterPlant(화분, 도구 `Prop_WateringCan`). 전부 반복 동작, 총 동작 44개.
- 시설 앞 좌표는 동작에 들어 있지 않다(이동은 게임 쪽). Sleep·Death의 Root 회전·높이는 자세일 뿐 위치가 아니다.
- 작업 동작 12개는 손을 0.30 앞으로 뻗게 보정된 버전(`reach_corrected`)이다.
- `VisualContact` 마커는 **시각 타이밍 참고용**이다. 자원 지급·대미지·치료 완료 이벤트가 아니다.

## 6. 도구·시설

**도구**(`tools-manifest.json`): 원점 = 손잡이 잡는 점, 메시는 소켓 로컬 좌표로 저장. 소켓 본 머리에 부모로 붙이고 로컬 변환을 0으로 두면 휴식 자세에서 손에 들린 상태가 된다(Blender 미리보기는 이 방식). 손잡이는 소켓 +Y, 날·머리는 캐릭터 정면. Hammer .62 / Axe·Pickaxe .74 / Saw .56 / Spoon .46 / Pestle .40 / Scalpel .28 / Stamp .30. 소품: Spear(근접 무기)·WateringCan=오른손, Shield(방패)=왼손, Book·Basket·Cloth=Carry. 손 장비: `Equip_AcidSprayer`(산 분사기, 노즐이 정면) · `Equip_Signaller`(지휘 신호기, 깃발) = 오른손. 장비 목록 대응: 근접 무기=Prop_Spear, 산 분사기=Equip_AcidSprayer, 방패=Prop_Shield, 지휘 신호기=Equip_Signaller, 보호 갑옷·비행 장비·장신구 4종 = 캐릭터 부품.

**시설 배율**(클릭하기 쉬운 크기, 캐릭터 기준):

| 분류 | 배율 | 기준 |
|---|---|---|
| 일반 시설·자연물 45개 | 1.5 균일 | |
| 작업대·식탁 8개 | XY 1.5, Z = 상판 0.80이 되게 | 작업 동작의 손 높이(0.65+0.20) |
| 침상 2개 | XY 1.8, Z = 매트 0.62 | 옆에서 치료 |
| 의자·벤치·변기 | XY 2.4, Z = 좌판 0.42 | 앉기 |
| 잠자리 매트 | XY 2.4, Z 1.2 | 누운 몸 길이 |
| 운반 물품 20개 | 가장 긴 변 0.70 | `Socket.Carry`에 들기 |

**`facility-points.json`**(시설 루트 로컬, 최종 배율):
- `points`: 원본 작업 지점(WorkPoint_0, Input/Output, Seat, Patient, CarryAnchor) — 배율 반영.
- `work.clip / tool`: 사용할 공통 동작·도구.
- `work.stand`: 캐릭터 원점을 둘 바닥 위치. `work.facing`: 캐릭터가 바라볼 방향(+Y = 시설 쪽).
- `work.hand_target`, `work.tool_contact`: 작업면 위 손·도구가 닿는 점(작업대·침상).
- `work.seat`, `work.lie`: 앉기·눕기 기준점(의자·침상·매트).
- `bounds_min/max`: 시설 전체 외곽. **클릭 영역은 이 외곽 상자 전체를 쓰는 것을 제안**(Unity 선택 기능은 이번 범위 아님, 미구현).

**추가 가구 62종(FURNITURE-97, 2026-10-18):** `FurnitureCatalog`에서 `BuildingKind`가 있는(=게임에서 건설 가능한) 97종 중 모델이 없던 62종. 목록·한국어 이름·도감 분야·동작·도구는 `scripts/extra_spec.py`, 모델은 `scripts/models_extra.py`(colony-core `geometry.py` 도우미·팔레트 재사용, 원본 미수정), 조립·내보내기는 `scripts/kit_facilities_extra.py`.
- 처음부터 키트 단위로 만들어 **배율 1**(상판 0.80·좌판 0.42·침대 0.62·문 폭 2.3/높이 3.0). 위 배율표 대상이 아니다.
- 같은 `facility-points.json`에 들어가며 추가 키: `name_ko`, `building_kind`, `catalog_section`, `memo`, `source: FURNITURE-97`, `work.turn`(캐릭터 Z 회전, π = 시설을 바라봄 / 0 = 등지고 앉기), `work.ride`(그네처럼 캐릭터가 함께 움직이는 피벗).
- 시설 자체 동작 8종(48f): 잠금문·창살문 `OpenClose`, 저장고 뚜껑 `OpenClose`, 요람·그네 `Rock` 반복, 쳇바퀴·물레·병뚜껑 모빌 `Spin` 반복. `hinges[].mode`에 표시.
- 분야: 타일 5 · 생활 6 · 저장 2 · 환경 4 · 전력 2 · 식량 4 · 작업 5 · 방어 1 · 휴게 11 · 장식 13 · 군사 5 · 마을 3 · 이동 1. 삼각형 합계 약 12,400.
- 사진: `previews/models/<id>.png`, 장비 착용 사진: `previews/equipment/<EquipmentRecipe>.png`(`scripts/render_equipment.py`).

## 7. 검증 결과

`validation.json` (Blender 5.2, `scripts/kit_validate.py`). **Blender 쪽 검사만 했다. Unity 임포트·Animator 재생은 하지 않았다.**

| 항목 | 결과 |
|---|---|
| 파일 분리 | 시설 키트: 리그 0 · 주민 부품 0 · 주민 동작 0 / 캐릭터 키트: 시설 0 · 동작 0 / 동작 파일: 메시 0 / 도구 파일: 리그 0 → 모두 통과 |
| 가중치 | 부품·조합 메시 123개, 정점 가중치 합 1·Armature 대상 일치, 문제 0건 |
| 부품 FBX | `Resident_Parts.fbx` 재불러오기: 본 16개 이름 일치, 메시 90개 중 90개 스킨 유지 |
| 동작 FBX | 34개(추가 4 포함) 재불러오기 후 손·머리·Root 위치 최대 오차 0.0 (Blender 가져오기가 Hand 본을 자동 연결해 위치 키를 무시하는 현상이 있어 검사 때만 연결을 풀고 비교. FBX 데이터 자체는 동일) |
| 시설 FBX | 79개 재불러오기: 작업 지점 Empty 위치 최대 오차 0.0001, 경첩 동작 6/6 유지 |
| 외형 교체 | `Swap__CC_Workshop__Combo_A~D` 4장면: 같은 시설·같은 위치에서 외형만 바꿈, 손·도구 위치 동일(같은 리그·같은 소켓) |
| 관통 표본 | 시설·교체 장면 80개 × 4프레임. 도구 정점이 몸 안 5cm 이상 들어간 경우 / 시설 바닥 범위 안 몸 정점이 시설 안 5cm 이상인 비율 3% 초과를 표시. 남은 표시: Preview__CC_Refrigerator, Preview__CC_Shower |

- `Preview__CC_Refrigerator`: Pickup에서 몸을 숙여 열린 냉장고 안으로 머리가 4~6% 들어간다(꺼내는 동작). 서는 위치를 0.3 뒤로 뺐지만 남아 있다. 전용 '선반에서 꺼내기' 동작이 있으면 해결된다.
- `Preview__CC_Shower`: 샤워 칸 안에 서는 시설이라 의도된 겹침.
- 도구 손잡이는 손 본 머리에서 0.159 앞(소켓 위치)에 있다. 손에서 떨어지는 프레임은 없다(소켓 본에 부모로 고정).
- 시각 확인: 작업대·주방·약제대·식탁·의자·변기·병상·매트·냉장고·바위·나무·운반·외형 교체 4종·공격을 정지 화면으로 확인. 나머지는 GIF 생성까지만 했고 한 장씩 모두 보지는 않았다.

### 남은 한계
- Unity 임포트·Animator·소켓 부착 방향은 미검증. Blender 본 축과 Unity 변환 차이로 도구 회전 보정이 필요할 수 있다.
- 동작은 기존 30개 그대로다. 높이가 다른 시설(높은 선반·낮은 바닥)에 맞춘 전용 손 높이 동작, 앉아서 먹기 전용 식탁 의자, 두 손 정밀 접촉(IK)은 없다.
- 표정 전환은 부품 교체 기준만 정했다(표정 애니메이션·눈 깜빡임 없음).
- 시설 배율은 분류별 규칙이다. 개별 시설 미세 조정·클릭 영역은 게임 적용 때 확인 필요.


## 8. 재생성 순서 (Blender 5.2, MCP 또는 `blender -b --python`)

1. `scripts/kit_build_character.py` → character-kit (부품 추출 `kit_character.py` + 조합 `kit_combos.py`)
2. `scripts/kit_export_character.py` → exports/character
3. `scripts/kit_anim.py` → animations + exports/anim
4. `scripts/kit_tools.py` → tools + exports/tools
5. `scripts/kit_facilities.py` → facility-kit + facility-points.json + exports/facilities
5-1. `scripts/kit_extra_clips.py` → 추가 동작 14개, `scripts/kit_equipment.py` → 장비 부품·손 장비 (그 뒤 2번 다시)
5-2. `scripts/kit_facilities_extra.py` → 추가 가구 62종(5번이 시설 키트를 새로 만들면 지워지므로 5번 뒤에 반드시 다시 실행)
6. `scripts/kit_preview.py` → preview (링크 조립)
7. `blender -b resident-preview.blend --python scripts/render_all.py` → previews/frames(장면마다 `-S <장면>`으로 별도 프로세스, 4개 병렬), 그 뒤 `uv run --with pillow python scripts/make_gifs.py`
   - 주의: 백그라운드 Blender는 활성 장면만 프레임 평가를 한다. 한 프로세스에서 여러 장면을 렌더하면 리그·경첩이 정지 자세로 찍힌다(2026-10-18 첫 GIF 94개가 이 문제로 정지 화면이었고 다시 렌더함).
8. `blender -b --python scripts/kit_validate.py` → validation.json

모든 스크립트는 빈 파일에서 시작해 원본을 읽기만 하므로 반복 실행해도 누적되지 않는다(기존 `tune.py`의 도구 회전 누적·`build.py` 중복 장면 문제 없음).
