# 진행 중 — 무리 전 동작/탑승부/발 접촉

Unity는 캠페인 채팅 소유. Assets 편집·Unity 조작은 반환 전 금지.

- 런타임 OrganicMotionBank/OrganicHordeMotion/OrganicGroundContact 및 HordeVisual/OrganicEnemyVisual 적용, 컴파일0 확인. 아직 bank 에셋 없어 기존 static 경로 임시유지.
- Blender 후보 refined17종 생성, 접지 가능한15종 Move + 탑승형2종5클립 보정. 원본 아직 미변경.
- contact-audit.json17종 완료, review 이미지는 RAM부족으로 생성 전 소유Blender종료.
- 추가 보폭 축소 필요7종: WallRam, AcidSpitter, BroodAlien, SiegeBrooder, VeilWeaver, BloomMender, BroodCarrier. refine_contacts.py에 축소수치/선택인자 구현했으나 재실행 전. 예 `blender --factory-startup -b -t 2 --python .../refine_contacts.py -- WallRam AcidSpitter ...`.
- 이후 audit_contacts.py 재검사, HuntRider/SiegeRider/ClawHunter render_review.py 3장 육안확인 필요.
- Candidate profile horizontalExtent는 *-rig.json MESH dimensions x/y최대로 채웠음. 7종 재생성 후 다시채우기.
- Unity 반환 후 pending small patches: Shader target3.5+instancingforcemaxcount250, runtime Batch/Remains split250, Animator Move속도 cap8유지(실제 지지발 속도 베이크 측정), HordeVisual bank로드확인에 organicMotion!=null short-circuit, MotionBank PreferBinarySerialization.
- refined/*.fbx와 blend를 백업후 art/Assets동일FBX에 반영(.meta보존). unchanged FBX는 유지. 재컴파일/임포트.
- BakeOrganicMotion.cs Main(name) Unity Edit에서17종 실행 → Resources/OrganicEnemies1114/<kind>/HordeMotion.asset(mesh+RGBAHalf position/normal+material). 리그원본 SampleAnimation24fps를 2400triangle cluster mesh에 베이크. 은폐/공격등86클립.
- verify_banks.cs와 OrganicHordeLiveChecks.cs 독립컴파일0. 후자는Play isolatednewgame,17종5extra each/지면/상태변경/부분손실death/전멸death 검사. 실제shader재생영상도 필수.
- 쉐이더 데이터샘플/groundcontact 스키닝단위 오류가능성 실측 필요. 동작완성으로 보고하지 말 것.
- 모든Assets코드작업끝난뒤graphify update(다른채팅갱신과조율),docs/README/board최신화,Unity반환.

추가후보: WallRam 앞방향이 Blender(-.56,-.83), Unity(-.56,0,.83)로34도틀어짐. OrganicEnemyVisual.Create에서 root yaw+34도(부모로SetParent하기전) 적용하고bank도동일방향베이크. 땅파기burrowOffset은별도필드로MoveTowards하여기존부드러운진입복원.

## 19:59 이후 재개 확인
- refine_selected.py 7종 재생성 및 전체17종 감사 완료. 최종 23클립 변경 후보. HuntRider/SiegeRider/ClawHunter 렌더3장 육안확인 완료. 이미지에서 메시 폭발/바닥 뚫림 없음. 미세한 실제 재생 품질은 Unity 검증 필요.
- finalize_candidates.py 실행: pending-runtime에4파일 최종 후보+원본 SHA 준비. Assets 미수정. apply_refined.py는 SHA와원본경로 확인 후 백업/반영하도록 작성했으나 아직 실행하지 않음.
- bake-compile.rsp 최신 BakeOrganicMotion 독립컴파일 종료0.
- 무리 클립17개 bank 생성/실재생은 아직 미검증. Unity는 캠페인 채팅 소유 유지.
- 19:57 부모 없는 동일 read-only Git diff 103개 재발 취소. 파일/이력 미변경. 기록 orphan-diffs-cleaned.json. 재발하면 exact command+parent absent 조건 재확인 후 해당 조회만 취소.
- 캠페인 채팅: 본선 귀환/재출전/저장재로드/승리검사 통과 보고, 보스 임시 표시/카메라/HUD 검수 후 반환 예정.
