# production-kit-20261019 — BLENDER-1019 (2026-10-19 확정 기획 Blender 제작)

요청서: `docs/CLAUDE_BLENDER_2026-10-19_REQUEST.md`. 범위는 Blender 원본·분리 자산·애니메이션·내보내기·검수 렌더. Unity 코드/씬/프리팹·HUD·시뮬레이션·Notion·커밋은 하지 않음.

## 1. 결과물 한눈에

| 분류 | 수량 | 원본 .blend | FBX | 미리보기 |
|---|---|---|---|---|
| 자원·식품·요리·술 (부록 A 전체) | 305종 × 단품/운반·적재(305)/부패(205)/씨앗봉투(82) | `pk-resources.blend` | `exports/resources/<id>.fbx` (변형 4종 한 파일) | `previews/png/resources/`, 보드 `resources_<분류>.png` |
| 밭 작물 성장 | 씨앗 작물 82종 × 5단계 | `pk-crops.blend` | `exports/crops/Crop_<id>.fbx` | `previews/png/crops/`, 보드 `crop_stages.png` |
| 도구·무기 (재질 단계) | 도구 6종×7단계 + 검·창·방패×6 + 칼집×6 = 66 | `pk-handheld.blend` | `exports/handheld/` | 보드 `tier_comparison.png` |
| 장착 부품 | 개방형 투구 5(귀 유형별)+단계 장식, 판갑·등판·허리·허벅지판·보호대·정강이판·가죽조끼·벨트, 무기 거치대 3, 갑옷 착용 파생(머리 LONG, 천옷) | `pk-equipment.blend` | `exports/equipment/` | 보드 `equipment.png` |
| 신규 시설 | 작업장 8 · 식품 4 · 온실 모듈 5 · 채취 5 · 유체/에너지 5 · 배관 모듈 14 · 매장 지점 10종×3상태 · 지하 범위 표시 4 = 75 | `pk-facilities.blend` | `exports/facilities/<id>.fbx` + 동작 `<id>@<take>.fbx` | 보드 `facilities_*.png` |
| 기존 시설 재사용 | 조리(Kitchen·Hearth)·목공·석재·제련로·가마·냉장고·냉동고·퇴비통·목재 발전기/전선/배터리 | `art/resident-kit/resident-facility-kit.blend` | 기존 `resident-kit/exports` | — |
| 신규 동작 | 38 클립(+ 기존 Kemono 클립 재사용 표 `manifest.json › motions_reused`) | `pk-motions.blend` (`PK|<clip>`) | `exports/animations/Kemono@PK_<clip>.fbx` | `previews/gif/close/`, `previews/gif/game/` (24 fps) |

- 목록·좌표·지점은 `manifest.json`, 도감 인덱스(분야→품목→이미지·설명·파일)는 `CATALOG.md`, 기존 자산 조사표는 `SURVEY.md`.
- 원본 라이브러리(`kemono-kit`, `resident-kit`, `characters`)는 수정하지 않음. 캐릭터는 `rig_adapter.py`가 사본으로 불러와 소켓만 추가·이동.

## 2. 공통 규칙 (엔진 연결 기준)

