# 프로젝트 로그

## 현재 상태 — 2026-10-18 SAVE 2(Claude) — 토끼 조합 키트 리그 수정·동작·시설 상호작용, Codex 작업 인수
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 이전 기록은 changelog.md. 공동 작업 보드 `docs/IMPLEMENTATION_BOARD.md`.
- **캐릭터 방향:** Codex의 미간 보정 토끼 얼굴 캐릭터(`art/characters/kemono-con-face`, Kiki 에셋 파생)를 조합형 키트로 만든 `art/kemono-kit`(KEMONO-MODULAR)이 최신. 이전 동물형 `art/resident-kit`은 시설·도구·작업 지점 원본으로 계속 사용.
- **KEMONO-MODULAR(Codex → Claude 인수):** Codex가 부품(머리 6·귀 5·꼬리 3·색 6·표정 17·겹침 3·상처 24)을 만들다 빌드 크래시 + 사용량 소진으로 중단 → Claude 인수.
  - 크래시 원인: Blender 5.2 `bpy.data.libraries.write`로 Scene 부분 저장 시 크래시 → 리그·컬렉션 저장 + `save_as_mainfile(copy=True)`. 라이브 Blender 대신 헤드리스 `scripts/run.py`(Kiki 참조 장면 재구성 + build).
  - 리그(사용자 지적 '안 맞고 튀어나옴'): 끝 뼈·치마 물리 뼈·장식 뼈 제거 98→58뼈, 치마 가중치 골반→허벅지 섞기, 귀·꼬리 뿌리 메시 기준, 귀 좌우 이름(+X=L), 소켓 재배치. build에 뼈 맞춤 검증 추가(밖 0·어긋남 0).
  - 동작 44: 걷기 보폭 1.6배·팔 1.8배·무릎 방향 수정. FBX 부품 1 + 동작 44(`exports/`).
  - 상호작용: `scripts/preview.py` → `kemono-preview-interactions.blend` 188장면(시설 138·외형 교체 6·동작 44), resident-kit 시설 링크·1.6배·서는 위치 0.35 당김. GIF 188(`previews/gif`), 노션 묶음 GIF(`previews/notion`).
  - 재생성 순서: `run.py` → `boards.py` → `save_openable.py` → `export.py` → `preview.py` → `render_all.py`. 상세 `art/kemono-kit/README.md`.
- **Notion:** 도감에 '토끼 주민 조합 키트·동작·시설 상호작용' 페이지(https://app.notion.com/p/3fdc4a0ecd3181d0bf39c6a39b619ccc) — 부품 보드·뼈 배치·동작 묶음 4·상호작용 묶음 12.
- **Unity:** EraChecks 화면 이름 단언 수정(HUD V5 `ScienceFacilities`) → 19 통과. **회귀 18개 일괄 실행은 사용자 요청으로 중단**(Campaign 86만 통과 확인, 나머지 실행 실패 로그 TIMEOUT/launch retry) → 재실행 필요.
- **건물 모델 연결(사용자 '일단 건물만'):** 준비만 됨, 미적용. 스크래치 `FacilityVisual.cs`(BuildingKind 100종 → `Resources/Facilities/CC_*.fbx`, 상자 Renderer `forceRenderingOff` + 바닥 맞춤 자식 모델)·`FacilityVisualChecks.cs`·매핑 JSON. 다음 세션에 Assets로 옮겨 검사.
- **미커밋/주의:** `art/`는 미추적(용량, 커밋 안 함). `AntColony.unity`·`Water.mat`·`TimeManager.asset` 출처 불명 → 커밋 제외. Blender GUI는 15:08 크래시(대화상자 남아 있을 수 있음).
- **다음:** 1) 회귀 18개 재실행(Unity 포커스) 2) 건물 모델 연결 적용·검사 3) kemono-kit Unity 연결(대규모 → 승인 후) 4) CarryPerson 구조 자세·시설 접촉 정밀화 5) InfirmaryChecks `git rm`, C 배관·J 수송 범위 확인.
