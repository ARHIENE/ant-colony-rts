# 프로젝트 로그

## 현재 상태 — 2026-10-19 SAVE(Claude) — 모델 없던 시설 5종 Blender 제작 + Unity 건물 모델 표시 적용
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 이전 기록은 changelog.md. 공동 작업 보드 `docs/IMPLEMENTATION_BOARD.md`(FACILITY-VISUAL-01).
- **열린 작업 정리:** 사용자 지시로 이전 미완료 목록(도감 GIF 재업로드=완료, CHARACTER-CODE-01 회귀·HUD-V5-REAL 재개·Blender 애니 요청서·회귀 묶음·SPEC-1012 잔여) 전부 취소.
- **Blender(모델 없던 BuildingKind 5종):** 비축더미(QueenChamber → `CC_Stockpile`), 무기 연구소(ResearchLab, 근접~비행 5종 공용), 방어시설 연구소(DefenseLab), 휴게실(RestRoom, 기획상 오락 시설로 대체 예정이나 코드·건설 메뉴에 남아 있어 제작), 범위형 분사탑(AreaAcidTower, 단일형과 별도 모델·포탑 회전 클립).
  - `art/resident-kit/scripts/extra_spec.py`(SPEC·HINGES 5줄) + `models_extra.py`의 `structures()` → `blender -b --python scripts/kit_facilities_extra.py`로 재생성(시설 146개, facility-points.json 포함). 렌더 확인 완료.
  - DigSite(옛 '확장' 지점)는 기획에서 삭제된 기능이라 모델 없음(씬에 상자로 남음).
- **Unity 적용:** `Assets/_Project/Scripts/Buildings/FacilityVisual.cs`(BuildingKind 104종 → `Resources/Facilities/CC_*`), `BuildingBase.OnEnable`에서 `FacilityVisual.Attach`. 완공·저장 복원·씬 건물 모두 상자 그리기만 끄고(forceRenderingOff, 크기 판정 유지) 바닥 면적에 맞춘 모델 표시. 시작 비축더미는 Data가 없어 Stockpile 타입으로 판별. FBX 103개 `Assets/Resources/Facilities/`(URP Lit 색 정상).
  - 검사: FacilityVisualChecks 513(101종 실제 생성·면적 안·바닥 맞춤·비축더미) · SaveRoundtrip 57 · Spec1012 159 통과.
  - 한계: 모델이 상자 바닥 면적 기준 균일 배율이라 일부 가구가 캐릭터보다 크게 보일 수 있음. 피격 색 깜빡임 등 상자 렌더러 효과는 안 보임. 건설 중(공사장)은 기존 상자 그대로.
- **미커밋/주의:** `art/`는 미추적(용량). `AntColony.unity`·`Water.mat`·`TimeManager.asset` 출처 불명 → 커밋 제외. `Assets/Kiki/`·`Assets/Screenshots/`·`Assets/reports/` 미추적(이번 작업 아님).
- **Notion 개발 일지:** https://app.notion.com/p/3fdc4a0ecd3181518379c478eb9ba48b
- **다음:** 1) 실제 플레이로 건물 크기·비율 체감 확인(필요하면 종류별 배율 보정) 2) 피격·선택 표시를 모델에도 반영할지 결정 3) kemono-kit 캐릭터 Unity 연결(대규모 → 승인 후) 4) 미실행 회귀 묶음.