- 단위: 1 Blender unit = 1 m. 캐릭터(kemono rig) 키 ≈ 1.2 m. **격자 1칸 = 1 m**(배관 모듈·시설 칸).
- 축: Blender +Z 위, 캐릭터·시설 정면 −Y. FBX = axis_forward −Z / axis_up Y, leaf bone 없음(`kemono-kit/scripts/export.py`와 동일).
- 원점: 바닥 물건 = 바닥 중앙. 손에 드는 물건 = **쥐는 점(주먹 중심)**, +Y = 머리/날 쪽, 타격면 −Z. 시설 = 바닥 면적 중앙.
- 소켓: `Socket.Tool.R/L`을 **주먹 구멍 중심으로 23 mm 이동**(손잡이 반지름 7 mm 기준, 손가락 감싸기 240°, 피부 오차 −3~0 mm). `Socket.Hip.R`(검 고리), `Socket.Back`, `Socket.Helm`(=Socket.Hat). 방패는 가운데 손잡이식: 손잡이 막대가 주먹을 관통하는 +Y, 방패 앞면 −Z(주먹 마디 방향).
- 강체/연성: 무기·도구·판갑·투구는 단일 뼈 부모(휘지 않음, 동작 중 길이 변화 0.000 mm 측정). 천옷·끈만 스키닝.
- 시설 지점(Empty, +Y = 바라보는 방향): `WorkPoint_0` 서는 곳, `HandTarget` 손 접촉, `InputPoint`/`OutputPoint` 투입/배출(완성품은 옆 바닥), `FluidIn_L/G`·`FluidOut_L/G` 배관 포트(L 액체 파랑·굵음 / G 기체 노랑·가늚), `PowerIn/Out`, `SeatPoint`, `ControlPoint`, `ChargePoint`(드럼·압축 용기), `CarryPoint`, `Select`(엔진 클릭 영역 상자).
- 시설 상태: 탱크 `Level_Empty/Partial/Full`(하나만 표시), 양조통 `Airlock_Fermenting`, 가마 `Glow_Running`. 가동 = `<id>@Run` 재생, 정지 = 정지 자세.
- resident-kit 자산은 resident 단위 → kit m 변환 ×0.625.

## 3. 조합 방법

1. 캐릭터: 기존 kemono 머리·귀·꼬리·표정·색 조합 그대로(`rig_adapter.combo`). 얼굴·체형 변경 없음.
2. 장비: 투구는 **귀 유형과 같은 `Helm_<귀>`**를 head 뼈에 부착(귀 구멍 위치가 유형별로 다름, 하나의 구멍으로 전부 지원하지 않음). 판갑 등은 `bone` 속성의 뼈에 강체 부착, 재질 단계 = 재질 교체 + `<부품>_Trim_<단계>`.
3. 갑옷 착용 시: `Part_Hair_LONG` → `Part_Hair_LONG_ARMOR`, `Part_Outfit_LINEN_00` → `Part_Outfit_LINEN_ARMOR`, `Part_Body_BASE_00` → `Part_Body_BASE_ARMOR`로 교체(판 아래 천은 몸 쪽으로 붙이고, 판에 완전히 가려지는 천·피부 면은 삭제한 파생. 웨이트 유지, 귀·꼬리는 그대로). 가슴판·등판은 위판(`spine_02`)과 아래 미늘판(`*_Lower`, `spine_01`) 두 장을 함께 붙임.
4. 무기 휴대: `previews/carry-placement.json`의 위치·부모 뼈 + 거치대(`Holder_Sword/Shield/Spear`). 손으로 옮기는 순간은 클립 이벤트 프레임(아래).
5. 동작: 공용 클립 + 시설별 보정. 시설은 `manifest.json › facilities.<id>.contact`(클립·이벤트 프레임·남는 높이 차)를 사용해 서는 위치를 맞추고 남는 차이는 엔진 손 IK로 보정.

## 4. 재생성 순서 (`art/production-kit-20261019`에서)

```
B="F:/Blender/blender.exe -b --factory-startup --python"
$B scripts/build_equipment.py          # 장비·투구·파생 부품
$B scripts/fit_check.py -- search      # 휴대 위치 탐색 + 겹침
$B scripts/build_holders.py            # 무기 거치대
$B scripts/fit_check.py                # 겹침(기존 클립)
$B scripts/build_handheld.py           # 도구·무기 라이브러리
$B scripts/build_motions.py            # PK 클립 + 해석 IK 베이크(양손 도구: 상체 비틀기·도구 위치 탐색, 몸통·머리·판 회피) + 측정 → previews/motion-report.json
$B scripts/fit_check.py -- motions     # 겹침(기존 + 신규 클립) → previews/fit-report-motions.json
$B scripts/build_resources.py ; $B scripts/build_crops.py
$B scripts/build_facilities.py ; $B scripts/contact_check.py ; $B scripts/build_facilities.py   # 서는 위치 보정 반영
$B scripts/render_items.py -- <resources|crops|facilities|handheld|equipment>
$B scripts/render_motion.py -- close   /  -- game <clip...>
uv run --with pillow python scripts/gifs.py ; uv run --with pillow python scripts/boards.py
$B scripts/export_fbx.py -- <animations|handheld|equipment|resources|crops|facilities>
$B scripts/reimport_check.py -- <같은 분류>
py scripts/build_manifest.py           # manifest.json / CATALOG.md / SURVEY.md
```

