# HORDE-VIS-1102 인계 — 2026-11-02

- **표시 전용 HordeVisual과 80무리×50몸체 검사 작성. 오프라인 컴파일 오류 0건. Unity 렌더링·GC·FPS는 미검증이다.**
- 신규 파일 3개만 작성했다. WildMonster/AntVisual/WaveDefense/LocalIncursions/Save/UI 및 기존 변경은 수정하지 않았다. Unity·캡처·커밋·Notion 실행 없음.
- 게임 방향 6.1의 후반 3000마리 이상·무리 체력 표현을 확인했다. 전투 규칙·실제 마릿수·저장은 Claude 담당이다.

## 연결 API
- namespace: AntColony.Units.
- public static HordeVisual Attach(GameObject host, System.Func<int> count).
- 호출 예: HordeVisual.Attach(gameObject, () => HordeCount).
- 호스트에 AntVisual이 생성된 뒤 연결한다. 비활성 호스트에도 Attach 가능하며 실제 준비는 활성 상태의 첫 LateUpdate에 한다.
- 중복 Attach는 기존 컴포넌트의 콜백만 교체한다. count는 호스트를 포함한 전체 살아 있는 몸체 수이며 매 프레임 한 번 읽는다.
- 추가 표시 수 = max(0, count() - 1). 기본 몸체는 호스트 AntVisual이 계속 표시한다. count 콜백 내부도 할당 없이 구현해야 한다.
- 신규 생성·저장 복원 경로의 Attach 연결은 Claude가 수행해야 한다. 기존 파일에는 연결 코드를 넣지 않았다.

## 메시·재질 경로
- AntVisual은 KemonoModel.Create로 Kemono/Upright/리그·스킨 부품을 만들며, 대체 자산은 QuirkyAnt다.
- 호스트 하위의 활성 MeshRenderer/SkinnedMeshRenderer에서 모델 최상위 자식을 찾는다. 꺼진 원래 큐브·숨긴 부품·선택 링·파티클은 제외한다.
- SkinnedMeshRenderer는 최초 사용 시 BakeMesh로 현재 포즈를 고정한다. 원본 Animator·모델·재질은 변경하지 않는다.
- 같은 원본 메시·활성 부품·재질·팔레트 조합은 공유 캐시 하나를 사용한다. 개체별 초기 포즈 차이는 캐시에 추가하지 않는다.
- 같은 원본 재질+색+텍스처별로 부품을 합친 뒤 단일 Mesh의 submesh로 구성한다. 팔레트의 MaterialPropertyBlock _BaseColor/_BaseMap도 복제한다.
- 재질은 원본 URP 재질의 복사본이며 enableInstancing=true. 원본 텍스처·셰이더·기타 재질 설정은 유지한다. 자산 파일 수정 없음.
- 서로 다른 재질을 한 셰이더/아틀라스로 강제 합치지 않았다. 렌더 호출 수는 무리 수×재질 submesh 수×배치 수다.
- 같은 조합을 쓰는 마지막 HordeVisual이 파괴되면 공유 메시·재질을 해제한다. 비활성화만 하면 재활성화를 위해 캐시를 유지한다.

## 위치·동작·비용
- 몸체마다 고정 나선 위치와 시드 위상을 사용한다. 위치는 무리 중심과 함께 이동하며 방향 전환으로 배치를 회전시키지 않는다.
- 반경은 약 0.45×sqrt(몸체 수)m, 최대 6m. 반경 상한 이후에는 원판을 같은 안정된 순서로 더 채운다.
- 마릿수 감소 시 마지막 인덱스부터 사라진다. 약 177몸체까지는 바깥쪽 우선이며 이후는 고정된 채움 순서다. 남은 몸체 위치는 재배치하지 않는다.
- 이동 방향으로 몸체가 향하며 걷는 동안 작은 y 흔들림·좌우 기울기만 적용한다. 추가 Animator·GameObject·Collider·Raycast 없음.
- 발 높이를 베이크 메시 바닥 기준으로 정규화하고 호스트 y를 사용한다. 경사면·계단에서는 추가 몸체가 뜨거나 파묻힐 수 있다.
- Graphics.RenderMeshInstanced에 재사용 Matrix4x4[511]을 전달한다. 일반 인스턴싱의 역행렬 상수 버퍼 한도를 고려해 1023보다 작은 배치를 사용한다.
- 정상 LateUpdate 경로에는 배열/LINQ/문자열 생성이 없다. 최초 메시 준비·캐시 미스·이전 최대 수를 넘는 count 증가 때는 할당한다. 위치 배열은 이후 재사용한다.
- 컴포넌트/호스트가 비활성 상태면 제출하지 않는다. 호스트 파괴 시 컴포넌트와 공유 자원을 정리한다.
- 추가 몸체의 공격·피격 포즈·손 도구·표정·갑옷 변화는 실시간 반영하지 않는다. 첫 준비 시 활성 외형의 고정 포즈에 걷기 흔들림만 제공한다.

