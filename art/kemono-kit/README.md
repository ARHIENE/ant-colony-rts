# 조합 부품 렌더 시안

## 현재 범위

미간을 좁힌 `art/characters/kemono-con-face/rabbit-con-face.blend`를 기본 얼굴로 사용한다. 얼굴 비율을 새로 설계하지 않았다. 기본 의상은 얇은 천옷 `LINEN` 한 종류다. 갑옷·망토·바지 등 의상 변형은 최신 사용자 요청에 따라 제외했다. 장비는 이후 별도 작업이다.

**현재 납품은 조합할 부품과 수동 조합 예시의 Blender 렌더다. Unity 캐릭터 생성·랜덤 조합·게임 적용은 구현하지 않았다.**

## 보는 파일

- `kemono-parts-boards.blend`: 머리·귀·표정·색상·겹침 효과·꼬리·상처 등 9개 보드. 장면 목록에서 분야를 선택한다.
- `kemono-character-kit.blend`: 기본 캐릭터가 보이는 수동 부품 파일. `Part_범주_선택지_번호` 오브젝트로 분리돼 있다.
- `previews/combinations.png`: 같은 천옷을 입힌 조합 예시 6개.
- `previews/hair.png`, `ears.png`, `expressions.png`, `colors.png`, `overlays.png`, `tails.png`, `injuries.png`: 분야별 비교 렌더.
- `previews/hair-parts.png`, `ear-parts.png`: 얼굴을 제외하고 독립 머리·귀 부품만 보여주는 렌더. 완성 캐릭터와 부품을 혼동하지 않도록 추가했다.
- `parts-manifest.json`: 슬롯별 부품 목록. `combinations.json`: 수동 조합 예시의 선택값.

## 조합 규칙

| 범주 | 선택지 | 사용 방식 |
|---|---:|---|
| 기본 몸·천옷 | 각각 1 | 항상 사용 |
| 머리 | 6 | 하나 선택 |
| 귀 | 5 | 하나 선택 |
| 꼬리 | 3 | 하나 선택 |
| 색상 | 6 | 머리·귀·꼬리에 같은 팔레트 적용 |
| 얼굴·표정 | 17 | 하나 선택, 기본 얼굴과 동시에 겹치지 않음 |
| 눈물·땀·홍조 | 3 | 표정 위에 독립적으로 겹침 |
| 흉터·긁힘·멍·반창고 | 4종 × 6부위 = 24 | 표정과 별개로 여러 부품 사용 가능 |

표정은 기본·기쁨·놀람·화남·아픔·졸림·잠듦·풀이 죽음·뿌듯함·호기심·당황·맛있음·공허함·울먹임·울음·긴장·사망이다. 상처 부위는 이마·볼·몸통·왼팔·오른팔·다리다. 머리나 옷에 가려지는 부위는 실제 착용 상태에서 일부 안 보일 수 있다.

## 제작·검증

- 도구 보정(2026-10-18): 창 원점·도끼·망치·곡괭이 형상 수정. 해당 도구 사용 29장면에 오른손 쥐기, 27작업 장면에 손목 IK·표면 타격 지점·낮은 작업물의 발 고정 자세 적용. `scripts/tool_contact.py`는 Blender 미리보기 보정이며 공유 동작 FBX에 구워 넣은 결과가 아니다. Unity용으로 내보낼 때는 제약 조건 베이크가 필요하다. `contact-validation.json`과 `tool-head-validation.json`에 검사 결과를 기록한다. 원본 백업은 `*.before-contact.blend`, `*.before-grip-fix.blend`.

- 원본 얼굴 정점과 기본형의 최대 좌표 오차 약 `3e-8`: 미간 보정 비율 유지.
- 60개 부품 선택지. 가중치가 없는 정점 및 존재하지 않는 뼈 참조는 0개.
- 실제 부품은 103개 메시로 나뉘며, 같은 선택지에 포함되는 좌우 귀·눈물·입 선 등은 함께 켠다. 비교 렌더용 캐릭터는 납품 부품을 합친 메시가 아니다.
- 기존 키트와 원본 시안은 덮어쓰지 않았다.
- 기존 Kiki 에셋에서 파생한 메시·텍스처를 사용한다. 독자 제작 원본 모델로 취급하지 않는다.
- `scripts/build.py` → `scripts/boards.py` → `scripts/save_openable.py`로 재생성한다. Blender 및 원본 Unity 에셋 파일이 필요하다.
- `kemono-preview.blend`, `kemono-animations.blend`, `exports/`는 범위 정정 전에 만든 리그·동작 실험 자료다. 최종 표정 보정 이후 FBX는 다시 내보내지 않았으며 게임용 납품·Unity 검증 완료 자료가 아니다. 현재 확인 기준은 위 렌더와 부품 보드다.

