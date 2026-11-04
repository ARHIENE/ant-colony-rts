# 생물 키트(critter-kit) — 목장 4종 + 습격 적 2종

2026-10-18 Claude. 기획(자원 시스템·목장)에 있는 사육 생물 4종이 Unity에서 **임시 캡슐**(`World/Critter.cs` Spawn)로만 표시되던 것을 Blender 모델·리그·동작으로 만들었다. 같은 방식으로 습격 이벤트 적(`EventActor` 말벌·밤 포식자, 역시 캡슐)도 추가했다. Unity 연결은 아직 하지 않았다.

## 파일
| 경로 | 내용 |
|---|---|
| `critter-kit.blend` | 6종 각각 리그 `<종>Rig` + 메시 + 동작 `<종>|<동작>` |
| `exports/<종>.fbx` | 리그 + 메시(휴식 자세) |
| `exports/<종>@<동작>.fbx` | 리그만 + 동작 1개 |
| `critter-manifest.json` | 본 목록·메시 수·동작 길이 |
| `previews/gif/<종>@<동작>.gif` | 움직이는 미리보기 43개 |
| `scripts/critters.py`, `kit_critters.py`, `render_critters.py`, `make_gifs.py` | 재생성 |

## 규격
- 주민 키트와 같은 단위(주민 키 약 2.24), 정면 -Y, 24fps, Generic, 강체 가중치(부품별 한 본).
- 외형: 주민과 같은 둥근 로우폴리, 큰 검은 눈 + 하이라이트, 볼 홍조.
- 크기(몸길이): 진딧물 ~1.3, 누에 ~1.7, 거미 ~1.5(다리 포함 폭 넓음), 벌 ~1.4. **새끼는 같은 자산을 게임에서 약 0.6배**로 쓰는 것을 제안(새끼 전용 모델 없음).

| 종 | 본 | 동작 |
|---|---|---|
| 진딧물 Aphid | Root, Body, Head, Antenna.L/R, Leg1~3.L/R | Idle, Walk(삼각 보행), Eat, Lie(쓰러짐·휴식), Bound(결박 몸부림), Hurt, Death(뒤집힘) |
| 누에 Silkworm | Root, Seg0~4, Head | 위와 같음, Walk = 몸마디 물결 기어가기 |
| 거미 Spider | Root, Body, Abdomen, Head, Leg1~4.L/R | 위 + Attack(달려들기) |
| 벌 Bee | Root, Body, Abdomen, Head, Antenna.L/R, Wing.L/R, Leg1~3.L/R | 위 + Attack(침), Fly(날갯짓 + 떠 있기) |
| 말벌 Wasp(적) | 벌과 같은 구조, 화난 눈썹·큰 침 | Idle, Walk, Fly, Attack, Hurt, Death |
| 밤 딱정벌레 NightBeetle(적, 밤 포식자) | Root, Body, Head, Leg1~3.L/R, 빛나는 붉은 눈(발광 재질) | Idle, Walk, Lie, Attack, Hurt, Death |

- 게임 상태 연결 제안: 배회 = Walk, 먹이통·방목 = Eat, 경상/중상·수면 = Lie, 포획·결박·운반 = Bound, 피격 = Hurt, 사망·도축 대상 = Death 마지막 프레임 유지, 거미·벌 공격 = Attack, 벌 이동 = Fly.

## 검증·한계
- 6종 모델·리그·동작 생성, FBX 내보내기, 동작별 12프레임 렌더로 눈으로 확인했다.
- FBX 재불러오기 수치 검사는 주민 키트와 달리 **하지 않았다**. Unity 임포트·재생은 미검증.
- 다리는 엉덩이 본 하나로 통째로 움직이는 단순 구조다(관절 굽힘 없음). 털·날개 반투명은 단색 재질과 알파.
- 아직 없는 것: 보스(기존 MiniBird 자산 사용 중), 야생 전용 종, 동물 표정 교체.