## 검사 실행 — Claude
- 폐기 가능한 시작된 게임의 평화 구간, 기존 습격대 없음, 일시정지 해제 상태에서 await HordeVisual1102Checks.Main().
- SceneObjects.Any<LocalIncursions>()의 internal TrySpawnWaveGroup(20, false)을 리플렉션으로 4회 호출한다. 적 장수 없이 일반 호스트 80개를 생성한다.
- 각 호스트에 count=50을 연결해 총 4000몸체(호스트 80+추가 3920)를 요청한다. 실제 HordeCount/무리 체력은 건드리지 않는 시각 부하 검사다.
- Attach 중복 방지, 50→25→1 감소, 호스트 비활성 시 표시 수 0을 검사한다. 이 검사는 제출 요청 수 확인이며 실제 픽셀 확인은 별도다.
- 1배속·자동저장/위협·무력화 자동일시정지 임시 해제 → 1초 안정화 → 10초 Time.unscaledDeltaTime 평균/P95·생존 호스트 범위를 기록한다.
- 호스트가 죽어 80개 유지에 실패하면 고정 4000몸체 측정 불충족으로 표시한다. 감소한 결과로 목표 달성을 주장하지 않는다.
- finally에서 생성한 호스트만 Wound(1e9f)로 정리하고 설정·배속을 복구한다. 전투 피해·사체·드롭·포로는 실제로 발생하고 되돌리지 않는다. 저장하지 말고 게임을 폐기한다.
- 생성 위치가 본거지에서 약 45m 떨어진 여러 방향이다. 80무리가 실제 카메라 안에 보이도록 구도를 잡아 측정한다. 오프스크린 결과는 가시 부하 측정이 아니다.
- 동일 저장·시드·카메라·해상도·VSync·플랫폼으로 비교한다. Profiler에서 HordeVisual.LateUpdate의 GC Alloc, 렌더 스레드/GPU 시간, Draw Calls, 그림자·submesh 비용을 확인한다.
- 검사 async/생존 수 순회 비용은 전체 프레임 수치에 포함된다. 런타임 count 증가·캐시 생성 구간과 정상 상태 GC를 구분해서 확인한다.
- 별도로 단일 무리 count>1023의 다중 배치, 피해에 따른 감소, 비활성/파괴, 장거리 이동·회전·일시정지·경사면·그림자·빌드의 셰이더 인스턴싱 변형을 확인한다.

## 검증·제약
- Unity 6000.5.8f1 내장 Roslyn, 전체 게임 소스 514개+HordeVisual1102Checks 오프라인 컴파일: exit 0 / 오류 0 / 기존 경고 33.
- Bee의 Unity DLL/define과 Library/ScriptAssemblies 참조 101개를 사용했다. 출력은 프로젝트 밖 임시 폴더에만 생성했다.
- 로그: C:\TESTTEMP\ant-hordevis1102-e994a46b79df4d259d95a7557621f6cc\compile.log.
- Unity 6000.5의 GetEntityId API 사용. 정적 MeshRenderer 대체 모델은 메시 Read/Write가 필요하며 꺼져 있으면 한 번 경고 후 표시 컴포넌트를 끈다. 스킨 메시의 BakeMesh 경로는 이를 요구하지 않는다.
- 원본이 URP와 호환되고 인스턴싱을 지원하는 셰이더여야 한다. 플랫폼 미지원·준비 실패도 경고 후 끈다. 실제 플레이어 빌드의 인스턴싱 셰이더 변형 포함 여부는 확인해야 한다.
- 단일 메시로 합쳐도 원본 몸체의 정점·삼각형 수는 줄지 않는다. 4000몸체의 GPU 비용·다중 재질 draw 수·그림자가 남은 위험이다.
- 코드 분석 사실: 몸체별 Animator/물리 없음, 메시 공유·행렬 재사용·안정된 인덱스 제거.
- 미검증 예상 효과: 개별 GameObject/Animator 복제보다 낮은 CPU 비용. 정상 상태 GC 0B·평균/P95·실제 외형·4000몸체 성능 충족은 아직 측정하지 않았다.
- 카카오톡 완료 알림 도구는 현재 세션에 없다.