## 5. Blender ↔ 엔진 경계

- Blender: 모델·상태 메시·지점·선택 영역·공용 클립(베이크됨)·이벤트 프레임·시설별 접촉 보정값.
- 엔진: 부모 전환(허리↔손, 손→바닥, 바닥→머리)을 이벤트 프레임에 수행, 도구 표시/숨김, 손 IK 잔차 보정, 재고·수리·예약·피해·발효 시간·취기 수치, 유체 흐름, 탐사 확률. Blender에서 이를 흉내 낸 것은 없음.

## 6. 상태 (2026-10-24 Codex 재검증)

- 캐릭터 Unity용 FBX: 117부품·60뼈·판갑 11개. 판갑은 같은 리그의 단일 뼈 웨이트로 내보내며 `_ARMOR` 몸·천옷·긴 머리 파생과 함께 전환한다. `scripts/export_character.py -- --model-only`, `scripts/check_unity_character.py` 참조. 재가져오기 검사 통과.
- 재가져오기: PK 동작 44, 시설 112(본체+동작), 바닥 소품 13 — 실패 0. 이번 검증에서 도구 71·장비 29 전체 재검사는 하지 않았다.
- 시설 접촉: `contact_check.py` 재실행, 18곳 모두 오차 0~2.1 cm. 기존 4곳 4~5.4 cm 기록은 최신 원본과 달라 교체했다. Unity 시설 접근 위치·손 접촉 검증 결과는 아니다.
- `repair_wrist_roll.py`: 손 위치·타이밍을 유지하며 손잡이 회전을 ±45° 범위에서 탐색한다. 손목 <60°, 기존 대비 3° 이상 개선, 도구 관통 증가 없음, 왼손 grip2 <1 mm, 강체 길이 변화 <0.01 mm인 후보만 원본에 저장한다. 곡괭이 61.8°→56.1°, 삽 78.0°→53.7° 적용. 괭이 후보 67.2°는 기준 미달로 거부하여 기존 69.8° 유지. 상세 `previews/wrist-repair-1024.json`. `build_motions.py`로 처음부터 재생성하면 이 후처리를 다시 실행해야 한다.
- 겹침 검사: 최신 동작으로 `fit_check.py -- motions` 재실행(1230프레임·3프레임 간격). 판갑·보호대·투구 장식·거치대 등 겹침이 남아 있다. `previews/fit-report-motions.json` 참조. 도구 관통 증가 없음은 관통 0을 뜻하지 않는다.
- 색상: `export_unity_materials.py`가 Blender 천옷 재색칠 그래프를 sRGB/linear 변환과 함께 픽셀로 옮긴다. `Assets/Resources/Kemono/Textures/Linen_Baked.png` 및 원본 단색 `SourceMaterials.json`. 머리·귀·꼬리는 Unity `KemonoAppearance`에서 기존 6색을 적용한다(귀 안쪽 제외).
- 렌더 검토: 보정된 삽·곡괭이의 중간/타격 프레임을 기존 `render_motion.py`로 렌더해 확인했다. 전체 동영상·게임 Play 검증은 하지 않았다.
- **Unity 미검증:** 공식 CLI 경로 Access denied 및 MCP 자동 승인 거절. Configure Import + Animator 메뉴 재실행, 실제 머티리얼·팔레트·표정·도구 방향/크기·단궁 발사 프레임·판갑·소품·시설 16종 화면·캐릭터 키 1.6 비교 검증이 남아 있다. 바닥 보존식 모델은 리소스 반입만 했고 게임 연결은 미완료다.