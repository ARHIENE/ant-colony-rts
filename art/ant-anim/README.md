# 개미 작업 애니메이션 (ART-ANIM-01)

요청: `docs/COLONY_INTERACTION_ANIMATION_REQUEST.md`. 대상 리그: Unity에 있는 `Assets/ThirdParty/Quirky Series/QuirkyAnt`(Generic, 본 12개, 다리 6개 각 1본). 원본 FBX는 읽기만 하고 덮어쓰지 않음.

## 산출물
- `ant-work-clips.blend` / `exports/Ant_WorkClips.fbx`: 같은 `Rig` 계층에 새 클립 17개(24fps). FBX에는 새 클립만 들어 있음. Unity에서 Avatar를 QuirkyAnt와 같은 Generic으로 쓰면 됨.
- `ant-tools.blend` / `exports/Ant_Tools.fbx`: 도구 8종 `Tool_Hammer/Axe/Pickaxe/Saw/Pestle/Scalpel/Spoon/Stamp`. 원점 = 쥐는 곳, 손잡이 +Z. 재질은 colony-core 팔레트.
- `previews/`: 핵심 프레임(옆·3/4). `scripts/`: `build_clips.py`(클립), `poses.py`(포즈 정의), `build_tools.py`, `render_poses.py`, `attach_preview.py`.

| 클립 | 길이(f) | 루프 | 문서 역할 |
|---|---|---|---|
| StandEnter / StandExit | 7 / 8 | | 직립 전환(.3s) |
| StandIdle | 24 | ○ | 직립 대기 |
| Pickup / PutDown | 16 / 16 | | 숙여 집기→앞다리 받침 / 내려놓기 |
| Carry | 10 | ○ | 기존 Walk + 앞다리·머리 운반 자세 |
| GroundWork | 24 | ○ | 낮게 숙여 교대 긁기(진폭 변주) |
| BenchWork | 24 | ○ | 직립, 한 다리 고정·한 다리 손질 |
| Hammer / Chop / Mine | 20 / 24 / 24 | ○ | 들기→접촉(12·14·13f)→반동 |
| Treat / Operate | 32 / 32 | ○ | 숙여 처치(맨손) / 직립 정밀 수술(도구) |
| Sleep / Rest | 48 / 40 | ○ | 다리 접고 엎드림·호흡 / 앉아 더듬이 손질 |
| Mourn | 36 | | 고개·더듬이 숙이기 |
| Inspect | 40 | ○ | 고개 좌우·앞다리로 짚기 |

기존 클립 재사용: Idle_A/B/C, Walk, Run, Eat, Sit, Death 등.

## 도구 부착(Blender 기준 값)
`leg.01.R` 본 자식, 본 공간 위치 (0, .022, 0), 회전 (-90°, 0, 0) → 손잡이가 앞다리 연장 방향. 본 tail보다 다리 메시가 길어 .022 앞으로 뺌. Unity 축 변환 후 값은 임포트해서 다시 맞춰야 함.

## 시설 작업 지점·경첩 (2026-10-17, `art/colony-core/`)
- `scripts/interaction_spec.py`: 79개 모델 → 클립·도구·추가 지점 매핑(요청서 기준 제안값). 성장 중 작물(Young/Growing)·산성탑은 개미 동작 없음.
- `scripts/build_interaction.py`(colony-core.blend 연 상태에서 실행) → `colony-interaction.blend`(원본은 그대로), `interaction_points.json`(루트 로컬 좌표·바라볼 방향, 앞 = -Y), `exports-interaction/*.fbx`(냉장고 문·냉동고 뚜껑·문틀·목장문·성문 양쪽 `OpenClose`, 산성탑 `Aim`, 48f).
- Blender 안 Empty 이름은 전역 중복이라 `WorkPoint_0.045`처럼 접미사가 붙음 → Unity는 JSON 키(`WorkPoint_0` 등)를 기준으로 쓸 것.
- `scripts/render_interaction.py` + `make_gifs.py`: 개미(배율 4)를 지점에 세워 클립·도구·경첩 동시 재생한 도감용 GIF `previews/interaction/<id>.gif`. 원본 QuirkyAnt 클립(Sit/Carry/Eat/Walk 등)은 Rig 오브젝트 위치를 0으로 고정하는 키가 있어 미리보기에서만 제거함(Unity 배치 시에도 Rig 루트 위치 키 주의).

## 한계·미완
- 다리당 본 1개라 무릎·발끝 IK 없음. 직립 시 배가 바닥을 받치는 삼각 자세로 표현.
- 접촉 프레임 이벤트, 파티클·소리, 작업 지점 예약은 아직 없음. 지점 위치는 바운딩박스 기반 초기값(Unity에서 실제 충돌·NavMesh 기준 재조정 필요).
- **Unity 연결 안 됨**: Animator 상태 추가, `AntVisual`의 실제 작업 상태 → 클립 매핑, 도구 부착·해제, 예약/중단/저장 동기화는 다음 단계.