## 리그·동작·상호작용 (2026-10-18 Claude 인수, 사용자 지적 '리그가 캐릭터랑 안맞음' 수정)

- **리그 정리(`common.py`):** Kiki 원본 뼈 중 이 체형 밖으로 튀어나온 뼈 제거: 끝 뼈(`_end`)·치마 물리 뼈 12개·`Accesstory`·`shoulder Dress`. 98 → **58뼈**. 가중치는 이관(`parts.skirt_to_legs`): 치마는 허리=골반 → 밑단=허벅지로 섞어 걸음마다 밑단이 다리를 따라감, `shoulder Dress`→`shoulder`, `Accesstory`→`spine_02`.
- **귀·꼬리 뼈:** 실제 토끼귀·꼬리 메시에서 뿌리 위치 계산. 캐릭터 왼쪽 = +X(`upper_arm.L`과 같은 규칙)로 `Ear.L/R` 이름 정정.
- **소켓:** `Socket.Carry` 가슴 바로 앞(y −0.17), `Socket.Tool.*` 손바닥 중앙, `Socket.Hat` 머리카락 꼭대기(1.17).
- **동작(`motions.py`, 2026-10-18 오후 재작성):** 예전 주민 레시피를 옮겨 쓴 동작은 관절이 몇 도씩만 움직이고 등속(LINEAR)이라 '조금조금' 움직였음(사용자 지적) → 실제 동작처럼 키 포즈 + Bezier 가감속으로 44개 전부 새로 작성. 팔은 A포즈 대신 자연스럽게 내림, 상체 굽힘은 척추 3뼈에 분산, 무릎·골반이 같이 움직임. 힘쓰는 일(도끼·곡괭이·망치)은 예비 동작→빠른 타격→멈춤→복귀, 느긋한 일(쉬기·목욕·그네·잠)은 느리게. 길이도 실제 속도에 맞춤(예: Chop 40·Rest 96·Sleep 96프레임). `animations.py`는 `motions.create`를 부른다. 동작만 다시 만들 때 `scripts/remotion.py`(`kemono-animations.blend` + `clips.json` 갱신). 축 규칙은 `motions.py` 머리 주석.
- **검증(`validation.json`):** 기본 조합 메시 범위 밖 뼈 0, 가중치 중심과 뼈 거리 3cm 초과 0(통째로 붙은 머리·귀·꼬리는 뿌리가 부품에 닿는지 확인), 가중치 없는 정점 0.
- **빌드 크래시 수정:** Blender 5.2는 `bpy.data.libraries.write`로 Scene을 부분 저장하면 크래시(`BKE_view_layer_copy_data`) → 리그·컬렉션 단위 저장, 미리보기는 `save_as_mainfile(copy=True)`.
- **재생성(헤드리스):** `blender -b --factory-startup --python scripts/run.py`(Kiki 참조 장면 재구성 + build) → `boards.py` → `save_openable.py` → `export.py`(부품 FBX 1 + 동작 FBX 44).
- **상호작용 미리보기:** `scripts/preview.py` → `kemono-preview-interactions.blend`(188장면: 시설 138 + 외형 교체 6 + 동작 44). resident-kit 시설·도구·`facility-points.json`을 **링크만** 하고 수정하지 않는다. 시설이 예전 주민(키 2.07) 기준이라 캐릭터를 1.6배로 놓고(손 높이 0.69), 팔이 짧아 서는 위치를 시설 쪽으로 0.35 당김, 앉기 엉덩이 높이 0.60. 이 공통 배치 뒤에 **겹침 보정(`clearance`)**: 렌더하는 모든 프레임에서 몸과 시설 삼각형 교차(BVH)를 검사해 앉기는 좌석 위로 앞뒤·높이 조정(무릎이 좌판 앞 끝 밖으로), 그네는 높이만, 눕기는 몸통·다리 기준(머리·귀는 베개에 눌려도 됨), 서서 하는 일은 올라온 바닥 위로 + 뒤로/옆으로 비켜 섬(손은 작업 접촉이라 제외), 운반물은 가슴 앞으로. 물(목욕통·온천)은 제외. 결과는 장면 속성 `clear`. 검사 `scripts/overlap.py` → `previews/overlap/*.json`. 렌더 `scripts/render_all.py`(장면별 `-S` 프로세스, 2프레임마다) → `previews/frames`, GIF `scripts/gifs.py`(실시간 83ms) → `previews/gif`, 노션 묶음 `previews/notion`.
- **남은 것:** Unity 미연결. 상호작용은 미리보기 배치(손이 정확히 작업 지점을 잡는 IK 없음). 남은 의도적 접촉: 손-작업물/운반물, 머리카락·귀-베개, 몸-물. 장비 착용 부품은 이 체형용으로 아직 없다.

개발 일지 작성·커밋·push를 수행하는 SAVE 작업은 실행하지 않았다.
